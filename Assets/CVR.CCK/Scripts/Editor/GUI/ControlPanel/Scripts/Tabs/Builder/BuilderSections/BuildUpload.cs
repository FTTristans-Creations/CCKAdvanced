#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder;

public partial class BuilderTab
{
    private class BuildUploadSection : BuilderUISection
    {
        private VisualElement _buildTargetButton;
        private VisualElement _buildSelector; // Upload, LocalTest
        private Label _buildUploadBtn;
        private Button _backBtn;
        private TextField _changelogInput;
        private Toggle _legalCopyrightToggle;
        private Toggle _legalTagsToggle;
        private VisualElement _slideContainer;
        private VisualElement _buildUploadBox;
        private VisualElement _buildPage;
        private VisualElement _uploadPage;
        private ProgressBar _uploadProgress;
        private VisualElement _progressContainer;
        private Button _dismissButton;

        private VisualElement _buildInfoBlock;
        private VisualElement _uploadInfoBlock;

        // Transition State
        private bool _isTransitioning;
        private float _transitionStartTime;
        private float _startTranslateX;
        private float _targetTranslateX;
        private float _startHeight;
        private float _targetHeight;
        private const float TransitionDuration = 0.3f;
        
        private bool _isBuilding;

        public event Func<LegalAssurance, Task> OnUploadStartRequested;
        public event Action OnCancelRequested; 

        public BuildUploadSection(VisualElement root, BuilderState state) : base(root, state)
        {
            CacheUIElements();
            SetupEventHandlers();
            SetupProgressUI();
            UpdateBuildTargetIcon();
        }

        private void CacheUIElements()
        {
            _buildTargetButton = Root.Q<VisualElement>("btn-target-build-platform");
            _buildSelector = Root.Q<VisualElement>("selector-build");
            _buildUploadBtn = Root.Q<Label>("btn-build");
            _backBtn = Root.Q<Button>("btn-back");
            _changelogInput = Root.Q<TextField>("field-content-changelog");
            _legalCopyrightToggle = Root.Q<Toggle>("cck-legal-toggle-1");
            _legalTagsToggle = Root.Q<Toggle>("cck-legal-toggle-2");
            
            _buildInfoBlock = Root.Q<VisualElement>("info-test");
            _uploadInfoBlock = Root.Q<VisualElement>("info-upload");

            _slideContainer = Root.Q("SlideContainer");
            _buildUploadBox = Root.Q("Build");
            _buildPage = Root.Q("BuildPage");
            _uploadPage = Root.Q("UploadPage");
            _uploadProgress = Root.Q<ProgressBar>("UploadProgress");
            _progressContainer = Root.Q("ProgressContainer");
            _dismissButton = Root.Q<Button>("btn-dismiss");
        }

        private void SetupEventHandlers()
        {
            SetupBuildTargetHandlers();
            SetupBuildUploadHandlers();
            SetupLegalHandlers();
            SetupProgressHandlers();
        }

        private void SetupBuildTargetHandlers()
        {
            _buildTargetButton.RegisterCallback<MouseUpEvent>(_ => ShowBuildTargetMenu());
        }

        private void SetupBuildUploadHandlers()
        {
            _buildSelector.RegisterCallback<ClickEvent>(ShowBuildSelectorMenu);
            
            _buildUploadBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                if (State.BuildPurpose == BuildPurpose.OnlinePublish)
                {
                    _buildUploadBtn.MarkDirtyRepaint();
                    if (!State.IsUploadPage) TransitionToPage(true);
                    return;
                }
                
                // Local Test
                HandleUploadRequest();
            });

            _backBtn.clicked += () =>
            {
                if (State.IsUploadPage) TransitionToPage(false);
            };

