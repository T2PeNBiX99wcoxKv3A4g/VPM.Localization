using io.github.ykysnk.Localization.Editor;
using io.github.ykysnk.utils.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Test.Editor
{
    [CustomEditor(typeof(Test2))]
    [CanEditMultipleObjects]
    public class TestEditor2 : BasicEditor
    {
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