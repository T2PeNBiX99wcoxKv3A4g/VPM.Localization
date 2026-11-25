using io.github.ykysnk.Localization.Editor;
using io.github.ykysnk.utils.Editor;
using UnityEditor;

// One line per assembly is all a consumer needs; the S/G/UILocalize sugar then resolves the helper by itself.
[assembly: LocalizationPackage("io.github.ykysnk.localization")]

namespace Test.Editor
{
    [CustomEditor(typeof(Test))]
    [CanEditMultipleObjects]
    public class TestEditor : BasicEditor
    {
        private const string TextProp = "text";

        private SerializedProperty? _text;

        protected override void OnEnable()
        {
            _text = serializedObject.FindProperty(TextProp);
        }

        protected override void OnErrorHandleInspectorGUI()
        {
            EditorGUILayout.PropertyField(_text, _text!.G());
            EditorGUILayout.PropertyField(_text, "Do not exist".G());
            LocalizationHelper.For(typeof(TestEditor)).SelectLanguageGUI();
        }
    }
}
