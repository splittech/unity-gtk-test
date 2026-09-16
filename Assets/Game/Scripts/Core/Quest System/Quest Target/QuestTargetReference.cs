using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public struct QuestTargetReference
    {
        public string BindingId;

        public QuestTarget Resolve(IExposedPropertyTable context)
        {
            if (string.IsNullOrEmpty(BindingId))
                return null;

            ExposedReference<QuestTarget> reference = new()
            {
                exposedName = new PropertyName(BindingId)
            };

            return reference.Resolve(context);
        }
    }
}