using io.github.ykysnk.utils;
using io.github.ykysnk.utils.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    [CustomPropertyDrawer(typeof(BasicTranslate))]
    public class BasicTranslateDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var uxml =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    AssetDatabase.GUIDToAssetPath("666a4a69b697dcd45bfe25da3d5636a6"));

            if (uxml == null) return BasicEditor.CreateUxmlImportErrorUI();

            var tree = uxml.CloneTree();
            tree.UILocalize(false);
            tree.BindProperty(property);

            var keyField = tree.Q<TextField>("key");
            var translateField = tree.Q<TextField>("translate");
            var tooltipField = tree.Q<TextField>("tooltip");
            var suffixField = tree.Q<TextField>("tooltipSuffix");
            var useTooltipToggle = tree.Q<Toggle>("useTooltip");

            // BaseField draws [label][checkbox]; this control reads better as [checkbox][label].
            var toggleInput = useTooltipToggle.Q(className: "unity-base-field__input");
            if (toggleInput != null)
                useTooltipToggle.Insert(0, toggleInput);

            // Tooltips are opt-in: while the toggle is off both tooltip fields stay hidden, and the loader ignores the
            // stored text as well.
            void ApplyTooltipVisibility()
            {
                var on = useTooltipToggle.value;
                tooltipField.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                suffixField.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }

            useTooltipToggle.RegisterValueChangedCallback(_ => ApplyTooltipVisibility());
            ApplyTooltipVisibility();
            // The binding sets the initial value after CreatePropertyGUI returns, so re-apply once it is available.
            useTooltipToggle.schedule.Execute(ApplyTooltipVisibility);

            var copyButton = tree.Q<Button>("copy");
            copyButton.clicked += () =>
            {
                var copyBasicTranslate = new BasicTranslate
                {
                    key = keyField.value,
                    translate = translateField.value,
                    tooltip = tooltipField.value,
                    useTooltip = useTooltipToggle.value,
                    tooltipSuffix = suffixField.value
                };

                EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(copyBasicTranslate);
            };

            var pasteButton = tree.Q<Button>("paste");
            pasteButton.schedule.Execute(() =>
            {
                pasteButton.SetEnabled(TranslateClipboard.TryRead(out _, out _));
            }).Every(1000);

            pasteButton.clicked += () =>
            {
                if (TranslateClipboard.TryRead(out var pasteBasicTranslate, out var exception))
                {
                    keyField.value = pasteBasicTranslate.key;
                    translateField.value = pasteBasicTranslate.translate;
                    tooltipField.value = pasteBasicTranslate.tooltip;
                    useTooltipToggle.value = pasteBasicTranslate.useTooltip;
                    suffixField.value = pasteBasicTranslate.tooltipSuffix;
                }
                else
                    Utils.LogWarning(nameof(BasicTranslateDrawer),
                        $"Failed to paste: {exception?.Message}\n{exception?.StackTrace}");
            };
            return tree;
        }
    }
}