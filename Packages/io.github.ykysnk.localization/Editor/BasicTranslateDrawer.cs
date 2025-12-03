using System;
using io.github.ykysnk.utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor;

[CustomPropertyDrawer(typeof(BasicTranslate))]
public class BasicTranslateDrawer : PropertyDrawer
{
    private static readonly string LocalizationName = GlobalLocalization.NameToLocalizationName(nameof(BasicTranslate));

    private static readonly string LocalizationKeyID =
        $"label.{LocalizationName}.key";

    private static readonly string LocalizationTranslateID = $"label.{LocalizationName}.translate";
    private static readonly string LocalizationTooltipID = $"label.{LocalizationName}.tooltip";
    private static readonly string LocalizationCopyID = $"label.{LocalizationName}.copy";
    private static readonly string LocalizationPasteID = $"label.{LocalizationName}.paste";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        var visualTreeAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                AssetDatabase.GUIDToAssetPath("666a4a69b697dcd45bfe25da3d5636a6"));

        if (visualTreeAsset == null)
        {
            root.Add(new Label("Failed to load text field uxml assets, please reimport the package to fix this issue."));
            return root;
        }

        var keyProperty = property.FindPropertyRelative("key");
        var translateProperty = property.FindPropertyRelative("translate");
        var tooltipProperty = property.FindPropertyRelative("tooltip");

        var visualTree = visualTreeAsset.CloneTree();

        var keyField = visualTree.Q<TextField>("key");

        keyField.label = LocalizationKeyID.S();
        keyField.tooltip = LocalizationKeyID.Tooltip();
        keyField.RegisterCallback<ChangeEvent<string>>(evt => keyProperty.stringValue = evt.newValue);
        keyField.BindProperty(keyProperty);

        LocalizationKeyID.Register((label, tooltip) =>
        {
            keyField.label = label;
            keyField.tooltip = tooltip;
        });

        var translateField = visualTree.Q<TextField>("translate");

        translateField.label = LocalizationTranslateID.S();
        translateField.tooltip = LocalizationTranslateID.Tooltip();
        translateField.RegisterCallback<ChangeEvent<string>>(evt => translateProperty.stringValue = evt.newValue);
        translateField.BindProperty(translateProperty);

        LocalizationTranslateID.Register((label, tooltip) =>
        {
            translateField.label = label;
            translateField.tooltip = tooltip;
        });

        var tooltipField = visualTree.Q<TextField>("tooltip");

        tooltipField.label = LocalizationTooltipID.S();
        tooltipField.tooltip = LocalizationTooltipID.Tooltip();
        tooltipField.RegisterCallback<ChangeEvent<string>>(evt => tooltipProperty.stringValue = evt.newValue);
        tooltipField.BindProperty(tooltipProperty);

        LocalizationTooltipID.Register((label, tooltip) =>
        {
            tooltipField.label = label;
            tooltipField.tooltip = tooltip;
        });

        var copyButton = visualTree.Q<Button>("copy");
        copyButton.text = LocalizationCopyID.S();
        copyButton.tooltip = LocalizationCopyID.Tooltip();
        copyButton.clicked += () =>
        {
            var copyBasicTranslate = new BasicTranslate
            {
                key = keyProperty.stringValue,
                translate = translateProperty.stringValue,
                tooltip = tooltipProperty.stringValue
            };

            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(copyBasicTranslate);
        };

        LocalizationCopyID.Register((label, tooltip) =>
        {
            copyButton.text = label;
            copyButton.tooltip = tooltip;
        });

        var pasteButton = visualTree.Q<Button>("paste");
        pasteButton.text = LocalizationPasteID.S();
        pasteButton.tooltip = LocalizationPasteID.Tooltip();
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

        LocalizationPasteID.Register((label, tooltip) =>
        {
            pasteButton.text = label;
            pasteButton.tooltip = tooltip;
        });

        root.Add(visualTree);
        return root;
    }
}