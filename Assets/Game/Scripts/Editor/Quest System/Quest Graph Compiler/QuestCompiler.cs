using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class QuestCompiler
    {
        public static bool Compile(QuestRunner runner)
        {
            try
            {
                if (runner == null)
                    throw new InvalidOperationException("Не указан Runner.");

                if (Application.isPlaying)
                    throw new InvalidOperationException(
                        "Компилируй граф вне Play Mode.");

                if (runner.SourceGraph == null)
                    throw new InvalidOperationException(
                        "Не назначен Source Graph.");

                if (runner.Definition == null ||
                    !AssetDatabase.Contains(runner.Definition))
                {
                    throw new InvalidOperationException(
                        "Создай asset QuestDefinition и назначь его Runner.");
                }

                string path = AssetDatabase.GetAssetPath(runner.SourceGraph);

                if (!path.EndsWith(
                        "." + QuestGraph.AssetExtension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Source Graph должен быть файлом .quest.");
                }

                var graph = GraphDatabase.LoadGraph<QuestGraph>(path);

                if (graph == null)
                    throw new InvalidOperationException(
                        "Не удалось загрузить QuestGraph.");

                // Сначала полностью проверяем и собираем данные.
                var steps = BuildSteps(graph);

                // Только после успешной проверки изменяем asset.
                var definition = runner.Definition;

                Undo.RecordObject(definition, "Compile Quest");
                definition.SetCompiledData(steps[0].Id, steps);

                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);

                Debug.Log(
                    $"Квест скомпилирован. Шагов: {steps.Count}.",
                    definition);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Ошибка компиляции квеста: {exception.Message}",
                    runner);

                return false;
            }
        }

        private static List<QuestDefinition.Step> BuildSteps(
            QuestGraph graph)
        {
            var allNodes = graph.GetNodes().ToList();

            if (allNodes.Count == 0)
                throw new InvalidOperationException("Граф пуст.");

            if (allNodes.Any(node => node is not QuestNode))
            {
                throw new InvalidOperationException(
                    "Эта версия поддерживает только QuestNode, без подграфов.");
            }

            var nodes = allNodes.Cast<QuestNode>().ToList();

            var byId = new Dictionary<string, QuestNode>();
            var nextById = new Dictionary<string, string>();
            var incomingCount = new Dictionary<string, int>();

            foreach (var node in nodes)
            {
                string id = node.ID.ToString();

                if (!byId.TryAdd(id, node))
                    throw new InvalidOperationException(
                        $"Повторяющийся ID ноды: {id}.");

                incomingCount.Add(id, 0);

                if (string.IsNullOrEmpty(
                        node.ReadTargetReference().BindingId))
                {
                    throw new InvalidOperationException(
                        $"У ноды {id} не задан ключ Target.");
                }

                if (node.GetInputPortByName("In") == null ||
                    node.GetOutputPortByName("Out") == null)
                {
                    throw new InvalidOperationException(
                        $"У ноды {id} отсутствуют порты In/Out.");
                }
            }

            foreach (var node in nodes)
            {
                string id = node.ID.ToString();
                var output = node.GetOutputPortByName("Out");

                var connections = new List<IPort>();
                output.GetConnectedPorts(connections);

                if (connections.Count > 1)
                    throw new InvalidOperationException(
                        $"У ноды {id} несколько выходных переходов.");

                string nextId = null;

                if (connections.Count == 1)
                {
                    var destination = connections[0];

                    if (destination.Name != "In" ||
                        destination.GetNode() is not QuestNode nextNode ||
                        !byId.ContainsKey(nextNode.ID.ToString()))
                    {
                        throw new InvalidOperationException(
                            $"Выход ноды {id} должен вести в In другой QuestNode.");
                    }

                    nextId = nextNode.ID.ToString();
                    incomingCount[nextId]++;

                    if (incomingCount[nextId] > 1)
                        throw new InvalidOperationException(
                            $"У ноды {nextId} несколько входных переходов.");
                }

                nextById.Add(id, nextId);
            }

            // Дополнительно проверяем входящие соединения.
            foreach (var node in nodes)
            {
                var connections = new List<IPort>();
                node.GetInputPortByName("In")
                    .GetConnectedPorts(connections);

                string id = node.ID.ToString();

                if (connections.Count != incomingCount[id])
                    throw new InvalidOperationException(
                        $"Некорректные входящие соединения ноды {id}.");
            }

            var roots = nodes
                .Where(node => incomingCount[node.ID.ToString()] == 0)
                .ToList();

            if (roots.Count != 1)
                throw new InvalidOperationException(
                    "Должен быть ровно один шаг без входящего перехода.");

            var visited = new HashSet<string>();
            var result = new List<QuestDefinition.Step>();

            string currentId = roots[0].ID.ToString();

            while (currentId != null)
            {
                if (!visited.Add(currentId))
                    throw new InvalidOperationException(
                        "Циклы в этой версии не поддерживаются.");

                var node = byId[currentId];
                string nextId = nextById[currentId];

                result.Add(new QuestDefinition.Step(
                    currentId,
                    $"Шаг {result.Count + 1}",
                    node.ReadTargetReference(),
                    nextId));

                currentId = nextId;
            }

            if (visited.Count != nodes.Count)
                throw new InvalidOperationException(
                    "Граф содержит недостижимые ноды или отдельный цикл.");

            return result;
        }
    }
}