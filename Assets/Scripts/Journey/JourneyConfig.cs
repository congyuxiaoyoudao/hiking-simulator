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
        [Header("每站等待时间（真实秒数，仅新旅途生效）")]
        [Min(0.1f)] public float normalWaitSeconds = 300;
        [Min(0.1f)] public float quickWaitSeconds = 30;
        [Min(0.1f)] public float demoWaitSeconds = 3;
        [Header("行走时间（每次出发，独立于行动点恢复）")]
        [InspectorName("默认行走时长（秒）"), Min(.1f)] public float travelSeconds = 10;
        [Tooltip("按目的站顺序填写；未填写或为 0 时使用默认时长。第一个元素为出生点到第 1 站。")]
        [InspectorName("各站行走时长覆盖（秒）")] public float[] stationTravelSeconds = new float[0];
        [InspectorName("最后到终点的时长（秒）"), Min(.1f)] public float finishTravelSeconds = 10;
        public float TravelSeconds(int destination, int stationCount) => destination >= stationCount ? Mathf.Max(.1f, finishTravelSeconds) :
            stationTravelSeconds != null && destination < stationTravelSeconds.Length && stationTravelSeconds[destination] > 0 ?
                Mathf.Max(.1f, stationTravelSeconds[destination]) : Mathf.Max(.1f, travelSeconds);
        [Header("镜头")]
        [Min(0.1f)] public float revealSeconds = 2;
        [Min(1)] public float localCameraSize = 4.8f;
        [Min(1)] public float panoramaCameraSize = 7;
        [Min(0.1f)] public float wheelDistance = 2;
        public MaterialDefinition[] materials =
        {
            new MaterialDefinition { id = "water", displayName = "水", color = new Color(.28f, .7f, .95f) },
            new MaterialDefinition { id = "seed", displayName = "种子", color = new Color(.62f, .82f, .32f) }
        };

        public float WaitSeconds(JourneyMode mode) => Mathf.Max(.1f,
            mode == JourneyMode.Normal ? normalWaitSeconds : mode == JourneyMode.Quick ? quickWaitSeconds : demoWaitSeconds);
    }
}
