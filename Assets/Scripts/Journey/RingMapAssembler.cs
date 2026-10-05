using System;
using UnityEngine;
namespace Hiking.Journey
{
    // Saved provider entry point retained; generation now uses straight route coordinates.
    public static class RingMapAssembler
    {
        public static JourneyMap Build(RingMapSettings settings, Transform parent)
        {
            if (settings == null || settings.pathMaterial == null || settings.blockCount < 1)
                throw new InvalidOperationException("请配置路线设置与路径材质。");
            var root = new GameObject("JourneyRoute");
            root.transform.SetParent(parent, false);
            var map = root.AddComponent<JourneyMap>();
            try
            {
                map.sourceSettings = settings;
                map.RouteLength = Mathf.Max(2, settings.stationLength) * settings.blockCount;
                map.radius = map.RouteLength / (2 * Mathf.PI);
                map.stations = new Station[settings.blockCount];
                float section = map.RouteLength / settings.blockCount;
                int patches = Mathf.Clamp(settings.patchesPerStation, 1, 24);
                for (int i = 0; i < map.stations.Length; i++)
                {
                    var station = new GameObject("Station_" + (i + 1).ToString("00")).AddComponent<Station>();
                    station.transform.SetParent(root.transform, false);
                    station.stationId = "station-" + (i + 1);
                    station.cameraRange = new Vector2(i * section, (i + 1) * section);
                    station.stopProgress = (i + settings.StopPosition(i)) / settings.blockCount;
                    station.stopPoint = Anchor(map, station.transform, "StopPoint",
                        new Vector3((i + station.stopProgress) * section, settings.ringThickness * .5f + .5f, 0));
                    var marker = Anchor(map, station.transform, "StopLabel",
                        new Vector3((i + station.stopProgress) * section, 2f, 0));
                    var text = marker.gameObject.AddComponent<TextMesh>();
                    text.text = "STOP " + (i + 1).ToString("00"); text.fontSize = 48;
                    text.characterSize = .04f; text.anchor = TextAnchor.MiddleCenter; text.color = Color.white;
                    station.slots = new PlacementSlot[3];
                    for (int j = 0; j < 3; j++)
                    {
                        var slot = Anchor(map, station.transform, "Slot_" + j,
                            new Vector3((i + .25f * (j + 1)) * section, .5f, 1));
                        station.slots[j] = slot.gameObject.AddComponent<PlacementSlot>();
                        station.slots[j].slotId = station.stationId + "/slot-" + j;
                        slot.gameObject.SetActive(false);
                    }
                    for (int p = 0; p < patches; p++)
                    {
                        var obj = new GameObject("Patch_" + (p + 1).ToString("00"));
                        obj.transform.SetParent(station.transform, false);
                        var mesh = CreateStrip((i + (float)p / patches) * section, section / patches,
                            settings.ringThickness, settings.StationColor(i), obj.name);
                        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                        obj.AddComponent<MeshRenderer>().sharedMaterial = settings.pathMaterial;
                        map.RegisterRouteMesh(mesh);
                    }
                    map.stations[i] = station;
                }
                map.finishPoint = Anchor(map, root.transform, "FinishPoint_100Percent",
                    new Vector3(map.RouteLength, settings.ringThickness * .5f + .5f, 0));
                map.SetBend(0);
                if (!map.Validate(out string error)) throw new InvalidOperationException(error);
                return map;
            }
            catch { UnityEngine.Object.Destroy(root); throw; }
        }
        static Transform Anchor(JourneyMap map, Transform parent, string name, Vector3 route)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false);
            map.RegisterRouteObject(point, route);
            return point;
        }
        static Mesh CreateStrip(float start, float length, float thickness, Color color, string name)
        {
            const int segments = 4;
            var vertices = new Vector3[4 * (segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[4 * segments * 6];
            for (int face = 0; face < 4; face++)
            {
                Vector2 a, b;
                if (face == 0) { a = new Vector2(thickness / 2, -.5f); b = new Vector2(thickness / 2, .5f); }
                else if (face == 1) { a = new Vector2(-thickness / 2, .5f); b = new Vector2(-thickness / 2, -.5f); }
                else if (face == 2) { a = new Vector2(-thickness / 2, -.5f); b = new Vector2(thickness / 2, -.5f); }
                else { a = new Vector2(thickness / 2, .5f); b = new Vector2(-thickness / 2, .5f); }
                for (int n = 0; n <= segments; n++)
                {
                    int v = face * (segments + 1) * 2 + n * 2;
                    float x = start + length * n / segments;
                    vertices[v] = new Vector3(x, a.x, a.y); vertices[v+1] = new Vector3(x, b.x, b.y);
                    uv[v] = new Vector2(x, 0); uv[v+1] = new Vector2(x, 1);
                    if (n == segments) continue;
                    int t = (face * segments + n) * 6;
                    triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
                    triangles[t+3] = v+1; triangles[t+4] = v+3; triangles[t+5] = v+2;
                }
            }
            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            var mesh = new Mesh { name = name, vertices = vertices, colors = colors, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}

