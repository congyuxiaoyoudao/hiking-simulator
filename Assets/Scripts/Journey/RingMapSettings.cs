using UnityEngine;

namespace Hiking.Journey
{
    // Asset type/name retained so existing scene and asset GUIDs remain valid.
    [CreateAssetMenu(menuName = "Hiking/Station Map Settings")]
    public class RingMapSettings : ScriptableObject
    {
        [Header("直线站点地图（新旅途生效）")]
        [InspectorName("站点数量"), Min(1)] public int blockCount = 6;
        [InspectorName("每站路线长度"), Min(2)] public float stationLength = 15;
        [InspectorName("每站地块数量"), Min(3)] public int patchesPerStation = 15;
        public Material pathMaterial;
        [Header("随机站点主题颜色")]
        [InspectorName("雪山")] public Color snowColor = Color.white;
        [InspectorName("草原")] public Color grassColor = new Color(.3f, .72f, .3f);
        [InspectorName("沙漠")] public Color desertColor = new Color(1f, .82f, .25f);
        public Color ThemeColor(StationTheme theme) => theme == StationTheme.Snow ? snowColor : theme == StationTheme.Grass ? grassColor : desertColor;
        [Header("地块初始水分")]
        [Min(0)] public int snowMoisture = 4;
        [Min(0)] public int grassMoisture = 6;
        [Min(0)] public int desertMoisture = 2;
        public Font tileLabelFont;
        public int InitialMoisture(StationTheme theme) => Mathf.Max(0, theme == StationTheme.Snow ? snowMoisture : theme == StationTheme.Grass ? grassMoisture : desertMoisture);
        [Header("道路与完成全景")]
        [InspectorName("全景圆环尺寸倍率"), Min(1)] public float panoramaScale = 1.5f;
        [InspectorName("道路厚度"), Range(.1f, 2f)] public float ringThickness = .5f;
        void OnValidate()
        {
            blockCount = Mathf.Max(1, blockCount);
            patchesPerStation = Mathf.Max(3, patchesPerStation);
            stationLength = Mathf.Max(2, stationLength);
        }
    }
}
