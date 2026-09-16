using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core
{
    public class QuestRunner : MonoBehaviour, IExposedPropertyTable
    {
        [SerializeField]
        private QuestDefinition definition;

        [SerializeField]
        private bool playOnStart = true;

        [SerializeField, HideInInspector]
        private List<Binding> bindings = new();

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private Object sourceGraph;

        public Object SourceGraph => sourceGraph;
#endif

        private QuestDefinition activeDefinition;
        private QuestDefinition.Step currentStep;
        private QuestTarget currentTarget;

        private bool transitionPending;

        public QuestDefinition Definition => definition;
        public QuestDefinition.Step CurrentStep => currentStep;

        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }

        public event Action Completed;

        private void Start()
        {
            if (playOnStart)
                StartQuest();
        }

        [ContextMenu("Start Quest")]
        public void StartQuest()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            StopQuest();
            IsCompleted = false;

            if (definition == null)
            {
                Fail("Не назначен QuestDefinition.");
                return;
            }

            activeDefinition = definition;

            if (!activeDefinition.TryGetStep(
                    activeDefinition.EntryStepId, out var entry))
            {
                Fail("Не найден начальный шаг. Скомпилируй граф.");
                return;
            }

            IsRunning = true;
            EnterStep(entry);
        }

        private void EnterStep(QuestDefinition.Step step)
        {
            DetachTarget();

            currentStep = step;
            transitionPending = false;
            currentTarget = step.Target.Resolve(this);

            if (currentTarget == null)
            {
                Fail($"Не назначена цель шага '{step.Title}' ({step.Id}).");
                return;
            }

            currentTarget.OnStateChanged += OnTargetStateChanged;

            Debug.Log($"Начат шаг: {step.Title}", this);
        }

        private void OnTargetStateChanged(QuestTargetParam parameter)
        {
            if (!IsRunning || transitionPending)
                return;

            // Пока любое событие цели завершает шаг.
            transitionPending = true;

            // Повторные события этой цели больше не учитываются.
            DetachTarget();
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            if (!transitionPending)
            {
                if (currentTarget == null)
                    Fail($"Цель шага '{currentStep.Title}' уничтожена.");

                return;
            }

            transitionPending = false;

            if (currentStep.IsFinal)
            {
                CompleteQuest();
                return;
            }

            if (activeDefinition == null ||
                !activeDefinition.TryGetStep(
                    currentStep.NextStepId, out var nextStep))
            {
                Fail($"Не найден следующий шаг: {currentStep.NextStepId}.");
                return;
            }

            EnterStep(nextStep);
        }

        private void CompleteQuest()
        {
            StopQuest();
            IsCompleted = true;

            Debug.Log("Квест завершён.", this);
            Completed?.Invoke();
        }

        [ContextMenu("Stop Quest")]
        public void StopQuest()
        {
            DetachTarget();

            IsRunning = false;
            transitionPending = false;
            currentStep = null;
            activeDefinition = null;
        }

        private void Fail(string message)
        {
            StopQuest();
            Debug.LogError($"QuestRunner: {message}", this);
        }

        private void DetachTarget()
        {
            if (currentTarget != null)
                currentTarget.OnStateChanged -= OnTargetStateChanged;

            currentTarget = null;
        }

        private void OnDisable()
        {
            StopQuest();
        }

        public QuestTarget ResolveTarget(QuestTargetReference reference)
        {
            return reference.Resolve(this);
        }

        public Object GetReferenceValue(
            PropertyName id,
            out bool idValid)
        {
            foreach (var binding in bindings)
            {
                if (binding.Id != id)
                    continue;

                idValid = true;
                return binding.Value;
            }

            idValid = false;
            return null;
        }

        public void SetReferenceValue(PropertyName id, Object value)
        {
            foreach (var binding in bindings)
            {
                if (binding.Id != id)
                    continue;

                binding.Value = value;
                return;
            }

            bindings.Add(new Binding
            {
                Id = id,
                Value = value
            });
        }

        public void ClearReferenceValue(PropertyName id)
        {
            bindings.RemoveAll(binding => binding.Id == id);
        }

        [Serializable]
        private sealed class Binding
        {
            public PropertyName Id;
            public Object Value;
        }
    }
}