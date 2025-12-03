using io.github.ykysnk.utils.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor;

[CustomEditor(typeof(BasicLocalization))]
[CanEditMultipleObjects]
public class BasicLocalizationEditor : BasicEditor
{
    private const string LocalizationIDProp = "localizationID";
    private const string DisplayNameProp = "displayName";
    private const string TranslatesProp = "translates";

    private SerializedProperty? _displayName;
    private SerializedProperty? _localizationID;
    private SerializedProperty? _translates;

    protected override void OnEnable()
    {
        _localizationID = serializedObject.FindProperty(LocalizationIDProp);
        _displayName = serializedObject.FindProperty(DisplayNameProp);
        _translates = serializedObject.FindProperty(TranslatesProp);
    }

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var visualTreeAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                AssetDatabase.GUIDToAssetPath("92e5f13aabc45984ea26dbc24326d171"));

        if (visualTreeAsset == null)
        {
            root.Add(new Label("Failed to load text field uxml assets, please reimport the package to fix this issue."));
            return root;
        }

        var visualTree = visualTreeAsset.CloneTree();

        var localizationIDField = visualTree.Q<TextField>("localizationID");

        localizationIDField.label = _localizationID?.S();
        localizationIDField.RegisterCallback<ChangeEvent<string>>(evt => _localizationID!.stringValue = evt.newValue);
        localizationIDField.BindProperty(_localizationID);

        var displayNameField = visualTree.Q<TextField>("displayName");

        displayNameField.label = _displayName?.S();
        displayNameField.RegisterCallback<ChangeEvent<string>>(evt => _displayName!.stringValue = evt.newValue);
        displayNameField.BindProperty(_displayName);

        var translatesListView = visualTree.Q<ListView>("translates");
        translatesListView.headerTitle = _translates?.S();
        translatesListView.BindProperty(_translates);

        root.Add(visualTree);
        GlobalLocalization.DefaultHelper.SelectLanguageElement(root);
        return root;
    }

    protected override void OnInspectorGUIDraw()
    {
        EditorGUILayout.PropertyField(_localizationID, _localizationID?.G());
        EditorGUILayout.PropertyField(_displayName, _displayName?.G());
        EditorGUILayout.PropertyField(_translates, _translates?.G());
        GlobalLocalization.DefaultHelper.SelectLanguageGUI();
    }
}