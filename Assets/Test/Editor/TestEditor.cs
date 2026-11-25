using io.github.ykysnk.Localization.Editor;
using io.github.ykysnk.utils.Editor;
using UnityEditor;

namespace Test.Editor
{
    [CustomEditor(typeof(Test))]
    [CanEditMultipleObjects]
    public class TestEditor : BasicEditor
    {
        private const string TextProp = "text";

        // A consumer of the package brings its own id and helper; the package's own id is internal to it.
        private static readonly LocalizationHelper Helper = new("io.github.ykysnk.localization");

        private SerializedProperty? _text;

        protected override void OnEnable()
        {
            _text = serializedObject.FindProperty(TextProp);
        }

        protected override void OnErrorHandleInspectorGUI()
        {
            EditorGUILayout.PropertyField(_text, Helper.G(_text!));
            EditorGUILayout.PropertyField(_text, Helper.G("Do not exist"));
            Helper.SelectLanguageGUI();
        }
    }
}