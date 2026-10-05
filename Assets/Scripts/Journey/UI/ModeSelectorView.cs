using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hiking.Journey
{
    // 开始界面的模式选择、全程时长提示与开始按钮，挂在 StartPanel Prefab 的根节点上。
    // 与 MaterialBarView 同理：引用和点击都留在 Prefab 内部。
    public class ModeSelectorView : MonoBehaviour
    {
        [Serializable]
        public class Option
        {
            public JourneyMode mode;
            public Button button;
            public Text label;
        }

        public Option[] options;
        public Text info;
        public Button startButton;
        GameFlowController flow;

        public void Bind(GameFlowController owner)
        {
            flow = owner;
            if (options != null)
                foreach (var option in options)
                {
                    var captured = option;
                    if (captured.button == null) continue;
                    captured.button.onClick.AddListener(() => flow.UI.SetMode(captured.mode));
                }
            if (startButton != null)
                startButton.onClick.AddListener(() => flow.StartJourney(flow.UI.mode));
        }

        void LateUpdate()
        {
            if (flow == null) return;
            var ui = flow.UI;

            if (info != null)
            {
                double wait = flow.Config.WaitSeconds(ui.mode);
                int stations = flow.MapProvider != null && flow.MapProvider.ringSettings != null
                    ? flow.MapProvider.ringSettings.blockCount : 0;
                info.text = "每站等待 " + wait + " 秒";
                if (stations > 0)
                {
                    double total = wait * stations;
                    info.text += total >= 60
                        ? " · 全程等待约 " + System.Math.Round(total / 60.0) + " 分钟"
                        : " · 全程等待约 " + System.Math.Round(total) + " 秒";
                }
            }

            if (options == null) return;
            foreach (var option in options)
            {
                bool active = option.mode == ui.mode;
                if (option.label != null)
                    option.label.text = (active ? "✓ " : "") + GameUIController.ModeName(option.mode);
                if (option.button != null && option.button.targetGraphic != null)
                    option.button.targetGraphic.color = active ? ui.theme.modeSelected : ui.theme.modeNormal;
            }
        }
    }
}
