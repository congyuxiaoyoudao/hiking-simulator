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
                // One viewport for the spawn, followed by the stations; the route ends at the last station.
                map.RouteLength = Mathf.Max(2, settings.stationLength) * (settings.blockCount + 1);
                map.radius = map.RouteLength / (2 * Mathf.PI);
                map.stations = new Station[settings.blockCount];
                float section = map.ViewWidth;
                int patches = Mathf.Clamp(settings.patchesPerStation, 1, 24);
                var startBuffer = new GameObject("StartBuffer").transform;
                startBuffer.SetParent(root.transform, false);
                AddPatches(map, startBuffer, 0, section, patches, settings, settings.transitionColor);
                for (int i = 0; i < map.stations.Length; i++)
                {
                    var station = new GameObject("Station_" + (i + 1).ToString("00")).AddComponent<Station>();
                    station.transform.SetParent(root.transform, false);
                    station.stationId = "station-" + (i + 1);
                    station.cameraRange = new Vector2((i + 1) * section, (i + 2) * section);
                    // Default .5 spaces spawn, every stop, and finish evenly on the progress bar.
                    station.stopProgress = (i + .5f + settings.StopPosition(i)) / (settings.blockCount + 1);
                    float stopDistance = (i + 1) * section + station.stopProgress * section;
                    station.stopPoint = Anchor(map, station.transform, "StopPoint",
                        new Vector3(stopDistance, settings.ringThickness * .5f + .5f, 0));
                    var marker = Anchor(map, station.transform, "StopLabel",
                        new Vector3(stopDistance, 2f, 0));
                    var text = marker.gameObject.AddComponent<TextMesh>();
                    text.text = "STOP " + (i + 1).ToString("00"); text.fontSize = 48;
                    text.characterSize = .04f; text.anchor = TextAnchor.MiddleCenter; text.color = Color.white;
                    station.slots = new PlacementSlot[3];
                    for (int j = 0; j < 3; j++)
                    {
                        var slot = Anchor(map, station.transform, "Slot_" + j,
                            new Vector3((i + 1 + .25f * (j + 1)) * section, .5f, 1));
                        station.slots[j] = BuildSlot(slot, station.stationId + "/slot-" + j, settings);
                    }
                    AddPatches(map, station.transform, (i + 1) * section, section,
                        patches, settings, settings.StationColor(i));
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
        // 投放点由三个部件组成：轮廓、数量文字、命中区。
        // 轮廓和命中区默认关掉，由 PlacementSlot.Highlight 在选中材料时打开；
        // 注意不能对槽位整体 SetActive(false)——投放物是它的子物体，会被一起隐藏。
        static PlacementSlot BuildSlot(Transform anchor, string id, RingMapSettings settings)
        {
            var slot = anchor.gameObject.AddComponent<PlacementSlot>();
            slot.slotId = id;

            var markerObject = new GameObject("Marker");
            markerObject.transform.SetParent(anchor, false);
            markerObject.transform.localScale = Vector3.one * .6f;
            var marker = markerObject.AddComponent<SpriteRenderer>();
            marker.sprite = settings.slotMarkerSprite != null ? settings.slotMarkerSprite : WhiteSprite();
            marker.sortingOrder = 5;
            marker.enabled = false;
            slot.marker = marker;

            var labelObject = new GameObject("Count");
            labelObject.transform.SetParent(anchor, false);
            labelObject.transform.localPosition = new Vector3(0, .45f, -.05f);
            var label = labelObject.AddComponent<TextMesh>();
            label.fontSize = 48;
            label.characterSize = .02f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.white;
            slot.label = label;

            var hitbox = anchor.gameObject.AddComponent<BoxCollider2D>();
            hitbox.size = new Vector2(.8f, .9f);
            hitbox.enabled = false;
            slot.hitbox = hitbox;
            return slot;
        }
        static Sprite whiteSprite;
        // 没有配轮廓图时的兜底：2x2 纯白贴图、每单位 2 像素，正好是 1x1 世界单位。
        static Sprite WhiteSprite()
        {
            if (whiteSprite != null) return whiteSprite;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = "SlotMarkerTexture";
            var pixels = new Color[4];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2f);
            whiteSprite.name = "SlotMarker";
            return whiteSprite;
        }
        static void AddPatches(JourneyMap map, Transform parent, float start, float length,
            int count, RingMapSettings settings, Color color)
        {
            for (int p = 0; p < count; p++)
            {
                var obj = new GameObject("Patch_" + (p + 1).ToString("00"));
                obj.transform.SetParent(parent, false);
                var mesh = CreateStrip(start + p * length / count, length / count,
                    settings.ringThickness, color, obj.name);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                obj.AddComponent<MeshRenderer>().sharedMaterial = settings.pathMaterial;
                map.RegisterRouteMesh(mesh);
            }
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

