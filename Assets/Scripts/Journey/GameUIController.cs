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
        public GameObject toastRoot;
        public Text status, hint, toast, departText;
        public Button depart, returnButton;
        public Image progress;
        [Header("样式")]
        public UITheme theme = new UITheme();
        float toastUntil;
        Text panoramaTitle;
        public JourneyMode mode = JourneyMode.Normal;

        public void Initialize(GameFlowController owner)
        {
            flow = owner;
            if (toastRoot != null)
            {
                var background = toastRoot.GetComponent<Image>();
                if (background != null) background.color = theme.toastBackground;
            }
            if (toast != null) toast.color = theme.toastText;
            foreach (var label in panoramaPanel.GetComponentsInChildren<Text>(true))
                if (label.text.Contains("旅途完成")) { panoramaTitle = label; break; }
            // 面板自己管自己：材料栏与开始界面的引用和点击都留在各自的 Prefab 里，
            // 合并到别的场景时不需要重新拖引用。
            foreach (var view in GetComponentsInChildren<MaterialBarView>(true)) view.Bind(flow);
            foreach (var view in GetComponentsInChildren<ModeSelectorView>(true)) view.Bind(flow);
            foreach (var view in GetComponentsInChildren<ModeBadgeView>(true)) view.Bind(flow);
            ApplyButtonFeedback();
        }

        // 统一的悬停／按下／禁用反馈。数值集中在 UITheme，新加的按钮会自动套用。
        void ApplyButtonFeedback()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(theme.hoverBrightness, theme.hoverBrightness, theme.hoverBrightness, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(theme.pressedBrightness, theme.pressedBrightness, theme.pressedBrightness, 1f);
                colors.disabledColor = new Color(theme.disabledBrightness, theme.disabledBrightness, theme.disabledBrightness, theme.disabledAlpha);
                colors.fadeDuration = theme.feedbackFadeSec;
                button.colors = colors;
            }
        }
        public void StartJourney() { if (flow != null) flow.StartJourney(mode); }
        public void Depart() { if (flow != null) flow.Depart(); }
        public void ReturnToStart() { if (flow != null) { if (flow.IsPreviewing) flow.ExitPreview(); else flow.ReturnToStart(); } }
        public void PreviewPanorama() { if (flow != null) flow.PreviewPanorama(); }
        public void SelectMaterial(string id) { if (flow != null) flow.Placement.Select(id); }
        // 开始界面的三个模式按钮：选中项高亮，信息行显示当前等待时长（DEV36）。
        public void SetMode(JourneyMode value)
        {
            if (flow == null || flow.Session.Phase != JourneyPhase.Start) return;
            mode = value;
        }

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
            departText.color = session.Ready ? theme.textPrimary : theme.textDisabled;
            progress.rectTransform.anchorMax = new Vector2(session.WaitSeconds > 0 ? 1 - (float)(session.RemainingSeconds / session.WaitSeconds) : 0, 1);
            if (hint != null)
            {
                var selected = flow.Material(flow.Placement.SelectedMaterialId);
                hint.text = !stationary || selected == null ? "" :
                    session.Stock(selected.id) > 0 ? "点击高亮位置投放 · 右键取消" : "该材料库存不足";
            }
            if (Time.unscaledTime > toastUntil)
            {
                if (toastRoot != null) toastRoot.SetActive(false);
                if (toast != null) toast.text = "";
            }
        }
        public void Notify(string message)
        {
            if (toast == null) return;
            toast.text = message;
            if (toastRoot != null) toastRoot.SetActive(!string.IsNullOrEmpty(message));
            toastUntil = Time.unscaledTime + theme.toastSec;
        }
        public static string ModeName(JourneyMode mode) => mode == JourneyMode.Demo ? "开发演示" : mode == JourneyMode.Normal ? "普通模式" : "快速模式";

    }
}

