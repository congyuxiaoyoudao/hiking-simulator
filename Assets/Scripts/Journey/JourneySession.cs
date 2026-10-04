using System;
using System.Collections.Generic;

namespace Hiking.Journey
{
    public readonly struct PlacementRequest
    {
        public readonly int StationIndex;
        public readonly string SlotId;
        public readonly string MaterialId;
        public readonly int Quantity;
        public PlacementRequest(int stationIndex, string slotId, string materialId, int quantity = 1)
        { StationIndex = stationIndex; SlotId = slotId; MaterialId = materialId; Quantity = quantity; }
    }

    // Journey rules are independent of scene objects, animation and UI.
    public sealed class JourneySession
    {
        public JourneyPhase Phase { get; private set; } = JourneyPhase.Start;
        public int StationIndex { get; private set; }
        public int StationCount { get; private set; }
        public double RemainingSeconds { get; private set; }
        public double WaitSeconds { get; private set; }
        public bool Ready => Phase == JourneyPhase.AtSpawn || Phase == JourneyPhase.AtStation && RemainingSeconds <= 0;
        public bool ApproachingFirstStation => approachingFirstStation;
        public IReadOnlyList<PlacementRequest> Placements => placements;
        readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
        readonly List<PlacementRequest> placements = new List<PlacementRequest>();
        bool approachingFirstStation;

        public void Begin(int stationCount, double waitSeconds, IEnumerable<MaterialDefinition> materials, bool approachFirstStation = false)
        {
            if (stationCount < 1 || waitSeconds <= 0) throw new ArgumentOutOfRangeException();
            inventory.Clear(); placements.Clear();
            foreach (var material in materials)
                inventory.Add(material.id, Math.Max(0, material.startingCount));
            StationCount = stationCount;
            WaitSeconds = waitSeconds;
            StationIndex = 0;
            approachingFirstStation = approachFirstStation;
            RemainingSeconds = approachFirstStation ? 0 : WaitSeconds;
            Phase = approachFirstStation ? JourneyPhase.AtSpawn : JourneyPhase.AtStation;
        }

        public int Stock(string id) => id != null && inventory.TryGetValue(id, out var count) ? count : 0;
        public void Tick(double seconds)
        {
            if (Phase == JourneyPhase.AtStation && seconds > 0)
                RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
        }
        public bool TryDepart()
        {
            if (!Ready) return false;
            RemainingSeconds = 0;
            Phase = JourneyPhase.Moving;
            return true;
        }
        public bool Arrive()
        {
            if (Phase != JourneyPhase.Moving) return false;
            if (approachingFirstStation)
            {
                approachingFirstStation = false;
                RemainingSeconds = WaitSeconds;
                Phase = JourneyPhase.AtStation;
                return true;
            }
            if (StationIndex == StationCount - 1) Phase = JourneyPhase.Revealing;
            else
            {
                StationIndex++;
                RemainingSeconds = WaitSeconds;
                Phase = JourneyPhase.AtStation;
            }
            return true;
        }
        public bool FinishReveal()
        {
            if (Phase != JourneyPhase.Revealing) return false;
            Phase = JourneyPhase.Panorama;
            return true;
        }
        // Slot membership is validated by PlacementController against the live map.
        public bool TryPlace(PlacementRequest request)
        {
            if (Phase != JourneyPhase.AtStation || request.StationIndex != StationIndex ||
                string.IsNullOrEmpty(request.SlotId) || request.Quantity <= 0 ||
                Stock(request.MaterialId) < request.Quantity) return false;
            inventory[request.MaterialId] -= request.Quantity;
            placements.Add(request);
            return true;
        }
        public void Reset()
        {
            Phase = JourneyPhase.Start; StationIndex = 0; StationCount = 0;
            RemainingSeconds = 0; inventory.Clear(); placements.Clear();
            approachingFirstStation = false;
        }
    }
}
