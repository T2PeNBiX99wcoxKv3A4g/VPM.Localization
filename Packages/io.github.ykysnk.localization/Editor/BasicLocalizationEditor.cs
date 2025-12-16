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
        [SerializeField] private StyleSheet? uss;
        [SerializeField] private VisualTreeAsset? uxml;

        protected override VisualElement? CreateErrorHandleInspectorGUI()
        {
            var tree = uxml!.CloneTree();
            GlobalLocalization.DefaultHelper.UILocalize(tree);
            tree.Bind(serializedObject);
            return tree;
        }
    }
}