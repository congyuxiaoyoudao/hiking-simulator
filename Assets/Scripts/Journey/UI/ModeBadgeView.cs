using UnityEngine;
using UnityEngine.UI;

namespace Hiking.Journey
{
    // 当前模式的角标，常驻 Canvas 左上角（需求 DEV36：创建旅程后界面明确标记当前模式）。
    // 根节点始终激活，只切换子节点的显隐——脚本挂在根上，把自己 SetActive(false) 之后就再也醒不过来了。
    public class ModeBadgeView : MonoBehaviour
    {
        [Tooltip("关闭后角标不显示，对应需求 DEV36 的 modeBadgeVisible。")]
        public bool modeBadgeVisible = true;
        public Text label;
        GameFlowController flow;

        public void Bind(GameFlowController owner) => flow = owner;

        void LateUpdate()
        {
            if (flow == null || label == null) return;

            bool show = modeBadgeVisible && flow.Session.Phase != JourneyPhase.Start;
            if (label.gameObject.activeSelf != show)
            {
                label.gameObject.SetActive(show);
                if (!show) return;
            }
            if (!show) return;

            label.text = GameUIController.ModeName(flow.Mode);
            label.color = flow.Mode == JourneyMode.Quick ? flow.UI.theme.quickModeAccent : flow.UI.theme.textPrimary;
        }
    }
}
