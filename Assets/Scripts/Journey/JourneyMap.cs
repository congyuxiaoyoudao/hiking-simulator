using System.Collections.Generic;
using UnityEngine;

namespace Hiking.Journey
{
    public class JourneyMap : MonoBehaviour
    {
        public Station[] stations;
        public Transform finishPoint;
        public Bounds panoramaBounds;
        [Header("圆环地图（固定地图配置）")]
        public bool circular;
        [Min(5)] public float radius = 18;
        public float finishAngle = 360;
        public RingMapSettings sourceSettings;
        public float BlockAngle => 360f / stations.Length;
        [System.NonSerialized] public Mesh[] generatedMeshes;
        public float RouteLength { get; set; }
        public float Bend { get; private set; }
        public int AssembledPatchCount { get; private set; }
        public bool ShowingRing { get; private set; }
        public float PanoramaScale => Mathf.Max(1, sourceSettings.panoramaScale);
        public Vector3 RingPoint(Vector3 route) => RouteGeometry.Point(route, RouteLength, 1) * PanoramaScale;
        readonly List<Mesh> ringMeshes = new List<Mesh>();
        readonly List<MeshRenderer> ringRenderers = new List<MeshRenderer>();
        readonly List<MeshRenderer> stripRenderers = new List<MeshRenderer>();
        readonly List<bool> assembled = new List<bool>();
        readonly List<Mesh> routeMeshes = new List<Mesh>();
        readonly List<Vector3[]> routeVertices = new List<Vector3[]>();
        readonly List<Vector3[]> displayVertices = new List<Vector3[]>();
        readonly List<Transform> routeObjects = new List<Transform>();
        readonly List<Vector3> objectCoordinates = new List<Vector3>();

        // Mesh vertices are authored in route coordinates. Runtime meshes are owned by this map.
        public void RegisterRouteMesh(Mesh mesh)
        {
            routeMeshes.Add(mesh); routeVertices.Add(mesh.vertices);
            displayVertices.Add(new Vector3[mesh.vertexCount]);
            generatedMeshes = routeMeshes.ToArray();
            var original = System.Array.Find(GetComponentsInChildren<MeshFilter>(), f => f.sharedMesh == mesh);
            stripRenderers.Add(original.GetComponent<MeshRenderer>());
            var obj = new GameObject(original.name + "_ArchivedRing");
            obj.transform.SetParent(transform, false);
            var ring = Instantiate(mesh); ring.name = mesh.name + "_Ring";
            obj.AddComponent<MeshFilter>().sharedMesh = ring;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.enabled = false;
            ringMeshes.Add(ring); ringRenderers.Add(renderer); assembled.Add(false);
        }
        // Retire only patches that have fully left the local viewport. Their route IDs stay unchanged.
        public void ArchiveBefore(float distance)
        {
            for (int i = 0; i < routeMeshes.Count; i++)
            {
                if (assembled[i]) continue;
                float end = 0;
                foreach (var v in routeVertices[i]) end = Mathf.Max(end, v.x);
                if (end > distance) continue;
                var vertices = new Vector3[routeVertices[i].Length];
                for (int v = 0; v < vertices.Length; v++) vertices[v] = RingPoint(routeVertices[i][v]);
                ringMeshes[i].vertices = vertices; ringMeshes[i].RecalculateNormals(); ringMeshes[i].RecalculateBounds();
                assembled[i] = true; AssembledPatchCount++;
            }
        }
        public void ShowCompletedRing()
        {
            ArchiveBefore(RouteLength + .01f);
            ShowingRing = true; Bend = 1; circular = true;
            for (int i = 0; i < ringRenderers.Count; i++) { stripRenderers[i].enabled = false; ringRenderers[i].enabled = true; }
            for (int i = 0; i < routeObjects.Count; i++)
            {
                routeObjects[i].position = transform.TransformPoint(RingPoint(objectCoordinates[i]));
                routeObjects[i].rotation = transform.rotation * RouteGeometry.Rotation(objectCoordinates[i].x, RouteLength, 1);
            }
            panoramaBounds = new Bounds(transform.TransformPoint(new Vector3(0, 0, -radius) * PanoramaScale), new Vector3(2 * radius + 2, 4, 2 * radius + 2) * PanoramaScale);
        }
        public void ShowJourney()
        {
            ShowingRing = false;
            for (int i = 0; i < ringRenderers.Count; i++) { ringRenderers[i].enabled = false; stripRenderers[i].enabled = true; }
            SetBend(0);
        }
        public void RegisterRouteObject(Transform target, Vector3 routeCoordinates)
        {
            routeObjects.Add(target); objectCoordinates.Add(routeCoordinates);
            target.position = transform.TransformPoint(RouteGeometry.Point(routeCoordinates, RouteLength, Bend));
        }
        // One station fills the viewport. At each stop its left edge aligns with the viewport;
        // between stops it slides exactly one station length while the fox stays at Progress01.
        public float ViewStartDistance(float progress)
        {
            if (stations == null || stations.Length == 0) return 0;
            float section = RouteLength / stations.Length;
            if (progress <= stations[0].stopProgress) return 0;
            for (int i = 0; i < stations.Length - 1; i++)
                if (progress <= stations[i + 1].stopProgress)
                    return (i + Mathf.InverseLerp(stations[i].stopProgress,
                        stations[i + 1].stopProgress, progress)) * section;
            return (stations.Length - 1) * section;
        }
        public float RouteDistanceAtProgress(float progress) =>
            ViewStartDistance(Mathf.Clamp01(progress)) + Mathf.Clamp01(progress) * RouteLength / stations.Length;
        public Vector3 ProgressPoint(float progress)
        {
            var route = new Vector3(RouteDistanceAtProgress(progress), sourceSettings.ringThickness * .5f + .5f, 0);
            return transform.TransformPoint(ShowingRing ? RingPoint(route) : RouteGeometry.Point(route, RouteLength, Bend));
        }

