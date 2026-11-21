#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCK.Editor.ContentBuilder;

public partial class BuilderTab
{
    private class LastBuildInfoSection : BuilderUISection
    {
        private readonly VisualElement _lastBuildInfoSection;
        private readonly Label _lastBuildSize;
        private readonly Label _lastBuildTime;
        private readonly Label _lastBuildDuration;
        private readonly Label _lastBuildPlatform;
        private readonly Label _lastBuildAction;
        private readonly Label _lastBuildResult;
        
        public LastBuildInfoSection(VisualElement root, BuilderState state) : base(root, state)
        {
            _lastBuildInfoSection = Root.Q<VisualElement>("BuildInfo");
            _lastBuildSize = Root.Q<Label>("label-build-size");
            _lastBuildTime = Root.Q<Label>("label-build-time");
            _lastBuildDuration = Root.Q<Label>("label-build-duration");
            _lastBuildPlatform = Root.Q<Label>("label-build-platform");
            _lastBuildAction = Root.Q<Label>("label-build-action");
            _lastBuildResult = Root.Q<Label>("label-build-result");

            ContentBuilderAPI.OnStatusUpdated += OnBuildStatusUpdated;
        }

        private void OnBuildStatusUpdated(ContentBuilderAPI.BuildStatus status)
        {
            if (status is ContentBuilderAPI.BuildStatus.None)
                RestoreFromState();
        }

        #region BuilderUISection Overrides
        
        public override void LockInterface(bool locked)
        {
            // Not needed
        }

        public override void ClearFields() // Called after a build has completed
        {
            // Not needed
        }

        public override void Dispose()
        {
            ContentBuilderAPI.OnStatusUpdated -= OnBuildStatusUpdated;
        }
        
        #endregion BuilderUISection Overrides

        public void RestoreFromState()
        {
            bool hasBuiltThisSession = !string.IsNullOrEmpty(LastBuildInfoState.BuildSize);
            _lastBuildInfoSection.style.display = hasBuiltThisSession ? DisplayStyle.Flex : DisplayStyle.None;
            _lastBuildSize.text = LastBuildInfoState.BuildSize;
            _lastBuildTime.text = LastBuildInfoState.BuildTime;
            _lastBuildDuration.text = LastBuildInfoState.BuildDuration;
            _lastBuildPlatform.text = LastBuildInfoState.BuildPlatform;
            _lastBuildAction.text = LastBuildInfoState.BuildPurpose.ToString();

            switch (LastBuildInfoState.BuildResult)
            {
                case ContentBuilderAPI.BuildStatus.Completed:
                    _lastBuildResult.text = "Succeeded";
                    _lastBuildResult.style.color = new Color(0f, 1f, 0.07843138f);
                    break;
                case ContentBuilderAPI.BuildStatus.Failed:
                    _lastBuildResult.text = "Failed";
                    _lastBuildResult.style.color = new Color(1f, 0.2f, 0.14f);
                    break;
                case ContentBuilderAPI.BuildStatus.Cancelled:
                    _lastBuildResult.text = "Canceled";
                    _lastBuildResult.style.color = new Color(0.34f, 0.72f, 1f);
                    break;
            }
        }
    }
}
#endif