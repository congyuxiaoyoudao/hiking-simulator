using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Hiking.Journey
{
    public class CameraController : MonoBehaviour
    {
        public Camera WorldCamera;
        [Header("直线旅途与全景")]
        [UnityEngine.Serialization.FormerlySerializedAs("routeScreenWidth")]
        [Tooltip("全景视角宽窄：1 对应 50° 视角；值越小，视角越宽。旅途视窗不受影响。")]
        [Range(.6f, 1f)] public float panoramaScreenWidth = .84f;
        [Range(30, 80)] public float overviewPitchAngle = 45;
        [InspectorName("螺旋上升时长"), Min(.1f)] public float overviewTransitionDuration = 6;
        [InspectorName("画面衔接时长"), Min(.1f)] public float projectionTransitionDuration = .8f;
        [InspectorName("螺旋起始抬升高度"), Min(1)] public float revealStartHeight = 5;
        [InspectorName("螺旋起始向外距离"), Min(.1f)] public float revealStartOutward = 3;
        [InspectorName("螺旋起始视角"), Range(15, 50)] public float revealStartFieldOfView = 35;
        [Tooltip("Canvas 中的画面衔接层，布局在场景中编辑")]
        public RawImage transitionImage;
        [InspectorName("全景相机高度（距环心）"), Min(1)] public float overviewHeight = 30;
        public float RevealOrbitDegrees { get; private set; }
        GameFlowController flow;
        Coroutine reveal;
        bool dragging;
        Vector2 lastPointer;
        float panoramaYaw, zoom = 1;
        TextMesh[] labels;
        RenderTexture transitionTexture;
        public bool IsTransitioning => reveal != null;
        public float ProjectionBlend { get; private set; }

        public void Initialize(GameFlowController owner)
        {
            flow = owner; WorldCamera.orthographic = true;
        }
        public void ResetCamera()
        {
            if (reveal != null) StopCoroutine(reveal);
            reveal = null; dragging = false; panoramaYaw = 0; zoom = 1; labels = null;
            WorldCamera.ResetProjectionMatrix(); ClearSnapshot(); ProjectionBlend = 0;
            if (flow != null) flow.Traveler.SetTransitionInset(null);
        }
        public void ShowStation(Station station)
        {
            if (flow.Map != null) FrameJourney();
        }
        void LateUpdate()
        {
            if (flow == null || flow.Map == null) return;
            if (reveal == null && (flow.IsPreviewing || flow.Session.Phase == JourneyPhase.Panorama)) BrowsePanorama();
            else if (reveal == null && flow.Session.Phase != JourneyPhase.Revealing) FrameJourney();
            flow.Traveler.FaceCamera(WorldCamera);
            if (labels == null) labels = flow.Map.GetComponentsInChildren<TextMesh>(true);
            foreach (var label in labels) label.transform.rotation = WorldCamera.transform.rotation;
        }
        void FrameJourney()
        {
            var map = flow.Map;
            WorldCamera.ResetProjectionMatrix();
            float width = map.ViewWidth;
            float progress = flow.Traveler.Progress01;
            float cameraX = map.transform.position.x - map.RouteLength * .5f
                + map.ViewStartDistance(progress) + width * .5f;
            WorldCamera.orthographic = true;
            WorldCamera.orthographicSize = width / (2 * Mathf.Max(.1f, WorldCamera.aspect));
            var rotation = Quaternion.Euler(overviewPitchAngle, 0, 0);
            var target = new Vector3(cameraX, map.transform.position.y + .6f, map.transform.position.z);
            WorldCamera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * 30, rotation);
            map.ArchiveBefore(cameraX - width * .5f - map.transform.position.x + map.RouteLength * .5f);
        }
        void Frame(Bounds bounds, float yaw, float zoomFactor)
        {
            WorldCamera.ResetProjectionMatrix();
            WorldCamera.orthographic = false;
            WorldCamera.fieldOfView = PanoramaFieldOfView;
            var rotation = Quaternion.Euler(overviewPitchAngle, yaw, 0);
            // Height is the user's exact camera elevation above the ring center, not an auto-fit minimum.
            float distance = Mathf.Max(1, overviewHeight) * zoomFactor /
                Mathf.Max(.01f, Mathf.Sin(overviewPitchAngle * Mathf.Deg2Rad));
            WorldCamera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
            WorldCamera.farClipPlane = Mathf.Max(100, distance + bounds.extents.magnitude * 2 + 10);
        }
        float PanoramaFieldOfView => Mathf.Clamp(50 / Mathf.Max(.1f, panoramaScreenWidth), 5, 120);
        void BrowsePanorama()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var p = mouse.position.ReadValue();
                bool blocked = !Application.isFocused || PointerUtility.OverUI(p);
                if (blocked || !mouse.leftButton.isPressed) dragging = false;
                if (!blocked && mouse.leftButton.wasPressedThisFrame) { dragging = true; lastPointer = p; }
                if (dragging) { panoramaYaw -= (p.x - lastPointer.x) * .3f; lastPointer = p; }
                if (!blocked) zoom = Mathf.Clamp(zoom - mouse.scroll.ReadValue().y / 120 * .1f, .8f, 2);
            }
            Frame(flow.Map.panoramaBounds, panoramaYaw, zoom);
        }
        public virtual void RevealPanorama(Bounds bounds, Action onComplete)
        {
            if (reveal != null) return;
            reveal = StartCoroutine(Reveal(onComplete));
        }
        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6 - 15) + 10);
        }
        static Matrix4x4 BlendProjection(Matrix4x4 from, Matrix4x4 to, float t)
        {
            var result = new Matrix4x4();
            for (int i = 0; i < 16; i++) result[i] = Mathf.Lerp(from[i], to[i], t);
            return result;
        }
        void CaptureSnapshot()
        {
            ClearSnapshot();
            if (transitionImage == null) return;
            transitionTexture = new RenderTexture(Mathf.Max(1, WorldCamera.pixelWidth),
                Mathf.Max(1, WorldCamera.pixelHeight), 24) { name = "JourneyTransitionSnapshot" };
            var previous = WorldCamera.targetTexture;
            try
            {
                WorldCamera.targetTexture = transitionTexture;
                WorldCamera.Render();
            }
            finally { WorldCamera.targetTexture = previous; }
            transitionImage.texture = transitionTexture;
            transitionImage.color = Color.white;
            transitionImage.gameObject.SetActive(true);
        }
        void ClearSnapshot()
        {
            if (transitionImage != null)
            {
                transitionImage.gameObject.SetActive(false);
                transitionImage.texture = null;
            }
            if (transitionTexture != null)
            {
                transitionTexture.Release(); Destroy(transitionTexture); transitionTexture = null;
            }
        }
        void OnDestroy() { ClearSnapshot(); }

        IEnumerator Reveal(Action onComplete)
        {
            FrameJourney();
            flow.Traveler.FaceCamera(WorldCamera);
            CaptureSnapshot();
            // The first yielded frame retains the previous projection, transform and map.
            RevealOrbitDegrees = 0;
            ProjectionBlend = 0;
            yield return null;
            float halfHeight = WorldCamera.orthographicSize;
            float aspect = WorldCamera.aspect;
            Vector3 oldFoxViewport = WorldCamera.WorldToViewportPoint(flow.Traveler.transform.position);
            float visualInset = flow.Traveler.visual != null ? flow.Traveler.visual.localPosition.x : 0;
            flow.Map.ShowCompletedRing(); flow.Traveler.RefreshPosition();
            Vector3 center = flow.Map.panoramaBounds.center;
            Vector3 fox = flow.Traveler.transform.position;

            // Align the ring's tangent at the actual traveler progress, including previews mid-route.
            float tangentYaw = (flow.Map.RouteDistanceAtProgress(flow.Traveler.Progress01) / flow.Map.RouteLength - .5f) * 360;
            var startRotation = Quaternion.Euler(overviewPitchAngle, tangentYaw, 0);
            var cameraRight = startRotation * Vector3.right;
            var cameraUp = startRotation * Vector3.up;
            var target = fox - cameraRight * ((oldFoxViewport.x - .5f) * 2 * halfHeight * aspect)
                - cameraUp * ((oldFoxViewport.y - .5f) * 2 * halfHeight);
            Vector3 startPosition = target - startRotation * Vector3.forward * 30;
            float near = WorldCamera.nearClipPlane;
            float far = Mathf.Max(WorldCamera.farClipPlane, flow.Map.panoramaBounds.size.magnitude * 3 + 30);
            WorldCamera.farClipPlane = far;
            var ortho = Matrix4x4.Ortho(-halfHeight * aspect, halfHeight * aspect,
                -halfHeight, halfHeight, near, far);
            WorldCamera.orthographic = false;
            WorldCamera.fieldOfView = 50;
            WorldCamera.projectionMatrix = ortho;
            WorldCamera.transform.SetPositionAndRotation(startPosition, startRotation);

            // The assembled ring stays visible. A close, raised camera sees only its local arc.
            Vector3 outward = Vector3.ProjectOnPlane(fox - center, Vector3.up).normalized;
            Vector3 stagePosition = fox + outward * revealStartOutward + Vector3.up * revealStartHeight;
            Quaternion stageRotation = Quaternion.LookRotation(fox - stagePosition);
            Vector3 fromCenter = stagePosition - center;
            float startAngle = Mathf.Atan2(fromCenter.x, fromCenter.z) * Mathf.Rad2Deg;
            panoramaYaw = startAngle + 180;
            Frame(flow.Map.panoramaBounds, panoramaYaw, 1);
            Vector3 finalPosition = WorldCamera.transform.position;
            float finalRadius = new Vector2(finalPosition.x - center.x, finalPosition.z - center.z).magnitude;
            float stageRadius = new Vector2(fromCenter.x, fromCenter.z).magnitude;
            float stageHeight = stagePosition.y;
            var perspective = Matrix4x4.Perspective(revealStartFieldOfView, aspect, near, far);
            float bridgeDuration = Mathf.Max(.1f, projectionTransitionDuration);
            for (float elapsed = 0; elapsed < bridgeDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Ease(elapsed / bridgeDuration);
                ProjectionBlend = t;
                WorldCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, stagePosition, t),
                    Quaternion.Slerp(startRotation, stageRotation, t));
                WorldCamera.projectionMatrix = BlendProjection(ortho, perspective, t);
                if (transitionImage != null) transitionImage.color = new Color(1, 1, 1, 1 - t);
                if (flow.Traveler.visual != null) flow.Traveler.SetTransitionInset(visualInset * (1 - t));
                yield return null;
            }
            ProjectionBlend = 1;
            WorldCamera.fieldOfView = revealStartFieldOfView;
            WorldCamera.ResetProjectionMatrix(); ClearSnapshot();
            flow.Traveler.SetTransitionInset(0);

            // Position and rotation at t=0 exactly match the bridge's end.
            float duration = Mathf.Max(.1f, overviewTransitionDuration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Ease(elapsed / duration);
                RevealOrbitDegrees = 360 * t;
                float angle = (startAngle + RevealOrbitDegrees) * Mathf.Deg2Rad;
                float horizontal = Mathf.Lerp(stageRadius, finalRadius, t);
                Vector3 position = center + new Vector3(Mathf.Sin(angle) * horizontal,
                    Mathf.Lerp(stageHeight, finalPosition.y, t) - center.y, Mathf.Cos(angle) * horizontal);
                Vector3 lookAt = Vector3.Lerp(fox, center, t);
                WorldCamera.fieldOfView = Mathf.Lerp(revealStartFieldOfView, PanoramaFieldOfView, t);
                WorldCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
                yield return null;
            }
            // The orbit endpoint is calculated by the same framing method used for browsing.
            Frame(flow.Map.panoramaBounds, panoramaYaw, 1);
            RevealOrbitDegrees = 360;
            reveal = null; onComplete?.Invoke();
        }

    }
}

