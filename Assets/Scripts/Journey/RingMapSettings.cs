using UnityEngine;

namespace Hiking.Journey
{
    // Asset type/name retained so existing scene and asset GUIDs remain valid.
    [CreateAssetMenu(menuName = "Hiking/Station Map Settings")]
    public class RingMapSettings : ScriptableObject
    {
        [Header("直线站点地图（新旅途生效）")]
        [InspectorName("站点数量"), Min(1)] public int blockCount = 6;
        [InspectorName("每站路线长度"), Min(2)] public float stationLength = 12;
        public const int TilesPerStation = 12;
        [HideInInspector] public int patchesPerStation = TilesPerStation;
        [InspectorName("过渡地块颜色")] public Color transitionTileColor = new Color(.4f, .47f, .58f);
        public Material pathMaterial;
        [Header("随机站点主题颜色")]
        [InspectorName("雪地")] public Color snowColor = Color.white;
        [InspectorName("平原")] public Color grassColor = new Color(.3f, .72f, .3f);
        [InspectorName("沙漠")] public Color desertColor = new Color(1f, .82f, .25f);
        public Color ThemeColor(StationTheme theme) => theme == StationTheme.Snow ? snowColor : theme == StationTheme.Grass ? grassColor : desertColor;
        [Header("自然植物")]
        [Tooltip("按地块主题选择自然生长植物；没有适宜植物时该地块不生成自然植物")]
        public PlantCatalog plantCatalog;
        [Tooltip("植物阶段未配置 Sprite 时使用的白色方块，按阶段占位色着色")]
        public Sprite fallbackPlantSprite;
        [Header("植物前后景位置（新地图生效）")]
        [Tooltip("前景植物相对道路顶面的 Y 轴高度；背景植物以此为基线")]
        [InspectorName("前景基线高度（Y）")] public float foregroundPlantY = .13f;
        [Tooltip("背景植物相对前景基线沿 Y 轴向上移动的距离")]
        [InspectorName("背景上移量"), Min(0)] public float backgroundPlantOffset = .96f;
        [Header("地块随机初值（百分比以 0～1 填写，新地图生效）")]
        public Vector2 soilContentRange = new Vector2(0, 1);
        public Vector2Int plantCountRange = new Vector2Int(0, 5);
        public Vector2 soilHumidityRange = new Vector2(0, 1);
        [Tooltip("水量 / 步；实际蒸发量 = 此值 × (1 + 温度 / 20)")]
        public Vector2 evaporationRateRange = new Vector2(.1f, .6f);
        public Vector2Int heightRange = new Vector2Int(0, 1);
        public Vector2 surfaceWaterRange = new Vector2(0, 30);
        [Header("单步水分模拟")]
        [Tooltip("每步渗水上限 = 此值 × 泥土含量，受地表水和剩余容量限制")]
        [Min(0)] public float infiltrationPerStep = 5;
        public Font tileLabelFont;
        public TileState CreateTile(StationTheme theme)
        {
            Vector2 temperature = theme == StationTheme.Snow ? new Vector2(0, 5) :
                theme == StationTheme.Grass ? new Vector2(5, 12) : new Vector2(12, 20);
            int count = Sample(plantCountRange, 0, 5);
            float soilContent = Sample(soilContentRange, 0, 1);
            float humidity = Sample(soilHumidityRange, 0, 1);
            var naturalPlants = plantCatalog != null ? plantCatalog.SpawnNaturalPlants(theme, count,
                soilContent * 100f, humidity * 100f) : null;
            return new TileState(theme, soilContent, count,
                humidity, Sample(evaporationRateRange, 0, float.MaxValue),
                Sample(heightRange, 0, 1), Sample(surfaceWaterRange, 0, 30), Random.Range(temperature.x, temperature.y),
                naturalPlants);
        }
        static float Sample(Vector2 range, float min, float max) => Random.Range(
            Mathf.Clamp(Mathf.Min(range.x, range.y), min, max), Mathf.Clamp(Mathf.Max(range.x, range.y), min, max));
        static int Sample(Vector2Int range, int min, int max) => Random.Range(
            Mathf.Clamp(Mathf.Min(range.x, range.y), min, max), Mathf.Clamp(Mathf.Max(range.x, range.y), min, max) + 1);
        [Header("道路与完成全景")]
        [InspectorName("全景圆环尺寸倍率"), Min(1)] public float panoramaScale = 1.5f;
        [InspectorName("道路厚度"), Range(.1f, 2f)] public float ringThickness = .5f;
        void OnValidate()
        {
            blockCount = Mathf.Max(1, blockCount);
            patchesPerStation = TilesPerStation;
            stationLength = Mathf.Max(2, stationLength);
        }
    }
}
