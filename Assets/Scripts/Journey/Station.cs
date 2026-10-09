using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hiking.Journey
{
    public enum StationTheme { Snow, Grass, Desert }

    public class Station : MonoBehaviour
    {
        public string stationId;
        public StationTheme theme;
        public int tileCount;
        public static bool IsTransitionTile(int tile) => tile % 4 == 3;
        public TileState[] Tiles { get; private set; }
        TextMesh[] moistureLabels;
        readonly Dictionary<PlantInstance, SpriteRenderer> plantMarkers = new Dictionary<PlantInstance, SpriteRenderer>();
        RingMapSettings mapSettings;
        bool debugLabels;
        public void InitializeTiles(RingMapSettings settings)
        {
            mapSettings = settings;
            Tiles = new TileState[tileCount]; moistureLabels = new TextMesh[tileCount];
            for (int i = 0; i < tileCount; i++) Tiles[i] = settings.CreateTile(theme);
        }
        public void BindNaturalPlant(PlantInstance plant, SpriteRenderer marker) => plantMarkers.Add(plant, marker);
        public float MoistureAt(int tile) => Tiles[tile].SurfaceWater;
        public void SetMoistureLabel(int tile, TextMesh label)
        {
            moistureLabels[tile] = label; RefreshMoisture(tile);
        }
        public void AddWater(int tile, int quantity)
        {
            if (quantity <= 0) return;
            Tiles[tile].AddWater(quantity); RefreshMoisture(tile);
        }
        public void SetDebugLabels(bool visible)
        {
            debugLabels = visible;
            for (int i = 0; i < tileCount; i++)
            {
                var label = moistureLabels[i];
                if (label != null)
                {
                    label.anchor = visible ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
                    label.characterSize = (visible ? .016f : .028f) * Mathf.Min(1, TileWidth);
                    label.lineSpacing = 1.1f;
                }
                RefreshMoisture(i);
            }
        }
        public void StepTiles(float infiltration)
        {
            for (int i = 0; i < tileCount; i++)
            {
                Tiles[i].Step(infiltration, mapSettings.plantCatalog);
                foreach (var plant in Tiles[i].NaturalPlants)
                {
                    if (!plantMarkers.TryGetValue(plant, out var marker) || marker == null) continue;
                    var definition = mapSettings.plantCatalog.Find(plant.plantId);
                    var stage = definition.stages[plant.stageIndex];
                    var sprite = stage.sprite != null ? stage.sprite : mapSettings.fallbackPlantSprite;
                    if (sprite == null) { marker.enabled = false; continue; }
                    marker.sprite = sprite;
                    marker.color = stage.sprite == null ? stage.fallbackColor : Color.white;
                    float size = Mathf.Min(TileWidth * .16f, .16f);
                    marker.transform.localScale = Vector3.one * (size /
                        Mathf.Max(.001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y)));
                }
                RefreshMoisture(i);
            }
            foreach (var entry in plantMarkers)
                if (entry.Key.life <= 0 && entry.Value != null) entry.Value.gameObject.SetActive(false);
        }
        static string Delta(float value, string suffix = "") => $"({value:+0.0;-0.0;0}{suffix})";
        void RefreshMoisture(int tile)
        {
            if (moistureLabels[tile] == null) return;
            var t = Tiles[tile]; var d = t.LastChange;
            moistureLabels[tile].text = debugLabels ?
                $"{stationId.Replace("station-", "")}-{tile + 1} {t.CategoryName}\n" +
                $"泥土 {t.SoilContent * 100:0.0}% {Delta(d.SoilContent * 100)}\n" +
                $"容量 {t.MaxWater:0.0} {Delta(d.MaxWater)}\n" +
                $"自然植物 {t.PlantCount} {Delta(d.PlantCount)}\n" +
                $"植 {PlantDetails(t)}\n" +
                $"湿度 {t.SoilHumidity * 100:0.0}% {Delta(d.SoilHumidity * 100)}\n" +
                $"蒸发 {t.EvaporationRate:0.00}/步 {Delta(d.EvaporationRate)}\n" +
                $"高度 {t.Height} {Delta(d.Height)}\n" +
                $"地表水 {t.SurfaceWater:0.0} {Delta(d.SurfaceWater)}\n" +
                $"温度 {t.Temperature:0.0}°C {Delta(d.Temperature)}" : $"地表水 {t.SurfaceWater:0.0}";
        }
        static string PlantDetails(TileState tile) => tile.PlantCount == 0 ? "无" :
            string.Join(" ", Array.ConvertAll(tile.NaturalPlants, plant =>
                $"{plant.life}/{plant.stageIndex + 1}{(plant.lastLifeChange > 0 ? "+" : plant.lastLifeChange < 0 ? "-" : "=")}"));
        public float startDistance, length;
        public float TileWidth => length / tileCount;
        public float TileCenter(int index) => startDistance + (Mathf.Clamp(index, 0, tileCount - 1) + .5f) * TileWidth;
        public string ThemeName => theme == StationTheme.Snow ? "雪地" : theme == StationTheme.Grass ? "平原" : "沙漠";
        public Transform stopPoint;
        public PlacementSlot[] slots;
        [Range(0, 1)] public float stopProgress;
        // StopPoint is the single source of truth, so moving the marker changes the actual destination.
        public float StopAngle
        {
            get
            {
                var map = GetComponentInParent<JourneyMap>();
                Vector3 point = map.transform.InverseTransformPoint(stopPoint.position);
                // XZ平面上的角度，逆时针方向（X使用负号）
                return Mathf.Repeat(Mathf.Atan2(-point.x, point.z) * Mathf.Rad2Deg, 360);
            }
        }
        [Tooltip("按顺序经过这些点，再抵达下一站；最后一站通向终点")]
        public Transform[] outgoingWaypoints;
        [Tooltip("本站左右边界（路线距离）")]
        public Vector2 cameraRange;
    }
}
