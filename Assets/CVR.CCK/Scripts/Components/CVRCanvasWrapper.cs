// #define CVR_CLIENT

using CVR.CCK;
using UnityEngine;
using UnityEngine.UI;

#if CVR_CLIENT
using ABI_RC.Core.Player;
using ABI_RC.Systems.VRModeSwitch;
#endif

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Canvas Wrapper")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-canvas-wrapper")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRCanvasWrapper : MonoBehaviour
    {
        #region Constants
        
        private const float MaxInteractionDistance = 100f;
        private const float DefaultColliderDepth = 0.0125f;
        
        #endregion Constants

        #region Public Properties
        
        // TODO: We probably need to check if the canvas is interactable via GraphicRaycaster & checking canvas.root?
        public bool IsCanvasEnabled => _canvas && _canvas.isActiveAndEnabled;
        
        #endregion Public Properties

        #region Serialized Fields
        
        [SerializeField, Tooltip("The maximum distance at which this canvas can be interacted with. This is measured from the hit point to the ray origin.")]
        public float interactionDistance = MaxInteractionDistance;

        #endregion Serialized Fields

        #region Private Fields
        
        private Canvas _canvas;
        private bool _isLegacyCanvas;
        
        #endregion Private Fields

        #region Static Methods
        
        /// <summary>
        /// Adds a CVRCanvasWrapper to legacy canvas objects that need it.
        /// TODO: Gate calling this behind a CCK version check, as this is only needed for legacy canvas handling
        /// </summary>
        /// <param name="canvas">The canvas to wrap</param>
        public static void AddForLegacyCanvas(Canvas canvas)
        {
            // if (canvas.renderMode != RenderMode.WorldSpace) 
            //     return; // Should we handle swapping this at runtime?
            
            GameObject go = canvas.gameObject;
            if (go.TryGetComponent(out CVRCanvasWrapper wrapper))
                return; // Already has a wrapper, no need to add another
                
            wrapper = go.AddComponent<CVRCanvasWrapper>();
            wrapper._isLegacyCanvas = true;
        }
        
        #endregion Static Methods

        #region Unity Events
        
        private void Start()
        {
            if (!TryGetComponent(out _canvas))
            {
                Debug.LogError("CVRCanvasWrapper requires a Canvas component on the GameObject.");
                Destroy(this);
                return;
            }
            
            SetupCanvas();
            UpdateWorldCamera();
            UpdateColliderForRenderMode();
            
#if CVR_CLIENT
            VRModeSwitchEvents.OnPostVRModeSwitch.AddListener(OnPostVRModeSwitch);
#endif
        }

        private void OnDestroy()
        {
#if CVR_CLIENT
            VRModeSwitchEvents.OnPostVRModeSwitch.RemoveListener(OnPostVRModeSwitch);
#endif
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            interactionDistance = Mathf.Clamp(interactionDistance, 0f, MaxInteractionDistance);
        }
#endif
        
        #endregion Unity Events

        #region Game Events
        
#if CVR_CLIENT
        private void OnPostVRModeSwitch(bool isVr) => UpdateWorldCamera();
#endif
        
        #endregion Game Events

        #region Public Methods
        
        /// <summary>
        /// Updates the BoxCollider enabled state based on the current canvas render mode.
        /// Call this when switching between screen space and world space at runtime.
        /// </summary>
        public void UpdateColliderForRenderMode()
        {
            if (!gameObject.TryGetComponent(out BoxCollider col))
                return; // No collider to update
                
            // Only enable collider for WorldSpace canvases
            bool shouldEnableCollider = _canvas.renderMode == RenderMode.WorldSpace;
            // if (col.enabled == shouldEnableCollider)
            //     return;
            
            col.enabled = shouldEnableCollider;
                
            // If we're enabling the collider, make sure it's properly configured
            if (shouldEnableCollider) ConfigureBoxColliderForRect(col);
        }
        
        #endregion Public Methods

        #region Private Methods
        
        private void UpdateWorldCamera()
        {
#if CVR_CLIENT
            _canvas.worldCamera = PlayerSetup.Instance.activeCam;
#else
            _canvas.worldCamera = Camera.main;
#endif
        }

        private void SetupCanvas()
        {
            GameObject go = gameObject;
            
            // People removed this component from the canvas due to it being not needed in the past.
            // Modern setups should add this component themselves.
            if (!go.TryGetComponent(out GraphicRaycaster _) && _isLegacyCanvas)
                go.AddComponent<GraphicRaycaster>();
            
            // Add or use the existing Box Collider
            if (!go.TryGetComponent(out BoxCollider _))
                go.AddComponent<BoxCollider>();
        }

        private void ConfigureBoxColliderForRect(BoxCollider col)
        {
            if (!gameObject.TryGetComponent(out RectTransform rectTransform)) 
                return; // Shouldn't happen, but just in case
            
            Vector3 newSize = new(
                rectTransform.sizeDelta.x,
                rectTransform.sizeDelta.y,
                DefaultColliderDepth / rectTransform.lossyScale.z);
                
            if (!IsFloatValid(newSize.x))
                newSize.x = DefaultColliderDepth;
            if (!IsFloatValid(newSize.y))
                newSize.y = DefaultColliderDepth;
            if (!IsFloatValid(newSize.z))
                newSize.z = DefaultColliderDepth;
                
            col.size = newSize;
            col.center = new Vector3(
                col.size.x * (0.5f - rectTransform.pivot.x), 
                col.size.y * (0.5f - rectTransform.pivot.y), 
                0f);
                
            col.isTrigger = true;
        }

        private static bool IsFloatValid(float val)
            => (!float.IsNaN(val) && !float.IsInfinity(val));
        
        #endregion Private Methods
    }
}
