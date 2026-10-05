using System;
using UnityEngine;

namespace Hiking.Journey
{
    // 界面配色与提示参数的唯一来源，避免颜色散落在脚本里（需求 DEV40）。
    // 字号与布局仍直接在 Canvas / Prefab 中编辑，这里只放运行时由脚本写入的值。
    [Serializable]
    public class UITheme
    {
        [Header("材料按钮")]
        public Color materialNormal = new Color(.17f, .29f, .31f);
        public Color materialSelected = new Color(.28f, .49f, .43f);
        public Color materialEmpty = new Color(.12f, .16f, .18f);

        [Header("模式选择")]
        public Color modeNormal = new Color(.17f, .29f, .31f);
        public Color modeSelected = new Color(.28f, .49f, .43f);
        [Tooltip("快速模式角标的强调色，让玩家一眼看出当前不是普通模式。")]
        public Color quickModeAccent = new Color(1f, .78f, .35f);

        [Header("提示条")]
        public Color toastBackground = new Color(.07f, .14f, .17f, .78f);
        public Color toastText = new Color(.92f, .95f, .89f);
        [Tooltip("提示停留时长（秒），对应需求 DEV12 的 toastSec。")]
        [Min(.5f)] public float toastSec = 2.5f;

        [Header("文字")]
        [Tooltip("可用状态下的文字颜色。")]
        public Color textPrimary = new Color(.92f, .95f, .89f);
        [Tooltip("禁用／不可用状态下的文字颜色。")]
        public Color textDisabled = new Color(.47f, .54f, .52f);

        [Header("按钮状态反馈")]
        [Tooltip("鼠标悬停时的亮度倍率（相对按钮底色）。")]
        [Range(1f, 2f)] public float hoverBrightness = 1.3f;
        [Tooltip("按下时的亮度倍率。")]
        [Range(.5f, 1f)] public float pressedBrightness = .78f;
        [Tooltip("禁用时的亮度倍率。")]
        [Range(.3f, 1f)] public float disabledBrightness = .6f;
        [Tooltip("禁用时的不透明度。")]
        [Range(.2f, 1f)] public float disabledAlpha = .7f;
        [Tooltip("状态切换的过渡时间（秒），0 为瞬间切换。")]
        [Min(0f)] public float feedbackFadeSec = .08f;
    }
}
