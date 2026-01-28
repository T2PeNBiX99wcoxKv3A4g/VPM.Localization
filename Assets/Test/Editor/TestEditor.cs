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

        private SerializedProperty? _text;

        protected override void OnEnable()
        {
            _text = serializedObject.FindProperty(TextProp);
        }

        protected override void OnErrorHandleInspectorGUI()
        {
            EditorGUILayout.PropertyField(_text, GlobalLocalization.DefaultHelper.G(_text!));
            EditorGUILayout.PropertyField(_text, GlobalLocalization.DefaultHelper.G("Do not exist"));
            GlobalLocalization.SelectLanguageGUI(GlobalLocalization.DefaultLocalization);
        }
    }
}