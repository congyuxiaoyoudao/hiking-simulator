using UnityEngine;

namespace Hiking.Journey
{
    public enum StationTheme { Snow, Grass, Desert }

    public class Station : MonoBehaviour
    {
        public string stationId;
        public StationTheme theme;
        public int tileCount;
        int[] moisture;
        TextMesh[] moistureLabels;
        public void InitializeMoisture(int initial)
        {
            moisture = new int[tileCount]; moistureLabels = new TextMesh[tileCount];
            for (int i = 0; i < tileCount; i++) moisture[i] = initial;
        }
        public int MoistureAt(int tile) => moisture[tile];
        public void SetMoistureLabel(int tile, TextMesh label)
        {
            moistureLabels[tile] = label; RefreshMoisture(tile);
        }
        public void AddWater(int tile, int quantity)
        {
            if (quantity <= 0) return;
            moisture[tile] += quantity; RefreshMoisture(tile);
        }
        void RefreshMoisture(int tile)
        {
            if (moistureLabels[tile] != null) moistureLabels[tile].text = "水分 " + moisture[tile];
        }
        public float startDistance, length;
        public float TileWidth => length / tileCount;
        public float TileCenter(int index) => startDistance + (Mathf.Clamp(index, 0, tileCount - 1) + .5f) * TileWidth;
        public string ThemeName => theme == StationTheme.Snow ? "雪山" : theme == StationTheme.Grass ? "草原" : "沙漠";
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
