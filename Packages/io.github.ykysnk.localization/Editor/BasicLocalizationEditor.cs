using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private const string ConvertFoldoutKey = "io.github.ykysnk.Localization.BasicLocalizationEditor.convert";

        [SerializeField] private VisualTreeAsset? uxml;

        private readonly List<int> _visibleIndices = new();

        private Label? _countLabel;
        private string _search = "";
        private ToolbarSearchField? _searchField;
        private ListView? _translatesList;

        private SerializedProperty Translates => serializedObject.FindProperty("translates");

        protected override VisualElement? CreateErrorHandleInspectorGUI()
        {
            var tree = uxml!.CloneTree();
            GlobalLocalization.DefaultHelper.UILocalize(tree);
            tree.Bind(serializedObject);

            _translatesList = tree.Q<ListView>("translates");
            _translatesList.showBoundCollectionSize = false;
            _translatesList.makeItem = () => new()
            {
                name = "translate-row"
            };
            _translatesList.bindItem = BindRow;
            _translatesList.itemIndexChanged += OnItemIndexChanged;
            _translatesList.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var selected = SelectedPropertyIndices();

                if (selected.Count == 0) return;

                evt.menu.AppendAction("label.basic_translate.copy".S(), _ => CopySelected(selected));

                var canBePaste = InternalLocalizationExtensions.TryPasteTranslate(out _, out _);
                evt.menu.AppendAction("label.basic_translate.paste".S(), _ => PasteSelected(selected),
                    canBePaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("label.basic_translate.remove".S(), _ => RemoveSelected(selected));
            }));

            _searchField = tree.Q<ToolbarSearchField>("search");
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? "";
                RefreshList();
            });

            var addButton = tree.Q<Button>("addTranslate");
            addButton.clicked += AddTranslate;

            var removeButton = tree.Q<Button>("removeTranslate");
            removeButton.clicked += () => RemoveSelected(SelectedPropertyIndices());

            void UpdateRemoveButton() => removeButton.SetEnabled(SelectedPropertyIndices().Count > 0);

            _translatesList.selectionChanged += _ => UpdateRemoveButton();
            UpdateRemoveButton();

            _countLabel = tree.Q<Label>("translatesCount");

            var convert = tree.Q<Foldout>("convert");
            convert.value = EditorPrefs.GetBool(ConvertFoldoutKey);
            convert.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(ConvertFoldoutKey, evt.newValue));

            tree.Q<Button>("exportJson").clicked += () => Export(".json", LocalizationConverter.ToJson);
            tree.Q<Button>("importJson").clicked +=
                () => Import("jsonFile", ".json", LocalizationConverter.TryImportJson);
            tree.Q<Button>("exportPo").clicked += () => Export(".po", LocalizationConverter.ToPo);
            tree.Q<Button>("importPo").clicked += () => Import("poFile", ".po", LocalizationConverter.TryImportPo);
            tree.Q<Button>("pickJson").clicked += () => PickSidecar("jsonFile", ".json");
            tree.Q<Button>("pickPo").clicked += () => PickSidecar("poFile", ".po");

            _translatesList.itemsSource = _visibleIndices;
            RefreshList();

            return tree;
        }

        private void BindRow(VisualElement element, int index)
        {
            element.Clear();

            if (index < 0 || index >= _visibleIndices.Count) return;

            var property = Translates.GetArrayElementAtIndex(_visibleIndices[index]);
            element.Add(new BasicTranslateDrawer().CreatePropertyGUI(property));
        }

        private void RefreshList()
        {
            if (_translatesList == null) return;

            var translates = Translates;
            var query = _search.Trim();

            _visibleIndices.Clear();
            for (var i = 0; i < translates.arraySize; i++)
            {
                var element = translates.GetArrayElementAtIndex(i);
                if (query.Length > 0 && !Matches(element, query)) continue;
                _visibleIndices.Add(i);
            }

            _translatesList.itemsSource = _visibleIndices;
            _translatesList.Rebuild();
            _translatesList.reorderable = query.Length == 0;
            UpdateCount(translates.arraySize);
        }

        private static bool Matches(SerializedProperty element, string query)
        {
            return Contains(element.FindPropertyRelative("key").stringValue) ||
                   Contains(element.FindPropertyRelative("translate").stringValue) ||
                   Contains(element.FindPropertyRelative("tooltip").stringValue);

            bool Contains(string value) =>
                !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateCount(int total)
        {
            if (_countLabel == null) return;

            var filtered = _search.Trim().Length > 0;
            _countLabel.text = filtered ? $"{_visibleIndices.Count}/{total}" : total.ToString();
        }

        private List<int> SelectedPropertyIndices() =>
            _translatesList == null
                ? new()
                : _translatesList.selectedIndices
                    .Where(i => i >= 0 && i < _visibleIndices.Count)
                    .Select(i => _visibleIndices[i])
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

        private void OnItemIndexChanged(int from, int to)
        {
            // With a filter active the visible order does not map to array indices, so reordering is disabled there.
            if (_search.Trim().Length > 0) return;

            var translates = Translates;
            if (from < 0 || to < 0 || from >= translates.arraySize || to >= translates.arraySize) return;

            translates.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();
            RefreshList();
        }

        private void CopySelected(List<int> selected)
        {
            if (selected.Count == 0) return;

            var translates = Translates;
            var index = selected[selected.Count - 1];
            if (index < 0 || index >= translates.arraySize) return;

            var element = translates.GetArrayElementAtIndex(index);
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(new BasicTranslate
            {
                key = element.FindPropertyRelative("key").stringValue,
                translate = element.FindPropertyRelative("translate").stringValue,
                tooltip = element.FindPropertyRelative("tooltip").stringValue,
                useTooltip = element.FindPropertyRelative("useTooltip").boolValue,
                tooltipSuffix = element.FindPropertyRelative("tooltipSuffix").stringValue
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
                element.FindPropertyRelative("useTooltip").boolValue = pasted.useTooltip;
                element.FindPropertyRelative("tooltipSuffix").stringValue =
                    string.IsNullOrEmpty(pasted.tooltipSuffix) ? ".tooltip" : pasted.tooltipSuffix;
            }

            serializedObject.ApplyModifiedProperties();
            RefreshList();
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
            RefreshList();
        }

        private void AddTranslate()
        {
            var translates = Translates;
            translates.arraySize++;

            var element = translates.GetArrayElementAtIndex(translates.arraySize - 1);
            element.FindPropertyRelative("key").stringValue = "";
            element.FindPropertyRelative("translate").stringValue = "";
            element.FindPropertyRelative("tooltip").stringValue = "";
            element.FindPropertyRelative("useTooltip").boolValue = false;
            element.FindPropertyRelative("tooltipSuffix").stringValue = ".tooltip";

            serializedObject.ApplyModifiedProperties();

            // Clear the filter so the new row is visible, then scroll to it.
            if (_searchField != null && _searchField.value.Length > 0)
            {
                _searchField.SetValueWithoutNotify("");
                _search = "";
            }

            RefreshList();
            _translatesList?.ScrollToItem(_visibleIndices.Count - 1);
        }

        private void Export(string extension, Func<BasicLocalization, string> writer)
        {
            if (!(target is BasicLocalization localization)) return;

            var assetPath = AssetDatabase.GetAssetPath(localization);
            if (string.IsNullOrEmpty(assetPath)) return;

            var filePath = Path.ChangeExtension(assetPath, extension);

            try
            {
                File.WriteAllText(filePath, writer(localization), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Utils.LogError(nameof(BasicLocalizationEditor), $"Failed to write {filePath}: {e.Message}");
                return;
            }

            AssignSidecar(extension == ".json" ? "jsonFile" : "poFile", filePath);

            Utils.Log(nameof(BasicLocalizationEditor), $"Exported {localization.name} to {filePath}");
        }

        private void PickSidecar(string fieldName, string extension)
        {
            if (!(target is BasicLocalization localization)) return;

            var source = EditorUtility.OpenFilePanel($"Select a {extension} file", "", extension.TrimStart('.'));
            if (string.IsNullOrEmpty(source)) return;

            var assetPath = AssetDatabase.GetAssetPath(localization);
            if (string.IsNullOrEmpty(assetPath)) return;

            // A reference can only point inside the project, so the picked file is copied next to the asset.
            var destination = Path.ChangeExtension(assetPath, extension);

            try
            {
                if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination),
                        StringComparison.OrdinalIgnoreCase))
                    File.Copy(source, destination, true);
            }
            catch (Exception e)
            {
                Utils.LogError(nameof(BasicLocalizationEditor), $"Failed to copy {source}: {e.Message}");
                return;
            }

            AssignSidecar(fieldName, destination);

            Utils.Log(nameof(BasicLocalizationEditor), $"Assigned {destination}");
        }

        private void AssignSidecar(string fieldName, string filePath)
        {
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);

            // Assign through the SerializedObject: setting the field on the object alone is overwritten by the next
            // binding pass and never marks the asset dirty.
            var field = serializedObject.FindProperty(fieldName);
            field.objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            serializedObject.ApplyModifiedProperties();
        }

        private void Import(string fieldName, string extension, ImportAction importer)
        {
            if (!(target is BasicLocalization localization)) return;

            var reference = serializedObject.FindProperty(fieldName).objectReferenceValue;
            var filePath = reference == null ? string.Empty : AssetDatabase.GetAssetPath(reference);

            if (string.IsNullOrEmpty(filePath))
            {
                var assetPath = AssetDatabase.GetAssetPath(localization);
                filePath = Path.ChangeExtension(assetPath, extension);
            }

            string text;
            try
            {
                if (!File.Exists(filePath))
                {
                    Utils.LogWarning(nameof(BasicLocalizationEditor),
                        $"No {extension} file is assigned for {localization.name}.");
                    return;
                }

                text = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Utils.LogError(nameof(BasicLocalizationEditor), $"Failed to read {filePath}: {e.Message}");
                return;
            }

            if (!importer(localization, text, out var exception))
            {
                Utils.LogError(nameof(BasicLocalizationEditor),
                    $"Failed to import {filePath}: {exception?.Message}\n{exception?.StackTrace}");
                return;
            }

            EditorUtility.SetDirty(localization);
            serializedObject.Update();
            RefreshList();

            Utils.Log(nameof(BasicLocalizationEditor), $"Imported {filePath}");
        }

        private delegate bool ImportAction(BasicLocalization localization, string? text, out Exception? exception);
    }
}