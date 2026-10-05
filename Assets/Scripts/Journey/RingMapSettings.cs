using System;
using UnityEngine;

namespace Hiking.Journey
{
    [CreateAssetMenu(menuName = "Hiking/Ring Map Settings")]
    public class RingMapSettings : ScriptableObject
    {
        [Header("站点地图与内部 Patch，新旅途生效")]
        [Tooltip("路线由这些站点顺序拼接。")]
        [InspectorName("站点地图数量"), Range(4, 24)] public int blockCount = 6;
        [InspectorName("每站路线长度"), Min(2)] public float stationLength = 6;
        [InspectorName("每站 Patch 数量"), Range(1, 24)] public int patchesPerStation = 6;
        public Material pathMaterial;
        [Tooltip("投放点的轮廓图。留空时用运行时生成的白色方块，方便先跑通流程。")]
        [InspectorName("投放点轮廓")] public Sprite slotMarkerSprite;
        [Tooltip("直线旅途与全景圆环共用的站点占位颜色。")]
        [InspectorName("各站占位颜色")] public Color[] stationColors =
        {
            new Color(.93f, .28f, .25f), new Color(.20f, .64f, .93f),
            new Color(.92f, .70f, .18f), new Color(.50f, .28f, .85f),
            new Color(.20f, .75f, .50f), new Color(.95f, .38f, .65f)
        };
        [Tooltip("出生点占一个视窗宽度，使用中性色。终点位于最后一站末尾。")]
        [InspectorName("起点段颜色")] public Color transitionColor = new Color(.7f, .7f, .66f);

        [Header("3D 路线设置")]
        [InspectorName("全景圆环尺寸倍率"), Min(1)] public float panoramaScale = 1.5f;
        [InspectorName("道路厚度"), Range(0.1f, 2f)] public float ringThickness = 0.5f;

        [Tooltip("0.5 使出生点、各站和终点在进度条上等间隔；调低或调高可提前或推迟该站驻足。实际地块内位置会随站序变化。新旅途生效。")]
        [InspectorName("各站驻足进度段比例（0～1）")] public float[] stopPositions = { .5f, .5f, .5f, .5f, .5f, .5f };
        public float BlockAngle => 360f / (blockCount + 1);
        public float StopPosition(int index) => stopPositions != null && index < stopPositions.Length ? Mathf.Clamp(stopPositions[index], .01f, .99f) : .5f;
        public Color StationColor(int index) => stationColors != null && index < stationColors.Length
            ? stationColors[index] : Color.HSVToRGB(Mathf.Repeat(index * .618034f, 1), .7f, 1);

        public void ResizeStops()
        {
            blockCount = Mathf.Clamp(blockCount, 4, 24);
            patchesPerStation = Mathf.Clamp(patchesPerStation, 1, 24);
            int oldLength = stopPositions == null ? 0 : stopPositions.Length;
            Array.Resize(ref stopPositions, blockCount);
            for (int i = 0; i < blockCount; i++) stopPositions[i] = i < oldLength ? Mathf.Clamp(stopPositions[i], .01f, .99f) : .5f;
            int oldColors = stationColors == null ? 0 : stationColors.Length;
            Array.Resize(ref stationColors, blockCount);
            for (int i = oldColors; i < blockCount; i++) stationColors[i] = Color.HSVToRGB(Mathf.Repeat(i * .618034f, 1), .7f, 1);
            stationLength = Mathf.Max(2, stationLength);
        }
        void OnValidate() => ResizeStops();
    }
}
