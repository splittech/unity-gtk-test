using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(QuestRunner))]
    public sealed class QuestRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty sourceGraph;

        private void OnEnable()
        {
            sourceGraph = serializedObject.FindProperty("sourceGraph");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var runner = (QuestRunner)target;

            EditorGUI.BeginChangeCheck();

            var selectedGraph = EditorGUILayout.ObjectField(
                "Quest Graph",
                sourceGraph.objectReferenceValue,
                typeof(UnityEngine.Object),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedGraph == null ||
                    QuestGraphEditingContext.IsQuestAsset(selectedGraph))
                {
                    sourceGraph.objectReferenceValue = selectedGraph;
                    QuestGraphEditingContext.Forget(runner);
                }
                else
                {
                    Debug.LogWarning("Нужно назначить файл .quest.", runner);
                }
            }

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "sourceGraph",
                "bindings");

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(
                EditorApplication.isPlayingOrWillChangePlaymode ||
                runner.SourceGraph == null ||
                runner.Definition == null))
            {
                if (GUILayout.Button("Compile Graph"))
                    QuestCompiler.Compile(runner);
            }

            bool canOpen =
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !EditorUtility.IsPersistent(runner) &&
                runner.gameObject.scene.IsValid() &&
                QuestGraphEditingContext.IsQuestAsset(runner.SourceGraph);

            using (new EditorGUI.DisabledScope(!canOpen))
            {
                if (GUILayout.Button("Open Graph"))
                    QuestGraphEditingContext.Open(runner);
            }
        }
    }
}