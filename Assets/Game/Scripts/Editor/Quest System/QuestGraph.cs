using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Game.Editor
{
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestGraph : Graph
    {
        public const string AssetExtension = "quest";

        [MenuItem("Assets/Create/Graphs/Quest Graph", false)]
        private static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestGraph>();
        }
    }
}
