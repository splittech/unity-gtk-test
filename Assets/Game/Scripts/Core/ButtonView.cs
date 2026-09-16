using System;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    [RequireComponent(typeof(Button))]
    public class ButtonView : MonoBehaviour
    {
        public event Action OnClicked;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(ClickButton);
        }

        [Button]
        private void ClickButton()
        {
            OnClicked?.Invoke();
        }
    }
}