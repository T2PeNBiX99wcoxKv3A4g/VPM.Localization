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
            var root = new VisualElement();
            var uxml =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    AssetDatabase.GUIDToAssetPath("666a4a69b697dcd45bfe25da3d5636a6"));

            if (uxml == null)
            {
                root.Add(new Label("Failed to load uxml assets, please reimport the package to fix this issue."));
                return root;
            }

            var visualTree = uxml.CloneTree();
            GlobalLocalization.DefaultHelper.UILocalize(visualTree);
            root.Bind(property.serializedObject);

            var keyField = visualTree.Q<TextField>("key");
            var translateField = visualTree.Q<TextField>("translate");
            var tooltipField = visualTree.Q<TextField>("tooltip");

            var copyButton = visualTree.Q<Button>("copy");
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

            var pasteButton = visualTree.Q<Button>("paste");
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

            root.Add(visualTree);
            return root;
        }
    }
}