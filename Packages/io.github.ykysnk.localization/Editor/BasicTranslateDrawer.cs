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
            GlobalLocalization.DefaultHelper.UILocalize(tree, false);
            tree.BindProperty(property);

            var keyField = tree.Q<TextField>("key");
            var translateField = tree.Q<TextField>("translate");
            var tooltipField = tree.Q<TextField>("tooltip");
            var tooltipToggle = tree.Q<Button>("tooltipToggle");
            var tooltipProperty = property.FindPropertyRelative("tooltip");

            // Tooltips are usually unused, so the field stays collapsed until the entry has one or the toggle is
            // pressed. The class on the toggle shows that an entry carries a tooltip while it is collapsed.
            bool? expandedOverride = null;

            void ApplyTooltipVisibility()
            {
                var hasTooltip = !string.IsNullOrEmpty(tooltipField.value) ||
                                 !string.IsNullOrEmpty(tooltipProperty.stringValue);
                var open = expandedOverride ?? false;

                tooltipField.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                tooltipToggle.EnableInClassList("translate-tooltip-toggle--active", hasTooltip);
            }

            tooltipToggle.clicked += () =>
            {
                expandedOverride = !(expandedOverride ?? false);
                if (expandedOverride.Value)
                    tooltipField.schedule.Execute(() => tooltipField.Focus());

                ApplyTooltipVisibility();
            };

            tooltipField.RegisterValueChangedCallback(_ => ApplyTooltipVisibility());

            ApplyTooltipVisibility();

            var copyButton = tree.Q<Button>("copy");
            copyButton.clicked += () =>
            {
                var copyBasicTranslate = new BasicTranslate
                {
                    key = keyField.value,
                    translate = translateField.value,
                    tooltip = tooltipField.value
                };

                EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(copyBasicTranslate);
            };

            var pasteButton = tree.Q<Button>("paste");
            pasteButton.schedule.Execute(() =>
            {
                pasteButton.SetEnabled(InternalLocalizationExtensions.TryPasteTranslate(out _, out _));
            }).Every(1000);

            pasteButton.clicked += () =>
            {
                if (InternalLocalizationExtensions.TryPasteTranslate(out var pasteBasicTranslate, out var exception))
                {
                    keyField.value = pasteBasicTranslate.key;
                    translateField.value = pasteBasicTranslate.translate;
                    tooltipField.value = pasteBasicTranslate.tooltip;
                }
                else
                    Utils.LogWarning(nameof(BasicTranslateDrawer),
                        $"Failed to paste: {exception?.Message}\n{exception?.StackTrace}");
            };
            return tree;
        }
    }
}
