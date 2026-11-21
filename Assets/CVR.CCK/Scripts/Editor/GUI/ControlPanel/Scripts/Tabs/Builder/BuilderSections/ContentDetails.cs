#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CVR.CCKEditor.API;

public partial class BuilderTab
{
    private class ContentDetailsSection : BuilderUISection
    {
        private TextField _nameInput;
        private TextField _descriptionInput;
        private VisualElement _thumbnailContainer;
        private Label _thumbnailLabel;
        private VisualElement _thumbnailOverlay;

        public event Action OnThumbnailCaptureRequested;
        public event Action OnThumbnailSelectRequested;

        public ContentDetailsSection(VisualElement root, BuilderState state) : base(root, state)
        {
            CacheUIElements();
            SetupEventHandlers();
        }
        
        private void CacheUIElements()
        {
            _nameInput = Root.Q<TextField>("input-name");
            _descriptionInput = Root.Q<TextField>("input-description");
            _thumbnailContainer = Root.Q("Thumbnail");
            _thumbnailLabel = _thumbnailContainer.Q<Label>("ThumbnailLabel");
            _thumbnailOverlay = _thumbnailContainer.Q("HoverOverlay");
        }

        private void SetupEventHandlers()
        {
            _nameInput.RegisterValueChangedCallback(evt =>
            {
                State.Name = evt.newValue;
            });

            _descriptionInput.RegisterValueChangedCallback(evt =>
            {
                State.Description = evt.newValue;
            });

            _thumbnailContainer.RegisterCallback<MouseEnterEvent>(_ =>
            {
                _thumbnailLabel.style.display = DisplayStyle.None;
                _thumbnailOverlay.style.display = DisplayStyle.Flex;
            });

            _thumbnailContainer.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                _thumbnailLabel.style.display = State.Thumbnail != null ? DisplayStyle.None : DisplayStyle.Flex;
                _thumbnailOverlay.style.display = DisplayStyle.None;
            });

            // Visual elements for thumbnail buttons
            var captureButton = Root.Q("btn-capture-image");
            var selectButton = Root.Q("btn-select-image");

            captureButton.RegisterCallback<MouseUpEvent>(_ => OnThumbnailCaptureRequested?.Invoke());
            selectButton.RegisterCallback<MouseUpEvent>(_ => OnThumbnailSelectRequested?.Invoke());

            // Set initial button states
            captureButton.EnableInClassList("clickable", true);
            selectButton.EnableInClassList("clickable", true);
        }

        public void RestoreFromState()
        {
            _nameInput.SetValueWithoutNotify(State.Name);
            _descriptionInput.SetValueWithoutNotify(State.Description);
            UpdateThumbnail(State.Thumbnail, State.SetNewThumbnail);
        }

        public void UpdateThumbnail(Texture2D texture, bool setNewThumbnail)
        {
            // if (texture) Debug.Log($"Setting texture {texture.GetInstanceID()}");
            // else Debug.Log("Clearing texture");
            
            State.Thumbnail = texture;
            State.SetNewThumbnail = setNewThumbnail;
            
            _thumbnailContainer.style.backgroundImage = new StyleBackground(texture);
            _thumbnailLabel.style.display = texture != null ? DisplayStyle.None : DisplayStyle.Flex;
            _thumbnailOverlay.style.display = DisplayStyle.None;
        }

        public override void LockInterface(bool locked)
        {
            _nameInput.SetEnabled(!locked);
            _descriptionInput.SetEnabled(!locked);
            Root.Q("btn-capture-image").SetEnabled(!locked);
            Root.Q("btn-select-image").SetEnabled(!locked);

            // Update visual state for thumbnail buttons
            Root.Q("btn-capture-image").EnableInClassList("disabled", locked);
            Root.Q("btn-select-image").EnableInClassList("disabled", locked);
        }

        public override void ClearFields()
        {
            _nameInput.value = string.Empty;
            _descriptionInput.value = string.Empty;
            
            _thumbnailContainer.style.backgroundImage = null;
            _thumbnailContainer.style.backgroundColor = Color.clear; // BRO
            _thumbnailLabel.style.display = DisplayStyle.Flex;
            _thumbnailOverlay.style.display = DisplayStyle.None;

            State.Name = null;
            State.Description = null;
            State.Thumbnail = null;
            State.SetNewThumbnail = false;
        }

        public void PopulateFields(CVRApiContent content)
        {
            if (content == null) return;

            State.Name = _nameInput.value = content.Name;
            State.Description = _descriptionInput.value = content.Description;
        }

        public override void Dispose()
        {
            OnThumbnailCaptureRequested = null;
            OnThumbnailSelectRequested = null;
        }
    }
}
#endif