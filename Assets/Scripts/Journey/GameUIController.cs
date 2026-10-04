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
        public Text status, hint, toast, departText, modeText;
        public Button startButton, modeButton, depart, returnButton;
        public Image progress;
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
            foreach (var label in panoramaPanel.GetComponentsInChildren<Text>(true))
                if (label.text.Contains("旅途完成")) { panoramaTitle = label; break; }
            UpdateModeLabel();
        }
        public void StartJourney() { if (flow != null) flow.StartJourney(mode); }
        public void Depart() { if (flow != null) flow.Depart(); }
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
            bool atSpawn = session.Phase == JourneyPhase.AtSpawn;
            status.text = atSpawn ? "出生点 · 0% · 等待出发" : session.Phase == JourneyPhase.Revealing ? "旅途完成 · 100% · 正在展开全景…" :
                $"第 {session.StationIndex + 1} / {session.StationCount} 站 · {(stationary ? "驻足" : "行走中")} · {flow.Traveler.Progress01:P0}";
            depart.interactable = session.Ready;
            departText.text = atSpawn ? "出发到第 1 站" : stationary ? (session.Ready ? (session.StationIndex == session.StationCount - 1 ? "出发去观景终点" : "出发到下一站") : $"恢复中 {System.Math.Ceiling(session.RemainingSeconds)} 秒") : "旅途中";
            progress.rectTransform.anchorMax = new Vector2(session.WaitSeconds > 0 ? 1 - (float)(session.RemainingSeconds / session.WaitSeconds) : 0, 1);
            foreach (var view in materialButtons)
            {
                var material = flow.Material(view.materialId);
                view.button.interactable = stationary && material != null;
                if (material == null) { view.label.text = "未配置材料"; continue; }
                bool selected = flow.Placement.SelectedMaterialId == material.id;
                view.label.text = (selected ? "✓ " : "") + material.displayName + "  × " + session.Stock(material.id);
                if (view.button.targetGraphic != null)
                    view.button.targetGraphic.color = selected ? view.selectedColor : view.normalColor;
            }
            if (hint != null) hint.text = stationary && flow.Placement.SelectedMaterialId != null ? "点击高亮位置投放 · 右键取消" : "";
            if (toast != null && Time.unscaledTime > toastUntil) toast.text = "";
        }
        public void Notify(string message)
        {
            if (toast == null) return;
            toast.text = message; toastUntil = Time.unscaledTime + 2.5f;
        }
        static string ModeName(JourneyMode mode) => mode == JourneyMode.Demo ? "开发演示" : mode == JourneyMode.Normal ? "普通模式" : "快速模式";

    }
}

