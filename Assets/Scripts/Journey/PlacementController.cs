using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hiking.Journey
{
    public class PlacementController : MonoBehaviour
    {
        public string SelectedMaterialId { get; private set; }
        public PlacementSlot PendingSlot { get; private set; }
        public event Action<PlacementRequest> PlacementCommitted;
        GameFlowController flow;
        public void Initialize(GameFlowController owner) => flow = owner;
        public bool IsAtSlot(PlacementSlot slot) => flow.Map != null && !flow.IsPreviewing &&
            flow.Session.Phase == JourneyPhase.AtStation && !flow.Traveler.IsMoving && slot != null &&
            slot.station == flow.CurrentStation && Array.IndexOf(flow.CurrentStation.slots, slot) >= 0 &&
            Mathf.Abs(flow.Map.RouteDistanceAtProgress(flow.Traveler.Progress01) - slot.station.TileCenter(slot.tileIndex)) < .001f;
        public PlacementSlot CurrentSlot
        {
            get
            {
                if (flow.Map == null) return null;
                return Array.Find(flow.CurrentStation.slots, IsAtSlot);
            }
        }
        public void OnTravelerArrived()
        {
            var slot = CurrentSlot;
            if (slot != null)
            {
                ClearSelection(); PendingSlot = slot; flow.UI.ShowPlacementPrompt();
            }
            else if (flow.AtStationEnd) flow.UI.ShowExitPrompt();
        }
        public void Select(string id)
        {
            var slot = CurrentSlot;
            if (slot == null || flow.UI.ExitPromptOpen || (id != "water" && id != "seed")) return;
            PendingSlot = slot; SelectedMaterialId = id;
            flow.UI.ShowPlacementPrompt(); RefreshHighlights();
        }
        public void ClearSelection() { PendingSlot = null; SelectedMaterialId = null; RefreshHighlights(); }
        public void RefreshHighlights()
        {
            if (flow == null || flow.Map == null) return;
            var material = flow.Material(SelectedMaterialId);
            foreach (var station in flow.Map.stations)
                foreach (var slot in station.slots)
                    slot.Highlight(slot == PendingSlot && IsAtSlot(slot) && material != null,
                        material != null ? material.color : Color.white);
        }
        void Update()
        {
            if (flow == null || !flow.UI.PlacementPromptOpen) return;
            if (!IsAtSlot(PendingSlot)) { flow.UI.ClosePlacementPrompt(false); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame ||
                Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                flow.UI.ClosePlacementPrompt();
        }
        public bool CanConfirm => flow.UI.PlacementPromptOpen && IsAtSlot(PendingSlot) &&
            (SelectedMaterialId == "water" || SelectedMaterialId == "seed") &&
            flow.Material(SelectedMaterialId) != null && flow.Session.Stock(SelectedMaterialId) > 0;
        public void ConfirmPlacement() => ApplyPlacement(PendingSlot);
        public bool ApplyPlacement(PlacementSlot slot)
        {
            if (!CanConfirm || slot != PendingSlot) return false;
            var material = flow.Material(SelectedMaterialId);
            var request = new PlacementRequest(flow.Session.StationIndex, slot.slotId, material.id);
            if (!flow.Session.TryPlace(request)) return false;
            slot.ShowPlacement(material, 1);
            flow.UI.ClosePlacementPrompt();
            PlacementCommitted?.Invoke(request);
            flow.UI.Notify("已投放：" + material.displayName);
            return true;
        }
    }
}
