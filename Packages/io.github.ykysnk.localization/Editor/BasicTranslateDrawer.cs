using System;
using io.github.ykysnk.utils;
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

            if (uxml == null)
            {
                var errorTree = new VisualElement();
                errorTree.Add(new Label("Failed to load uxml assets, please reimport the package to fix this issue."));
                return errorTree;
            }

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
            pasteButton.clicked += () =>
            {
                try
                {
                    var pasteBasicTranslate = JsonUtility.FromJson<BasicTranslate>(EditorGUIUtility.systemCopyBuffer);
                    keyField.value = pasteBasicTranslate.key;
                    translateField.value = pasteBasicTranslate.translate;
                    tooltipField.value = pasteBasicTranslate.tooltip;
                }
                catch (ArgumentException e)
                {
                    Utils.LogWarning(nameof(BasicTranslateDrawer), $"Failed to paste: {e.Message}\n{e.StackTrace}");
                }
            };
            return tree;
        }
    }
}