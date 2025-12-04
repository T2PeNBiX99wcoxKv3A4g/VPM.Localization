using io.github.ykysnk.utils.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    [CustomEditor(typeof(BasicLocalization))]
    [CanEditMultipleObjects]
    public class BasicLocalizationEditor : BasicEditor
    {
        private const string LocalizationIDProp = "localizationID";
        private const string DisplayNameProp = "displayName";
        private const string TranslatesProp = "translates";
        [SerializeField] private StyleSheet? uss;
        [SerializeField] private VisualTreeAsset? uxml;

        private SerializedProperty? _displayName;
        private SerializedProperty? _localizationID;
        private SerializedProperty? _translates;

        protected override void OnEnable()
        {
            _localizationID = serializedObject.FindProperty(LocalizationIDProp);
            _displayName = serializedObject.FindProperty(DisplayNameProp);
            _translates = serializedObject.FindProperty(TranslatesProp);
        }

        protected override VisualElement? CreateErrorHandleInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = uxml!.CloneTree();
            root.Bind(serializedObject);

            var localizationIDField = visualTree.Q<TextField>("localizationID");

            localizationIDField.label = _localizationID?.S();
            localizationIDField.tooltip = _localizationID?.Tooltip();

            _localizationID?.Register((label, tooltip) =>
            {
                localizationIDField.label = label;
                localizationIDField.tooltip = tooltip;
            });

            var displayNameField = visualTree.Q<TextField>("displayName");

            displayNameField.label = _displayName?.S();
            displayNameField.tooltip = _displayName?.Tooltip();

            _displayName?.Register((label, tooltip) =>
            {
                displayNameField.label = label;
                displayNameField.tooltip = tooltip;
            });

            var translatesListView = visualTree.Q<ListView>("translates");
            translatesListView.headerTitle = _translates?.S();
            translatesListView.tooltip = _translates?.Tooltip();

            _translates?.Register((label, tooltip) =>
            {
                translatesListView.headerTitle = label;
                translatesListView.tooltip = tooltip;
            });

            root.Add(visualTree);
            GlobalLocalization.DefaultHelper.SelectLanguageElement(root);
            return root;
        }
    }
}