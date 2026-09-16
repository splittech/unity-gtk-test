using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public abstract class QuestTarget : MonoBehaviour
    {
        public event Action<QuestTargetParam> OnStateChanged;

        protected void InvokeOnStateChanged(QuestTargetParam param)
        {
            OnStateChanged?.Invoke(param);
        }

        public abstract List<QuestTargetParam> ProvideAvailableParamList();
    }
}