        public void SetBend(float bend)
        {
            Bend = Mathf.Clamp01(bend); circular = Bend >= .9999f;
            var bounds = new Bounds(); bool first = true;
            for (int m = 0; m < routeMeshes.Count; m++)
            {
                for (int v = 0; v < routeVertices[m].Length; v++)
                {
                    var point = RouteGeometry.Point(routeVertices[m][v], RouteLength, Bend);
                    displayVertices[m][v] = point;
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
                routeMeshes[m].vertices = displayVertices[m];
                routeMeshes[m].RecalculateNormals(); routeMeshes[m].RecalculateBounds();
            }
            for (int i = 0; i < routeObjects.Count; i++)
            {
                if (routeObjects[i] == null) continue;
                routeObjects[i].position = transform.TransformPoint(RouteGeometry.Point(objectCoordinates[i], RouteLength, Bend));
                routeObjects[i].rotation = transform.rotation * RouteGeometry.Rotation(objectCoordinates[i].x, RouteLength, Bend);
            }
            bounds.Expand(new Vector3(1, 4, 1));
            panoramaBounds = new Bounds(transform.TransformPoint(bounds.center), bounds.size);
        }

        void OnDestroy()
        {
            foreach (var mesh in ringMeshes) if (mesh != null) Destroy(mesh);
            if (generatedMeshes == null) return;
            foreach (var mesh in generatedMeshes)
                if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }

        public Vector3 LocalPoint(float angle) => RingJourneyMath.Point(angle, radius);

        public bool IsVisible(PlacementSlot slot)
        {
            // 在3D模式下，所有槽位始终可见（由相机视锥体决定）
            return true;
        }

        public bool Validate(out string error)
        {
            error = null;
            if (circular && (radius <= 0 || finishAngle != 360))
            { error = "圆环半径必须为正，旅途终点必须为 360 度。"; return false; }
            if (stations == null || stations.Length < 1 || finishPoint == null)
                error = "地图需要站点和一个观景终点。";
            else
            {
                var ids = new HashSet<string>();
                var stationIds = new HashSet<string>();
                foreach (var station in stations)
                {
                    if (station == null || station.stopPoint == null || station.slots == null ||
                        station.slots.Length != 3 || string.IsNullOrEmpty(station.stationId) ||
                        !stationIds.Add(station.stationId) || station.cameraRange.y <= station.cameraRange.x)
                    { error = "站点需要唯一 ID、驻足点、三个投放点和有效镜头范围。"; break; }
                    foreach (var slot in station.slots)
                        if (slot == null || string.IsNullOrEmpty(slot.slotId) || !ids.Add(slot.slotId))
                        { error = "投放点缺失或 ID 重复。"; break; }
                    if (station.outgoingWaypoints != null)
                        foreach (var point in station.outgoingWaypoints)
                            if (point == null) error = "行走路径存在空引用。";
                    if (error != null) break;
                }
                if (error == null && circular)
                {
                    // 跳过驻足点位置验证，在3D模式下由用户自由调整
                }
                if (panoramaBounds.size.x <= 0 || panoramaBounds.size.y <= 0)
                    error = "全景范围无效。";
            }
            return error == null;
        }

        public Vector3[] RouteFrom(int index)
        {
            var points = new List<Vector3>();
            var via = stations[index].outgoingWaypoints;
            if (via != null) foreach (var point in via) points.Add(point.position);
            points.Add(index + 1 < stations.Length ? stations[index + 1].stopPoint.position : finishPoint.position);
            return points.ToArray();
        }
    }
}
