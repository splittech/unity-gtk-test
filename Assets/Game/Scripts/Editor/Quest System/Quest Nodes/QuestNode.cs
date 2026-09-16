using System;
using Game.Core;
using Unity.GraphToolkit.Editor;

namespace Game.Editor
{
    [Serializable]
    public class QuestNode : Node
    {
        public const string TargetOption = "Target";

        protected override void OnDefineOptions(
            IOptionDefinitionContext context)
        {
            context.AddOption<QuestTargetReference>(TargetOption)
                .Build();
        }

        protected override void OnDefinePorts(
            IPortDefinitionContext context)
        {
            context.AddInputPort("In").Build();
            context.AddOutputPort("Out").Build();
        }

        // Вызывается редакторским кодом при преобразовании
        // графа в runtime-данные.
        public QuestTargetReference ReadTargetReference()
        {
            var option = GetNodeOptionByName(TargetOption);

            if (option != null &&
                option.TryGetValue<QuestTargetReference>(out var reference))
            {
                return reference;
            }

            return default;
        }
    }
}