using System.Collections.Generic;
using System.Linq;
using io.github.ykysnk.utils;
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
        [SerializeField] private VisualTreeAsset? uxml;

        protected override VisualElement? CreateErrorHandleInspectorGUI()
        {
            var tree = uxml!.CloneTree();
            GlobalLocalization.DefaultHelper.UILocalize(tree);
            tree.Bind(serializedObject);

            var translatesList = tree.Q<ListView>("translates");
            translatesList.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var selectedList = translatesList.selectedIndices.OrderBy(i => i).ToList();

                if (!selectedList.Any()) return;

                evt.menu.AppendAction("label.basic_translate.copy".S(), _ => CopySelected(selectedList));

                var canBePaste = InternalLocalizationExtensions.TryPasteTranslate(out _, out _);
                evt.menu.AppendAction("label.basic_translate.paste".S(), _ => PasteSelected(selectedList),
                    canBePaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("label.basic_translate.remove".S(), _ => RemoveSelected(selectedList));
            }));

            var addButton = translatesList.Q<Button>("unity-list-view__add-button");
            addButton.clickable = null;
            addButton.clicked += AddTranslate;

            return tree;
        }

        // All edits go through the SerializedProperty so the asset is marked dirty, Undo works and the list view
        // rebuilds itself after the array changed.
        private SerializedProperty Translates => serializedObject.FindProperty("translates");

        private void CopySelected(List<int> selected)
        {
            var translates = Translates;
            var index = selected[selected.Count - 1];
            if (index < 0 || index >= translates.arraySize) return;

            var element = translates.GetArrayElementAtIndex(index);
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(new BasicTranslate
            {
                key = element.FindPropertyRelative("key").stringValue,
                translate = element.FindPropertyRelative("translate").stringValue,
                tooltip = element.FindPropertyRelative("tooltip").stringValue
            });
        }

        private void PasteSelected(List<int> selected)
        {
            if (!InternalLocalizationExtensions.TryPasteTranslate(out var pasted, out var exception))
            {
                Utils.LogWarning(nameof(BasicLocalizationEditor),
                    $"Failed to paste: {exception?.Message}\n{exception?.StackTrace}");
                return;
            }

            var translates = Translates;

            foreach (var index in selected)
            {
                if (index < 0 || index >= translates.arraySize) continue;

                var element = translates.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("key").stringValue = pasted.key;
                element.FindPropertyRelative("translate").stringValue = pasted.translate ?? "";
                element.FindPropertyRelative("tooltip").stringValue = pasted.tooltip ?? "";
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveSelected(List<int> selected)
        {
            var translates = Translates;

            // Delete from the end, otherwise every removal shifts the indices that still have to be removed.
            foreach (var index in selected.OrderByDescending(i => i))
            {
                if (index < 0 || index >= translates.arraySize) continue;
                translates.DeleteArrayElementAtIndex(index);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddTranslate()
        {
            var translates = Translates;
            translates.arraySize++;

            var element = translates.GetArrayElementAtIndex(translates.arraySize - 1);
            element.FindPropertyRelative("key").stringValue = "";
            element.FindPropertyRelative("translate").stringValue = "";
            element.FindPropertyRelative("tooltip").stringValue = "";

            serializedObject.ApplyModifiedProperties();
        }
    }
}
