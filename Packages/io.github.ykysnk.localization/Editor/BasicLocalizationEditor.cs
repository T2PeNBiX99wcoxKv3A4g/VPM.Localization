using io.github.ykysnk.utils.Editor;
using UnityEditor;

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

    protected override void OnInspectorGUIDraw()
    {
        EditorGUILayout.PropertyField(_localizationID, "label.localization_id".G(GlobalLocalization.DefaultLocalization));
        EditorGUILayout.PropertyField(_displayName, "label.display_name".G(GlobalLocalization.DefaultLocalization));
        EditorGUILayout.PropertyField(_translates, "label.translates".G(GlobalLocalization.DefaultLocalization));
        GlobalLocalization.SelectLanguageGUI(GlobalLocalization.DefaultLocalization);
    }
}