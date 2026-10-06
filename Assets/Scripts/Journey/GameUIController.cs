using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hiking.Journey
{
    public class GameUIController : MonoBehaviour
    {
        GameFlowController flow;
        [Header("界面面板：布局直接在 Canvas 中编辑")]
        public GameObject startPanel, journeyPanel, panoramaPanel;
        [Header("站点末尾确认")]
        public GameObject exitPrompt;
        public Text exitPromptText;
        public Button confirmExit, cancelExit;
        public bool ExitPromptOpen => exitPrompt != null && exitPrompt.activeSelf;
        public void CloseExitPrompt() { if (exitPrompt != null) exitPrompt.SetActive(false); }
        public void ShowExitPrompt()
        {
            if (PlacementPromptOpen || flow == null || !flow.AtStationEnd || flow.IsPreviewing || flow.Session.Phase != JourneyPhase.AtStation) return;
            if (exitPrompt != null) exitPrompt.SetActive(true);
        }
        [Header("投放确认")]
        public GameObject placementPrompt;
        public Text placementPromptText;
        public Button confirmPlacement, cancelPlacement;
        public MaterialButton[] placementMaterialButtons;
        public bool PlacementPromptOpen => placementPrompt != null && placementPrompt.activeSelf;
        public bool IsModalOpen => ExitPromptOpen || PlacementPromptOpen;
        public void ShowPlacementPrompt()
        {
            if (!flow.Placement.IsAtSlot(flow.Placement.PendingSlot) || ExitPromptOpen) return;
            if (placementPrompt != null) placementPrompt.SetActive(true);
        }
        public void ClosePlacementPrompt(bool offerExit = true)
        {
            if (placementPrompt != null) placementPrompt.SetActive(false);
            flow.Placement.ClearSelection();
            if (offerExit && flow.AtStationEnd) ShowExitPrompt();
        }
        public Text status, hint, toast, departText, modeText;
        public Button startButton, modeButton, depart, returnButton;
        public Image progress;
        public Button returnToTravelerButton;
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
            if (confirmPlacement != null) confirmPlacement.onClick.AddListener(() => flow.Placement.ConfirmPlacement());
            if (cancelPlacement != null) cancelPlacement.onClick.AddListener(() => ClosePlacementPrompt());
            if (placementMaterialButtons != null)
                foreach (var view in placementMaterialButtons)
                {
                    var id = view.materialId;
                    view.button.onClick.AddListener(() => flow.Placement.Select(id));
                }
            ClosePlacementPrompt(false);
            if (confirmExit != null) confirmExit.onClick.AddListener(() => flow.Depart());
            if (cancelExit != null) cancelExit.onClick.AddListener(CloseExitPrompt);
            CloseExitPrompt();
            if (returnToTravelerButton != null)
            {
                returnToTravelerButton.onClick.AddListener(() => flow.Camera.ReturnToTraveler());
                returnToTravelerButton.gameObject.SetActive(false);
            }
            foreach (var label in panoramaPanel.GetComponentsInChildren<Text>(true))
                if (label.text.Contains("旅途完成")) { panoramaTitle = label; break; }
            UpdateModeLabel();
        }
        public void StartJourney() { if (flow != null) flow.StartJourney(mode); }
        public void Depart() { ShowExitPrompt(); }
        public void ReturnToStart() { if (flow != null) { if (flow.IsPreviewing) flow.ExitPreview(); else flow.ReturnToStart(); } }
        public void PreviewPanorama() { if (flow != null) flow.PreviewPanorama(); }
        public void SelectMaterial(string id) { if (flow != null) flow.Placement.Select(id); }
        public void CycleMode()
        {
            if (flow == null || flow.Session.Phase != JourneyPhase.Start) return;
            mode = (JourneyMode)(((int)mode + 1) % 3);
            UpdateModeLabel();
        }
        void UpdateModeLabel() => modeText.text = ModeName(mode) + " · " + flow.Config.WaitSeconds(mode) + " 秒";

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
            bool stationary = !flow.IsPreviewing && session.Phase == JourneyPhase.AtStation;
            if (returnToTravelerButton != null)
                returnToTravelerButton.gameObject.SetActive(stationary && !IsModalOpen && flow.Camera.HasManualPan);
            bool atSpawn = session.Phase == JourneyPhase.AtSpawn;
            status.text = atSpawn ? "出生点 · 0% · 等待出发" : session.Phase == JourneyPhase.Revealing ? "旅途完成 · 100% · 正在展开全景…" :
                $"第 {session.StationIndex + 1} / {session.StationCount} 站 · {(stationary ? "驻足" : "行走中")} · {flow.Traveler.Progress01:P0}";
            if (stationary) status.text = $"第 {session.StationIndex + 1} / {session.StationCount} 站 · {flow.CurrentStation.ThemeName} · 已驻留 {Math.Floor(session.ResidenceSeconds)} 秒";
            depart.interactable = stationary && !PlacementPromptOpen && flow.AtStationEnd;
            departText.text = stationary ? (flow.AtStationEnd ? "查看离站选项" : "请走到本站最后一块") : "旅途中";
            if (ExitPromptOpen)
            {
                bool last = session.StationIndex == session.StationCount - 1;
                exitPromptText.text = $"本站已驻留 {Math.Floor(session.ResidenceSeconds)} 秒\n" +
                    (session.Ready ? (last ? "已达到最短停留时间，完成旅途？" : "已达到最短停留时间，前往下一站？") :
                    $"最短停留 {Math.Ceiling(session.WaitSeconds)} 秒，还需 {Math.Ceiling(session.RemainingSeconds)} 秒");
                confirmExit.interactable = flow.CanDepart;
            }
            progress.rectTransform.anchorMax = new Vector2(session.WaitSeconds > 0 ? 1 - (float)(session.RemainingSeconds / session.WaitSeconds) : 0, 1);
            bool atSlot = stationary && flow.Placement.CurrentSlot != null && !ExitPromptOpen;
            UpdateMaterials(materialButtons, atSlot);
            UpdateMaterials(placementMaterialButtons, PlacementPromptOpen && atSlot);
            if (PlacementPromptOpen)
            {
                var slot = flow.Placement.PendingSlot;
                if (slot != null)
                {
                    var selected = flow.Material(flow.Placement.SelectedMaterialId);
                    string action = selected == null ? "选择材料后确认，每次消耗 1 份" :
                        flow.Session.Stock(selected.id) <= 0 ? "该材料库存不足，请选择其他材料" :
                        selected.id == "water" ? "确认投水：本地块水分 +1" : "确认投种：本投放点种子 +1";
                    placementPromptText.text = $"是否投放？ · 第 {slot.tileIndex + 1} 块\n水分 {slot.station.MoistureAt(slot.tileIndex)} · 种子 {slot.SeedCount}\n{action}";
                }
                confirmPlacement.interactable = flow.Placement.CanConfirm;
            }
            if (hint != null) hint.text = !stationary ? "" : atSlot ? "到达投放点 · 可选择水或种子投放" :
                "点击地块移动 · 到白框投放点后选择材料 · 地块下方显示水分，框内数字为种子";
            if (toast != null && Time.unscaledTime > toastUntil) toast.text = "";
        }
        void UpdateMaterials(MaterialButton[] views, bool available)
        {
            if (views == null) return;
            foreach (var view in views)
            {
                var material = flow.Material(view.materialId);
                int stock = flow.Session.Stock(view.materialId);
                view.button.interactable = available && material != null && stock > 0;
                if (material == null) { view.label.text = "未配置材料"; continue; }
                bool selected = flow.Placement.SelectedMaterialId == material.id;
                view.label.text = (selected ? "✓ " : "") + material.displayName + " × " + stock;
                if (view.button.targetGraphic != null)
                    view.button.targetGraphic.color = selected ? view.selectedColor : view.normalColor;
            }
        }
        public void Notify(string message)
        {
            if (toast == null) return;
            toast.text = message; toastUntil = Time.unscaledTime + 2.5f;
        }
        static string ModeName(JourneyMode mode) => mode == JourneyMode.Demo ? "开发演示" : mode == JourneyMode.Normal ? "普通模式" : "快速模式";

    }
}

