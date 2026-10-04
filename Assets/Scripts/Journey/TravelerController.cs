using System;
using System.Collections;
using UnityEngine;
namespace Hiking.Journey
{
    public class TravelerController : MonoBehaviour
    {
        public Transform visual;
        public bool IsMoving { get; private set; }
        public float Progress01 { get; private set; }
        public bool Paused { get; set; }
        Coroutine movement;
        JourneyMap map;
        float? transitionInset;
        public void SetTransitionInset(float? inset) => transitionInset = inset;
        public void PlaceOnRoute(JourneyMap route)
        {
            CancelMovement(); map = route; Progress01 = 0;
            transform.SetParent(map.transform, false); RefreshPosition();
        }
        public void RefreshPosition()
        {
            if (map != null) transform.position = map.ProgressPoint(Progress01);
        }
        public void FaceCamera(Camera camera)
        {
            transform.rotation = camera.transform.rotation;
            if (visual == null) return;
            var sprite = visual.GetComponentInChildren<SpriteRenderer>();
            float halfWidth = sprite != null ? sprite.localBounds.extents.x * sprite.transform.localScale.x : .4f;
            // Keep the sprite fully on screen while its progress anchor touches the route's screen edge.
            visual.localPosition = new Vector3(transitionInset ?? (map != null && !map.ShowingRing ? (1 - 2 * Progress01) * halfWidth : 0), 0, 0);
        }
        public void MoveToProgress(float target, float duration, Action arrived)
        {
            if (IsMoving || map == null) return;
            movement = StartCoroutine(Walk(Mathf.Clamp01(target), duration, arrived));
        }
        IEnumerator Walk(float target, float duration, Action arrived)
        {
            IsMoving = true;
            float start = Progress01;
            float elapsed = 0;
            yield return null;
            while (Progress01 < target || Paused)
            {
                if (Paused) { yield return null; continue; }
                elapsed += Time.unscaledDeltaTime;
                Progress01 = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration)));
                RefreshPosition(); yield return null;
            }
            IsMoving = false; movement = null; arrived?.Invoke();
        }
        public void CancelMovement()
        {
            if (movement != null) StopCoroutine(movement);
            movement = null; IsMoving = false;
            transitionInset = null;
            if (visual != null) visual.localPosition = Vector3.zero;
        }
        void OnDisable() => CancelMovement();
    }
}

