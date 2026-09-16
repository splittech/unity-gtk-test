using System;
using System.Collections.Generic;
using Game.Core;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class QuestGraphEditingContext
    {
        private sealed class Entry
        {
            public QuestRunner Runner;
            public UnityEngine.Object Asset;
        }

        private static readonly Dictionary<EditorWindow, Entry>
            contexts = new();

        static QuestGraphEditingContext()
        {
            EditorApplication.playModeStateChanged += _ => contexts.Clear();
        }

        public static bool IsQuestAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return false;

            var path = AssetDatabase.GetAssetPath(asset);

            return path.EndsWith(
                "." + QuestGraph.AssetExtension,
                StringComparison.OrdinalIgnoreCase);
        }

        public static void Open(QuestRunner runner)
        {
            if (runner == null || !IsQuestAsset(runner.SourceGraph))
                return;

            if (EditorUtility.IsPersistent(runner) ||
                !runner.gameObject.scene.IsValid())
            {
                Debug.LogWarning(
                    "Открывай граф через QuestRunner из сцены.",
                    runner);
                return;
            }

            var asset = runner.SourceGraph;
            var path = AssetDatabase.GetAssetPath(asset);

            if (!AssetDatabase.OpenAsset(asset))
            {
                Debug.LogError("Не удалось открыть квестовый граф.", runner);
                return;
            }

            // Даём редактору завершить открытие окна.
            EditorApplication.delayCall += () =>
            {
                if (runner == null || runner.SourceGraph != asset)
                    return;

                var window = EditorWindow.focusedWindow;

                if (window == null ||
                    window is not IGraphWindow graphWindow ||
                    graphWindow.Graph == null ||
                    GraphDatabase.GetGraphAssetPath(graphWindow.Graph) != path)
                {
                    Debug.LogWarning(
                        "Не удалось определить окно графа. " +
                        "Повтори Open Graph в Inspector runner.",
                        runner);
                    return;
                }

                contexts[window] = new Entry
                {
                    Runner = runner,
                    Asset = asset
                };
            };
        }

        public static QuestRunner GetRunner(VisualElement element)
        {
            if (element.panel == null)
                return null;

            foreach (var pair in contexts)
            {
                var window = pair.Key;
                var entry = pair.Value;
                var runner = entry.Runner;

                if (window == null || runner == null)
                    continue;

                // Drawer должен находиться в панели именно этого окна.
                if (window.rootVisualElement.panel != element.panel)
                    continue;

                if (runner.SourceGraph != entry.Asset)
                    continue;

                if (window is not IGraphWindow graphWindow ||
                    graphWindow.Graph == null)
                    continue;

                var expectedPath = AssetDatabase.GetAssetPath(entry.Asset);
                var actualPath =
                    GraphDatabase.GetGraphAssetPath(graphWindow.Graph);

                if (actualPath == expectedPath)
                    return runner;
            }

            return null;
        }

        public static void Forget(QuestRunner runner)
        {
            var windows = new List<EditorWindow>();

            foreach (var pair in contexts)
            {
                if (pair.Key == null || pair.Value.Runner == runner)
                    windows.Add(pair.Key);
            }

            foreach (var window in windows)
                contexts.Remove(window);
        }
    }
}