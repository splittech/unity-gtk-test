using System;
using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor
{
    [CustomPropertyDrawer(typeof(QuestTargetReference))]
    public sealed class QuestTargetReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(
            SerializedProperty property)
        {
            var root = new VisualElement();

            var field = new ObjectField("Target")
            {
                objectType = typeof(QuestTarget),
                allowSceneObjects = true
            };

            var message = new HelpBox(
                "Открой этот граф кнопкой Open Graph на QuestRunner.",
                HelpBoxMessageType.Info);

            root.Add(field);
            root.Add(message);

            SerializedProperty GetIdProperty()
            {
                return property.FindPropertyRelative(
                    nameof(QuestTargetReference.BindingId));
            }

            void Refresh()
            {
                // Toolkit может уничтожить временный объект,
                // на котором основан SerializedProperty.
                if (property.serializedObject.targetObject == null)
                    return;

                property.serializedObject.UpdateIfRequiredOrScript();

                var runner = QuestGraphEditingContext.GetRunner(root);
                field.SetEnabled(runner != null);

                message.style.display = runner == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                field.tooltip = runner != null
                    ? $"Привязки QuestRunner: {runner.name}"
                    : "Нет контекста QuestRunner";

                QuestTarget target = null;
                var id = GetIdProperty().stringValue;

                if (runner != null && !string.IsNullOrEmpty(id))
                {
                    target = runner.GetReferenceValue(
                        new PropertyName(id), out _) as QuestTarget;
                }

                field.SetValueWithoutNotify(target);
            }

            field.RegisterValueChangedCallback(evt =>
            {
                var runner = QuestGraphEditingContext.GetRunner(root);

                if (runner == null)
                {
                    Refresh();
                    return;
                }

                var target = evt.newValue as QuestTarget;

                // Мини-пример поддерживает ссылки внутри одной сцены.
                if (target != null &&
                    (EditorUtility.IsPersistent(target) ||
                     target.gameObject.scene != runner.gameObject.scene))
                {
                    Debug.LogWarning(
                        "Выбери QuestTarget из той же сцены, что и Runner.",
                        runner);

                    Refresh();
                    return;
                }

                property.serializedObject.UpdateIfRequiredOrScript();

                var idProperty = GetIdProperty();
                var id = idProperty.stringValue;

                if (string.IsNullOrEmpty(id) && target == null)
                    return;

                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Assign Quest Target");

                // Ключ создаётся при первом назначении,
                // а не при каждом построении интерфейса ноды.
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                    idProperty.stringValue = id;

                    // SerializedProperty обеспечивает Undo изменения ключа.
                    property.serializedObject.ApplyModifiedProperties();
                }

                Undo.RecordObject(runner, "Assign Quest Target");

                if (target == null)
                    runner.ClearReferenceValue(new PropertyName(id));
                else
                    runner.SetReferenceValue(new PropertyName(id), target);

                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    runner);

                EditorUtility.SetDirty(runner);
                EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);

                Undo.CollapseUndoOperations(undoGroup);
                Refresh();
            });

            // Обновляет отображение после смены контекста и Undo/Redo.
            // Планировщик работает, пока элемент подключён к панели.
            root.schedule.Execute(Refresh).Every(250);

            return root;
        }
    }
}