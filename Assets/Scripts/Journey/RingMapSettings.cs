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

        [Header("3D 路线设置")]
        [InspectorName("全景圆环尺寸倍率"), Min(1)] public float panoramaScale = 1.5f;
        [InspectorName("道路厚度"), Range(0.1f, 2f)] public float ringThickness = 0.5f;

        [Tooltip("各站驻足位置，0.5 为本站中点。新旅途生效。")]
        [InspectorName("各地块驻足位置（0～1）")] public float[] stopPositions = { .5f, .5f, .5f, .5f, .5f, .5f };
        public float BlockAngle => 360f / blockCount;
        public float StopPosition(int index) => stopPositions != null && index < stopPositions.Length ? Mathf.Clamp(stopPositions[index], .01f, .99f) : .5f;

        public void ResizeStops()
        {
            blockCount = Mathf.Clamp(blockCount, 4, 24);
            patchesPerStation = Mathf.Clamp(patchesPerStation, 1, 24);
            int oldLength = stopPositions == null ? 0 : stopPositions.Length;
            Array.Resize(ref stopPositions, blockCount);
            for (int i = 0; i < blockCount; i++) stopPositions[i] = i < oldLength ? Mathf.Clamp(stopPositions[i], .01f, .99f) : .5f;
            stationLength = Mathf.Max(2, stationLength);
        }
        void OnValidate() => ResizeStops();
    }
}
