using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hiking.Journey
{
    public class PlacementController : MonoBehaviour
    {
        public string SelectedMaterialId { get; private set; }
        public int SelectedQuantity { get; private set; } = 1;
        public event Action<PlacementRequest> PlacementCommitted;
        [Tooltip("单次投放的份数上限，对应需求 DEV07 的 maxPlaceCount。")]
        [Min(1)] public int maxPlaceCount = 5;
        GameFlowController flow;
        public void Initialize(GameFlowController owner) => flow = owner;

        public void Select(string id)
        {
            if (flow.IsPreviewing || flow.Session.Phase != JourneyPhase.AtStation) return;
            string next = SelectedMaterialId == id ? null : id;
            if (next != SelectedMaterialId) SelectedQuantity = 1;
            SelectedMaterialId = next;
            RefreshHighlights();
        }
        public void ClearSelection() { SelectedMaterialId = null; RefreshHighlights(); }

        // 当前材料一次最多能投几份：取配置上限与库存的较小值，至少 1。
        public int QuantityLimit()
        {
            if (flow == null || SelectedMaterialId == null) return 1;
            return Mathf.Clamp(Mathf.Min(maxPlaceCount, flow.Session.Stock(SelectedMaterialId)), 1, int.MaxValue);
        }
        // 调整"准备投放"的份数（需求 DEV07）。没有选材料时不动。
        public void AdjustQuantity(int delta)
        {
            if (flow == null || SelectedMaterialId == null) return;
            SelectedQuantity = Mathf.Clamp(SelectedQuantity + delta, 1, QuantityLimit());
        }
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
            var station = flow.Map != null && flow.Map.stations != null
                ? flow.Map.stations[flow.Session.StationIndex] : null;
            if (station == null || station.slots == null || station.slots.Length == 0) return;
            // 三个投放点在同一 z 平面上，按这个平面求交才能得到准确落点。
            float planeZ = station.slots[0] != null ? station.slots[0].transform.position.z : 0f;
            var world = flow.Camera.ScreenToPointOnPlane(position, planeZ);
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
            int quantity = Mathf.Clamp(SelectedQuantity, 1, QuantityLimit());
            var request = new PlacementRequest(flow.Session.StationIndex, slot.slotId, material.id, quantity);
            if (!flow.Session.TryPlace(request))
            { flow.UI.Notify("材料不足"); return false; }
            slot.ShowPlacement(material, quantity);
            // 每次提交后回到 1 份，避免下一次点击误投一大堆；材料选择本身保留（DEV09）。
            SelectedQuantity = 1;
            // Extension point: effects/simulation consume the accepted request, never the preview.
            PlacementCommitted?.Invoke(request);
            flow.UI.Notify("已投放：" + material.displayName + " ×" + quantity);
            return true;
        }
    }
}
