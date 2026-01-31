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
            tree.Bind(property.serializedObject);

            var keyField = tree.Q<TextField>("key");
            var translateField = tree.Q<TextField>("translate");
            var tooltipField = tree.Q<TextField>("tooltip");

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