using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hiking.Journey
{
    // 材料栏自身的显示与点击，挂在 MaterialBar Prefab 的根节点上。
    // 引用保存在 Prefab 内部，合并到别的场景时不需要重新拖引用。
    // 点击在这里用代码注册：Prefab 资产无法保存对场景对象的引用，写成持久化 onClick 会变成 null。
    public class MaterialBarView : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string materialId;
            public Button button;
            public Text label;
        }

        public Entry[] entries;
        GameFlowController flow;

        public void Bind(GameFlowController owner)
        {
            flow = owner;
            if (entries == null) return;
            foreach (var entry in entries)
            {
                var captured = entry;
                if (captured.button == null) continue;
                captured.button.onClick.AddListener(() => flow.Placement.Select(captured.materialId));
            }
        }

        void LateUpdate()
        {
            if (flow == null || entries == null) return;
            var session = flow.Session;
            var theme = flow.UI.theme;
            bool stationary = !flow.IsPreviewing && session.Phase == JourneyPhase.AtStation;

            foreach (var entry in entries)
            {
                if (entry.button == null) continue;
                var material = flow.Material(entry.materialId);
                if (material == null)
                {
                    entry.button.interactable = false;
                    if (entry.label != null) entry.label.text = "未配置材料";
                    continue;
                }

                int stock = session.Stock(material.id);
                entry.button.interactable = stationary;
                bool selected = flow.Placement.SelectedMaterialId == material.id;
                if (entry.label != null)
                {
                    entry.label.text = (selected ? "✓ " : "") + material.displayName + "  × " + stock;
                    entry.label.color = stock > 0 ? theme.textPrimary : theme.textDisabled;
                }
                if (entry.button.targetGraphic != null)
                    entry.button.targetGraphic.color = stock > 0
                        ? (selected ? theme.materialSelected : theme.materialNormal)
                        : theme.materialEmpty;
            }
        }
    }
}
