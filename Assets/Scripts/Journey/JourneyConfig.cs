using System;
using UnityEngine;

namespace Hiking.Journey
{
    public enum JourneyMode { Demo, Normal, Quick }
    public enum JourneyPhase { Start, AtStation, Moving, Revealing, Panorama, AtSpawn }

    [Serializable]
    public class MaterialDefinition
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        [Min(0)] public int startingCount = 18;
    }

    [CreateAssetMenu(menuName = "Hiking/Journey Config")]
    public class JourneyConfig : ScriptableObject
    {
        [Header("自动旅途节奏")]
        [InspectorName("每块移动时间（秒）"), Min(.1f)] public float tileMoveSeconds = 5;
        [InspectorName("每块休息时间（秒）"), Min(0)] public float tileRestSeconds = 5;
        [Header("每站等待时间（真实秒数，仅新旅途生效）")]
        [HideInInspector] [Min(0.1f)] public float normalWaitSeconds = 300;
        [HideInInspector] [Min(0.1f)] public float quickWaitSeconds = 30;
        [HideInInspector] [Min(0.1f)] public float demoWaitSeconds = 3;
        [Header("行走时间（每次出发，独立于行动点恢复）")]
        [HideInInspector] [InspectorName("默认行走时长（秒）"), Min(.1f)] public float travelSeconds = 10;
        [Tooltip("按目的站顺序填写；未填写或为 0 时使用默认时长。下标 0 对应第 1 站（直接出生，无需行走）；下标 1 对应进入第 2 站。")]
        [HideInInspector] [InspectorName("各站行走时长覆盖（秒）")] public float[] stationTravelSeconds = new float[0];
        [HideInInspector] [InspectorName("最后到终点的时长（秒）"), Min(.1f)] public float finishTravelSeconds = 10;
        public float TravelSeconds(int destination, int stationCount) => destination >= stationCount ? Mathf.Max(.1f, finishTravelSeconds) :
            stationTravelSeconds != null && destination < stationTravelSeconds.Length && stationTravelSeconds[destination] > 0 ?
                Mathf.Max(.1f, stationTravelSeconds[destination]) : Mathf.Max(.1f, travelSeconds);
        [Header("站内移动与镜头")]
        [HideInInspector] [InspectorName("镜头显示地块数"), Min(1)] public int visibleTileCount = 7;
        [HideInInspector] [InspectorName("站内移动速度（地块/秒）"), Min(.1f)] public float walkTilesPerSecond = 3;
        [Header("镜头")]
        [HideInInspector] [Min(0.1f)] public float revealSeconds = 2;
        [HideInInspector] [Min(1)] public float localCameraSize = 4.8f;
        [HideInInspector] [Min(1)] public float panoramaCameraSize = 7;
        [HideInInspector] [InspectorName("滚轮横移速度（地块/格）"), Min(0.1f)] public float wheelDistance = 2;
        [HideInInspector] [InspectorName("滚轮平滑时间（秒）"), Min(.01f)] public float wheelSmoothTime = .2f;
        [HideInInspector] [InspectorName("回到赤狐平滑时间（秒）"), Min(.01f)] public float returnToTravelerSmoothTime = .35f;
        public MaterialDefinition[] materials =
        {
            new MaterialDefinition { id = "water", displayName = "水", color = new Color(.28f, .7f, .95f) },
            new MaterialDefinition { id = "seed", displayName = "种子", color = new Color(.62f, .82f, .32f) }
        };

        public float WaitSeconds(JourneyMode mode) => Mathf.Max(.1f,
            mode == JourneyMode.Normal ? normalWaitSeconds : mode == JourneyMode.Quick ? quickWaitSeconds : demoWaitSeconds);
    }
}
