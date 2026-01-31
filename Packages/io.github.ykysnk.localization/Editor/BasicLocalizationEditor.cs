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

            var list = (BasicLocalization)target;

            var translatesList = tree.Q<ListView>("translates");
            translatesList.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var selectedList = translatesList.selectedIndices.ToList();

                if (!selectedList.Any()) return;

                evt.menu.AppendAction("label.basic_translate.copy".S(), _ =>
                {
                    foreach (var selected in selectedList)
                        EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(list.translates[selected]);
                });

                var canBePaste = InternalLocalizationExtensions.TryPasteTranslate(out _, out _);

                evt.menu.AppendAction("label.basic_translate.paste".S(), _ =>
                {
                    foreach (var selected in selectedList)
                        if (InternalLocalizationExtensions.TryPasteTranslate(out var pasteBasicTranslate,
                                out var exception))
                        {
                            var obj = list.translates[selected];
                            obj.key = pasteBasicTranslate.key;
                            obj.translate = pasteBasicTranslate.translate;
                            obj.tooltip = pasteBasicTranslate.tooltip;
                            list.translates[selected] = obj;
                        }
                        else
                            Utils.LogWarning(nameof(BasicTranslateDrawer),
                                $"Failed to paste: {exception?.Message}\n{exception?.StackTrace}");
                }, canBePaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("label.basic_translate.remove".S(), _ =>
                {
                    foreach (var selected in selectedList)
                        list.translates.RemoveAt(selected);
                });
            }));

            var addButton = translatesList.Q<Button>("unity-list-view__add-button");
            addButton.clickable = null;
            addButton.clicked += () =>
            {
                var last = list.translates.LastOrDefault();
                list.translates.Add(new()
                {
                    key = last.key
                });
            };

            return tree;
        }
    }
}