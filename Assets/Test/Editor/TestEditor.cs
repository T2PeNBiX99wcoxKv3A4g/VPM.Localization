using io.github.ykysnk.Localization.Editor;
using io.github.ykysnk.utils.Editor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    [CustomEditor(typeof(Test))]
    [CanEditMultipleObjects]
    public class TestEditor : BasicEditor
    {
        private const string TextProp = "text";

        [CanBeNull] private SerializedProperty _text;

        protected override void OnEnable()
        {
            _text = serializedObject.FindProperty(TextProp);
        }

        protected override void OnInspectorGUIDraw()
        {
            EditorGUILayout.PropertyField(_text,
                _text != null ? GlobalLocalization.DefaultHelper.G(_text) : GUIContent.none);
            GlobalLocalization.SelectLanguageGUI(GlobalLocalization.DefaultLocalization);
        }
    }
}