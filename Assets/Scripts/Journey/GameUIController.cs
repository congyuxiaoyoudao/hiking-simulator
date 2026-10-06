using System;
using UnityEngine;
using UnityEngine.UI;
namespace Hiking.Journey
{
    public class GameUIController : MonoBehaviour
    {
        GameFlowController flow;
        public GameObject startPanel, journeyPanel, panoramaPanel;
        // Retain old serialized references while the scene migrates to automatic travel.
        [HideInInspector] public GameObject exitPrompt;
        [HideInInspector] public Text exitPromptText;
        [HideInInspector] public Button confirmExit, cancelExit;
        public bool ExitPromptOpen => false;
        public void CloseExitPrompt() { if (exitPrompt != null) exitPrompt.SetActive(false); }
        [Header("是否投放")]
        public GameObject placementDecision;
        public Text placementDecisionText;
        public Button choosePlacement, skipPlacement;
        [Header("材料与数量")]
        public GameObject placementPrompt;
        public Text placementPromptText;
        public Button confirmPlacement, cancelPlacement, quantityMinus, quantityPlus;
        public InputField quantityInput;
        public MaterialButton[] placementMaterialButtons;
        public bool PlacementPromptOpen => placementPrompt != null && placementPrompt.activeSelf;
        public bool PlacementDecisionOpen => placementDecision != null && placementDecision.activeSelf;
        public bool IsModalOpen => PlacementDecisionOpen || PlacementPromptOpen;
        public void ShowPlacementDecision()
        {
            if (!flow.Placement.IsAtSlot(flow.Placement.PendingSlot)) return;
            placementPrompt.SetActive(false); placementDecision.SetActive(true);
        }
        public void ShowPlacementPrompt()
        {
            if (!flow.Placement.IsAtSlot(flow.Placement.PendingSlot)) return;
            placementDecision.SetActive(false); placementPrompt.SetActive(true);
        }
        public void ClosePlacementPrompt(bool completeVisit = true)
        {
            if (placementPrompt != null) placementPrompt.SetActive(false);
            if (placementDecision != null) placementDecision.SetActive(false);
            flow.Placement.ClearSelection();
            if (completeVisit) flow.CompletePlacementVisit();
        }
        public Text status, hint, toast, departText, modeText;
        public Button startButton, modeButton, depart, returnButton;
        public Image progress;
        [HideInInspector] public Button returnToTravelerButton;
        [Serializable]
        public class MaterialButton
        {
            public string materialId;
            public Button button;
            public Text label;
            public Color normalColor = new Color(.17f, .29f, .31f);
            public Color selectedColor = new Color(.28f, .49f, .43f);
        }
        public MaterialButton[] materialButtons;
        float toastUntil;
        Text panoramaTitle;
        public JourneyMode mode = JourneyMode.Demo;
        public void Initialize(GameFlowController owner)
        {
            flow = owner;
            confirmPlacement.onClick.AddListener(() => { CommitQuantity(quantityInput.text); flow.Placement.ConfirmPlacement(); });
            cancelPlacement.onClick.AddListener(ShowPlacementDecision);
            choosePlacement.onClick.AddListener(() => flow.Placement.ChooseToPlace());
            skipPlacement.onClick.AddListener(() => flow.Placement.SkipPlacement());
            quantityMinus.onClick.AddListener(() => flow.Placement.AdjustQuantity(-1));
            quantityPlus.onClick.AddListener(() => flow.Placement.AdjustQuantity(1));
            quantityInput.onEndEdit.AddListener(CommitQuantity);
            foreach (var view in placementMaterialButtons)
            {
                var id = view.materialId; view.button.onClick.AddListener(() => flow.Placement.Select(id));
            }
            ClosePlacementPrompt(false); CloseExitPrompt();
            if (depart != null) depart.gameObject.SetActive(false);
            if (modeButton != null) modeButton.gameObject.SetActive(false);
            if (returnToTravelerButton != null) returnToTravelerButton.gameObject.SetActive(false);
            foreach (var label in panoramaPanel.GetComponentsInChildren<Text>(true))
                if (label.text.Contains("旅途完成")) { panoramaTitle = label; break; }
            if (modeText != null) modeText.text = $"自动行走 · 移动 {flow.Config.tileMoveSeconds:0.#} 秒 · 休息 {flow.Config.tileRestSeconds:0.#} 秒";
        }
        void CommitQuantity(string text)
        {
            flow.Placement.SetQuantity(int.TryParse(text, out int value) ? value : 1);
            quantityInput.SetTextWithoutNotify(flow.Placement.SelectedQuantity.ToString());
        }
        public void StartJourney() => flow.StartJourney(mode);
        public void Depart() { }
        public void CycleMode() { }
        public void ReturnToStart() { if (flow.IsPreviewing) flow.ExitPreview(); else flow.ReturnToStart(); }
        public void PreviewPanorama() => flow.PreviewPanorama();
        public void SelectMaterial(string id) => flow.Placement.Select(id);
        void LateUpdate()
        {
            if (flow == null) return;
            var session = flow.Session;
            startPanel.SetActive(session.Phase == JourneyPhase.Start);
            journeyPanel.SetActive(!flow.IsPreviewing && session.Phase != JourneyPhase.Start && session.Phase != JourneyPhase.Panorama);
            panoramaPanel.SetActive(flow.IsPreviewing || session.Phase == JourneyPhase.Panorama);
            if (panoramaTitle != null) panoramaTitle.text = flow.IsPreviewing ? "全景预览" : "旅途完成 · 全景观察";
            var returnLabel = returnButton.GetComponentInChildren<Text>();
            if (returnLabel != null) returnLabel.text = flow.IsPreviewing ? "返回旅途" : "返回开始";
            string activity = flow.Activity == JourneyActivity.Resting ? $"休息 {Math.Ceiling(flow.RestRemainingSeconds)} 秒" :
                flow.Activity == JourneyActivity.AwaitingPlacement ? "等待投放选择" : flow.Activity == JourneyActivity.BetweenStations ? "前往下一站" :
                flow.Activity == JourneyActivity.Complete ? "旅途完成" : "自动行走";
            if (flow.Map != null) status.text = $"第 {session.StationIndex + 1} / {session.StationCount} 站 · {flow.CurrentStation.ThemeName} · 第 {flow.CurrentTileIndex + 1} / 12 块 · {activity}";
            progress.rectTransform.anchorMax = new Vector2(flow.Traveler.Progress01, 1);
            UpdateMaterials(materialButtons, false);
            UpdateMaterials(placementMaterialButtons, PlacementPromptOpen);
            var slot = flow.Placement.PendingSlot;
            if (PlacementDecisionOpen && slot != null)
                placementDecisionText.text = $"已到达第 {slot.tileIndex + 1} 块投放点\n是否投放材料？\n不投放则休息 {flow.Config.tileRestSeconds:0.#} 秒后继续";
            if (PlacementPromptOpen && slot != null)
            {
                int quantity = flow.Placement.SelectedQuantity;
                var selected = flow.Material(flow.Placement.SelectedMaterialId);
                string action = selected == null ? "请选择材料" : flow.Session.Stock(selected.id) == 0 ? "库存不足，可返回选择不投放" :
                    selected.id == "water" ? $"投水 {quantity} 份：当前地块水分 +{quantity}" : $"投种 {quantity} 份：当前投放点种子 +{quantity}";
                placementPromptText.text = $"第 {slot.tileIndex + 1} 块 · 水分 {slot.station.MoistureAt(slot.tileIndex)} · 种子 {slot.SeedCount}\n{action}";
                if (!quantityInput.isFocused) quantityInput.SetTextWithoutNotify(quantity.ToString());
                quantityMinus.interactable = quantity > 1;
                quantityPlus.interactable = quantity < flow.Session.Stock(flow.Placement.SelectedMaterialId);
                confirmPlacement.interactable = flow.Placement.CanConfirm;
            }
            if (hint != null) hint.text = "自动前行 · 白框为投放点 · 灰蓝色为过渡块 · 下方数字为水分，框内数字为种子";
            if (toast != null && Time.unscaledTime > toastUntil) toast.text = "";
        }
        void UpdateMaterials(MaterialButton[] views, bool available)
        {
            if (views == null) return;
            foreach (var view in views)
            {
                var material = flow.Material(view.materialId); int stock = flow.Session.Stock(view.materialId);
                view.button.interactable = available && material != null && stock > 0;
                if (material == null) { view.label.text = "未配置材料"; continue; }
                bool selected = available && flow.Placement.SelectedMaterialId == material.id;
                view.label.text = (selected ? "✓ " : "") + material.displayName + " × " + stock;
                if (view.button.targetGraphic != null) view.button.targetGraphic.color = selected ? view.selectedColor : view.normalColor;
            }
        }
        public void Notify(string message)
        {
            if (toast == null) return; toast.text = message; toastUntil = Time.unscaledTime + 2.5f;
        }
    }
}
