using UnityEngine;
using UnityEngine.UI;

namespace Hiking.Journey
{
    // 番茄钟的界面与计时，挂在 Canvas/FocusTimer 这个 Prefab 上。
    // 它不挂在任何面板下面，所以切换开始界面／旅途／全景都不会中断计时（需求 DEV31）。
    public class FocusTimerView : MonoBehaviour
    {
        [Tooltip("一轮专注的时长（秒），需求 DEV31 的 focusDurationSec。")]
        [Min(1f)] public float focusDurationSec = 1500f;
        [Tooltip("读秒刷新间隔（秒），需求 DEV31 的 timerUiRefreshSec。")]
        [Min(.05f)] public float refreshSec = .25f;
        public Button toggle;
        public Text status;
        public Text action;

        readonly FocusSession session = new FocusSession();
        float nextRefresh;

        public FocusSession Session => session;

        void Awake()
        {
            if (toggle != null) toggle.onClick.AddListener(OnToggle);
            Refresh();
        }

        void Update()
        {
            if (session.Tick(Time.realtimeSinceStartupAsDouble)) OnFinished();
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + refreshSec;
                Refresh();
            }
        }

        void OnToggle()
        {
            if (session.Running) session.Cancel();
            else session.Start(Time.realtimeSinceStartupAsDouble, focusDurationSec);
            Refresh();
        }

        void OnFinished()
        {
            // DEV32（完成时生成待领补给）还没做，这里先只给一个提示。
            var ui = FindFirstObjectByType<GameUIController>();
            if (ui != null) ui.Notify("专注完成");
        }

        void Refresh()
        {
            if (action != null) action.text = session.Running ? "取消专注" : "开始专注";
            if (status != null)
                status.text = session.Running
                    ? "专注中 " + Format(session.RemainingSeconds)
                    : "番茄钟 " + Format(focusDurationSec);
        }

        static string Format(double seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
