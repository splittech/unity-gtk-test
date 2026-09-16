using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [RequireComponent(typeof(ButtonView))]
    public class ButtonQuestTarget : QuestTarget
    {
        private void Awake()
        {
            GetComponent<ButtonView>().OnClicked += OnButtonClicked;
        }

        private void OnButtonClicked()
        {
            InvokeOnStateChanged(new OnClicked());
        }

        public override List<QuestTargetParam> ProvideAvailableParamList()
        {
            return new() { new OnClicked() };
        }

        private class OnClicked : QuestTargetEmptyParam { };
    }
}