            // Build to Upload
            Root.Q<Button>("btn-upload").clicked += () =>
            {
                if (State.IsUploadPage) HandleUploadRequest();
            };
        }

        private void SetupLegalHandlers()
        {
            _changelogInput.RegisterValueChangedCallback(evt => State.Changelog = evt.newValue);
            _legalCopyrightToggle.RegisterValueChangedCallback(evt => State.LegalCopyrightAccepted = evt.newValue);
            _legalTagsToggle.RegisterValueChangedCallback(evt => State.LegalTagsAccepted = evt.newValue);
        }

        private void SetupProgressUI()
        {
            _progressContainer.style.display = DisplayStyle.None;
            _dismissButton.clicked += () => _progressContainer.style.display = DisplayStyle.None;
        }

        private void SetupProgressHandlers()
        {
            _dismissButton.clicked += () =>
            {
                if (_isBuilding) OnCancelRequested.Invoke(); // acts as cancel button now
                ShowProgress(false);
            };
        }

        public void ShowProgress(bool show)
        {
            _progressContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                _uploadProgress.value = 0;
                _uploadProgress.title = "Starting...";
            }
        }

        public void UpdateProgress(float progress, string details)
        {
            _uploadProgress.value = progress;
            _uploadProgress.title = details;
        }

        private void ShowBuildSelectorMenu(ClickEvent _)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Online Publish"),
                State.BuildPurpose == BuildPurpose.OnlinePublish,
                () =>
                {
                    State.BuildPurpose = BuildPurpose.OnlinePublish;
                    UpdateBuildInfoAndText();
                });

            menu.AddItem(new GUIContent("Local Test"),
                State.BuildPurpose == BuildPurpose.LocalTest,
                () =>
                {
                    State.BuildPurpose = BuildPurpose.LocalTest;
                    UpdateBuildInfoAndText();
                });

            menu.ShowAsContext();
        }
        
        private void ShowBuildTargetMenu()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Windows"),
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64,
                () => RequestBuildTargetChange(BuildTarget.StandaloneWindows64));

            menu.AddItem(new GUIContent("Android"),
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                () => RequestBuildTargetChange(BuildTarget.Android));

            menu.ShowAsContext();
        }

        private void RequestBuildTargetChange(BuildTarget target)
        {
            if (EditorUtility.DisplayDialog("Switch Build Target",
                    $"Are you sure you want to switch the build target to {target}?",
                    "Yes", "No"))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildPipeline.GetBuildTargetGroup(target), target);
                UpdateBuildTargetIcon();
            }
        }

        private void UpdateBuildTargetIcon()
        {
            _buildTargetButton.EnableInClassList("cck-build-platform-windows",
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64);
            _buildTargetButton.EnableInClassList("cck-build-platform-android",
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
        }

        private async void HandleUploadRequest()
        {
            if (_isBuilding)
                return;

            var legalAssurance = new LegalAssurance(
                _legalCopyrightToggle.value, 
                _legalTagsToggle.value
            );
            
            try
            {
                if (OnUploadStartRequested != null)
                {
                    await OnUploadStartRequested.Invoke(legalAssurance);
                }
            }
            finally
            {
                _isBuilding = false;
            }
        }

        public void SetIsUploading(bool isUploading)
        {
            _isBuilding = isUploading;
        }
        
        public void TransitionToPage(bool toUpload)
        {
            if (_isTransitioning) return;
        
            _isTransitioning = true;
            _transitionStartTime = (float)EditorApplication.timeSinceStartup;
        
            float containerWidth = _buildUploadBox.resolvedStyle.width;
            _startTranslateX = _slideContainer.style.translate.value.x.value;
            _targetTranslateX = toUpload ? -containerWidth : 0;
        
            _startHeight = _buildUploadBox.resolvedStyle.height;
            _targetHeight = (toUpload ? _uploadPage : _buildPage).resolvedStyle.height + 10;
        
            State.IsUploadPage = toUpload;
        }

        public void HandleLayout()
        {
            if (_isTransitioning)
            {
                UpdateTransition();
            }
            else if (_buildUploadBox != null)
            {
                UpdateStaticLayout();
            }
        }

        private void UpdateTransition()
        {
            float elapsedTime = (float)EditorApplication.timeSinceStartup - _transitionStartTime;
            float progress = Mathf.Clamp01(elapsedTime / TransitionDuration);
            progress = Mathf.SmoothStep(0, 1, progress);
        
            float currentTranslateX = Mathf.Lerp(_startTranslateX, _targetTranslateX, progress);
            _slideContainer.style.translate = new StyleTranslate(
                new Translate(new Length(currentTranslateX, LengthUnit.Pixel), 
                    new Length(0, LengthUnit.Pixel))
            );
        
            float currentHeight = Mathf.Lerp(_startHeight, _targetHeight, progress);
            _buildUploadBox.style.height = currentHeight;
        
            if (progress >= 1) _isTransitioning = false;
            
            Root.MarkDirtyRepaint();
        }

        private void UpdateStaticLayout()
        {
            float containerWidth = _buildUploadBox.resolvedStyle.width;
            float targetTranslateX = State.IsUploadPage ? -containerWidth : 0;
        
            _slideContainer.style.translate = new StyleTranslate(
                new Translate(new Length(targetTranslateX, LengthUnit.Pixel), 
                    new Length(0, LengthUnit.Pixel))
            );
        
            float targetHeight = (State.IsUploadPage ? _uploadPage : _buildPage).resolvedStyle.height + 10;
            _buildUploadBox.style.height = targetHeight;
        }

        public void RestoreFromState()
        {
            _changelogInput.value = State.Changelog;
            _legalCopyrightToggle.value = State.LegalCopyrightAccepted;
            _legalTagsToggle.value = State.LegalTagsAccepted;
            UpdateBuildInfoAndText();
    
            if (State.IsUploadPage) _buildUploadBox.MarkDirtyRepaint();
        }

        public override void LockInterface(bool locked)
        {
            _buildUploadBtn.SetEnabled(!locked && !_isBuilding);
            _backBtn.SetEnabled(!locked && !_isBuilding);
            _changelogInput.SetEnabled(!locked && !_isBuilding);
            _legalCopyrightToggle.SetEnabled(!locked && !_isBuilding);
            _legalTagsToggle.SetEnabled(!locked && !_isBuilding);
            _buildTargetButton.SetEnabled(!locked && !_isBuilding);

            UpdateBuildInfoAndText();
        }

        public override void ClearFields()
        {
            _changelogInput.value = string.Empty;
            // _legalCopyrightToggle.value = false;
            // _legalTagsToggle.value = false;
            //ShowProgress(false);
        }

        public void PopulateFields(CVRApiContent content)
        {
            if (content == null) return;
            
            // Restore last used build state if available
            _changelogInput.value = State.Changelog;
            _legalCopyrightToggle.value = State.LegalCopyrightAccepted;
            _legalTagsToggle.value = State.LegalTagsAccepted;
        }

        private void UpdateBuildInfoAndText()
        {
            if (State.BuildPurpose == BuildPurpose.OnlinePublish)
            {
                _buildInfoBlock.style.display = DisplayStyle.None;
                _uploadInfoBlock.style.display = DisplayStyle.Flex;
                _buildUploadBtn.text = _isBuilding 
                    ? CVR.CCKEditor.Localization.CCKLocalizationManager.GetString("ContentBuilder.BUILD_UPLOADING")
                    : CVR.CCKEditor.Localization.CCKLocalizationManager.GetString("ContentBuilder.BUILD_UPLOAD");
            }
            else if (State.BuildPurpose == BuildPurpose.LocalTest)
            {
                _buildInfoBlock.style.display = DisplayStyle.Flex;
                _uploadInfoBlock.style.display = DisplayStyle.None;
                _buildUploadBtn.text = _isBuilding
                    ? CVR.CCKEditor.Localization.CCKLocalizationManager.GetString("ContentBuilder.BUILD_TESTING")
                    : CVR.CCKEditor.Localization.CCKLocalizationManager.GetString("ContentBuilder.BUILD_TEST");
            }
        }

        public override void Dispose()
        {
            // Clean up any ongoing transitions
            _isTransitioning = false;
            _isBuilding = false;
        }
    }
}
#endif