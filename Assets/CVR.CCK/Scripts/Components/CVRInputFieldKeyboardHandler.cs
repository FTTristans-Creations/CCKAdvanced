//#define CVR_CLIENT

using CVR.CCK;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if CVR_CLIENT
using ABI_RC.Core.InteractionSystem;
#endif

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Input Field Keyboard Handler")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-input-field-keyboard-handler")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRInputFieldKeyboardHandler : MonoBehaviour, IPointerClickHandler
    {
        #region Serialized Fields
        
        public enum KeyboardMode { OpenKeyboard, Disabled }

        [Tooltip("Whether the built-in keyboard should display or not.")]
        public KeyboardMode keyboardMode = KeyboardMode.OpenKeyboard;

        /// <summary>
        /// Fired prior to the user keyboard being opened when clicking on the input field.
        /// </summary>
        // public UnityEvent onBeforeKeyboardOpen; // TODO: Wait for feedback requesting this, pretty sure TMP_InputField already allows this.
        
        #endregion Serialized Fields

        #region Implementation
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (keyboardMode == KeyboardMode.Disabled) 
                return;
            
            if (TryGetComponent(out TMPro.TMP_InputField input) && input.isActiveAndEnabled)
            {
                // onBeforeKeyboardOpen.Invoke();
#if CVR_CLIENT
                ViewManager.Instance.openMenuKeyboard(input);
#endif
            }
            else if (TryGetComponent(out InputField inputField) && inputField.isActiveAndEnabled)
            {
                // onBeforeKeyboardOpen.Invoke();
#if CVR_CLIENT
                ViewManager.Instance.openMenuKeyboard(inputField);
#endif
            }
        }

        #endregion Implementation
    }
}
