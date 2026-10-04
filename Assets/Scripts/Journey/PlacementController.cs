using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hiking.Journey
{
    public class PlacementController : MonoBehaviour
    {
        public string SelectedMaterialId { get; private set; }
        public event Action<PlacementRequest> PlacementCommitted;
        GameFlowController flow;
        public void Initialize(GameFlowController owner) => flow = owner;

        public void Select(string id)
        {
            if (flow.IsPreviewing || flow.Session.Phase != JourneyPhase.AtStation) return;
            SelectedMaterialId = SelectedMaterialId == id ? null : id;
            RefreshHighlights();
        }
        public void ClearSelection() { SelectedMaterialId = null; RefreshHighlights(); }
        public void RefreshHighlights()
        {
            if (flow == null || flow.Map == null) return;
            var material = flow.Material(SelectedMaterialId);
            for (int i = 0; i < flow.Map.stations.Length; i++)
                foreach (var slot in flow.Map.stations[i].slots)
                    slot.Highlight(flow.Session.Phase == JourneyPhase.AtStation && i == flow.Session.StationIndex && material != null,
                        material != null ? material.color : Color.white);
        }
        void Update()
        {
            if (flow == null || flow.IsPreviewing || flow.Session.Phase != JourneyPhase.AtStation) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame ||
                Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ClearSelection();
            var mouse = Mouse.current;
            if (mouse == null || SelectedMaterialId == null || !mouse.leftButton.wasPressedThisFrame) return;
            var position = mouse.position.ReadValue();
            if (PointerUtility.OverUI(position)) return;
            var world = flow.Camera.WorldCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, 10));
            var hit = Physics2D.OverlapPoint(world);
            ApplyPlacement(hit != null ? hit.GetComponent<PlacementSlot>() : null);
        }

        public bool ApplyPlacement(PlacementSlot slot)
        {
            if (flow.IsPreviewing || flow.Session.Phase != JourneyPhase.AtStation || flow.Map == null) return false;
            var station = flow.Map.stations[flow.Session.StationIndex];
            if (slot == null || Array.IndexOf(station.slots, slot) < 0 || !flow.Map.IsVisible(slot))
            { flow.UI.Notify("请选择当前站的三个投放点"); return false; }
            var material = flow.Material(SelectedMaterialId);
            if (material == null) return false;
            var request = new PlacementRequest(flow.Session.StationIndex, slot.slotId, material.id);
            if (!flow.Session.TryPlace(request))
            { flow.UI.Notify("材料不足"); return false; }
            slot.ShowPlacement(material, 1);
            // Extension point: effects/simulation consume the accepted request, never the preview.
            PlacementCommitted?.Invoke(request);
            flow.UI.Notify("已投放：" + material.displayName);
            return true;
        }
    }
}
