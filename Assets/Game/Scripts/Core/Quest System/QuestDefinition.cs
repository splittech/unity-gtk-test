using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Quests/Quest Definition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField]
        private string entryStepId;

        [SerializeField]
        private List<Step> steps = new();

        public string EntryStepId => entryStepId;
        public IReadOnlyList<Step> Steps => steps;

        public bool TryGetStep(string id, out Step result)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var step in steps)
                {
                    if (step != null && step.Id == id)
                    {
                        result = step;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

#if UNITY_EDITOR
        // Вызывается компилятором после проверки графа.
        // Сохранение asset выполняет редакторский код.
        public void SetCompiledData(
            string entryId,
            IEnumerable<Step> compiledSteps)
        {
            if (compiledSteps == null)
                throw new ArgumentNullException(nameof(compiledSteps));

            var newSteps = new List<Step>(compiledSteps);

            entryStepId = entryId;
            steps = newSteps;
        }
#endif

        [Serializable]
        public sealed class Step
        {
            [SerializeField]
            private string id;

            [SerializeField]
            private string title;

            [SerializeField]
            private QuestTargetReference target;

            [SerializeField]
            private string nextStepId;

            public string Id => id;
            public string Title => title;
            public QuestTargetReference Target => target;
            public string NextStepId => nextStepId;

            // Пустой переход означает завершение сценария.
            public bool IsFinal => string.IsNullOrEmpty(nextStepId);

            public Step(
                string id,
                string title,
                QuestTargetReference target,
                string nextStepId)
            {
                this.id = id;
                this.title = title;
                this.target = target;
                this.nextStepId = nextStepId;
            }
        }
    }
}