#if UNITY_EDITOR
using System;
using CVR.CCK;
using UnityEngine;
using UnityEngine.UIElements;
using CVR.CCKEditor.API;

public partial class BuilderTab
{
    private class ContentTagsSection : BuilderUISection
    {
        private Toggle[] _contentTags;
        private readonly string[] _tagIds = {
            "btn-screen-effects",
            "btn-flashing-effects",
            "btn-jumpscare",
            "btn-suggestive",
            "btn-violence",
            "btn-horror",
            "btn-gore",
            "btn-explicit"
        };

        public event Action<UgcTagsData> OnTagsChanged;

        public ContentTagsSection(VisualElement root, BuilderState state) : base(root, state)
        {
            InitializeToggles();
            SetupHelpButton();
        }

        private void InitializeToggles()
        {
            _contentTags = new Toggle[_tagIds.Length];
            
            for (int i = 0; i < _tagIds.Length; i++)
            {
                _contentTags[i] = Root.Q<Toggle>(_tagIds[i]);
                var index = i; // Capture for lambda
                _contentTags[i].RegisterValueChangedCallback(_ => 
                {
                    UpdateTags();
                    UpdateToggleStyle(_contentTags[index]);
                });
            }
        }

        private void UpdateToggleStyle(Toggle toggle)
        {
            toggle.EnableInClassList("selected", toggle.value);
        }

        private void SetupHelpButton()
        {
            Root.Q<Button>("btn-content-tags").clicked += () => Application.OpenURL(WebLinks.CCKDocsContentTagsUrl);
        }

        public void RestoreFromState()
        {
            SetToggleValueWithoutNotify(0, State.ScreenEffects);
            SetToggleValueWithoutNotify(1, State.FlashingEffects);
            SetToggleValueWithoutNotify(2, State.Jumpscare);
            SetToggleValueWithoutNotify(3, State.Suggestive);
            SetToggleValueWithoutNotify(4, State.Violence);
            SetToggleValueWithoutNotify(5, State.Horror);
            SetToggleValueWithoutNotify(6, State.Gore);
            SetToggleValueWithoutNotify(7, State.Explicit);
        }

        private void SetToggleValueWithoutNotify(int index, bool value)
        {
            _contentTags[index].SetValueWithoutNotify(value);
            UpdateToggleStyle(_contentTags[index]);
        }

        private void UpdateTags()
        {
            if (_contentTags == null) return;

            var tags = new UgcTagsData
            {
                ScreenEffects = GetToggleValue(0),
                FlashingEffects = GetToggleValue(1),
                Jumpscare = GetToggleValue(2),
                Suggestive = GetToggleValue(3),
                Violence = GetToggleValue(4),
                Horror = GetToggleValue(5),
                Gore = GetToggleValue(6),
                Explicit = GetToggleValue(7)
            };

            State.UpdateTags(tags);
            OnTagsChanged?.Invoke(tags);
        }

        private bool GetToggleValue(int index)
        {
            return _contentTags != null && 
                   index < _contentTags.Length && 
                   _contentTags[index] != null && 
                   _contentTags[index].value;
        }

        public override void LockInterface(bool locked)
        {
            if (_contentTags == null) return;

            foreach (var tag in _contentTags)
            {
                if (tag != null)
                {
                    tag.SetEnabled(!locked);
                    tag.EnableInClassList("disabled", locked);
                }
            }
        }

        public override void ClearFields()
        {
            foreach (var tag in _contentTags)
            {
                tag.value = false;
                UpdateToggleStyle(tag);
            }
        }

        public void PopulateFields(CVRApiContent content)
        {
            if (content?.ContentTags == null) return;

            SetToggleValueWithoutNotify(0, content.ContentTags.ScreenEffects);
            SetToggleValueWithoutNotify(1, content.ContentTags.FlashingEffects);
            SetToggleValueWithoutNotify(2, content.ContentTags.Jumpscare);
            SetToggleValueWithoutNotify(3, content.ContentTags.Suggestive);
            SetToggleValueWithoutNotify(4, content.ContentTags.Violence);
            SetToggleValueWithoutNotify(5, content.ContentTags.Horror);
            SetToggleValueWithoutNotify(6, content.ContentTags.Gore);
            SetToggleValueWithoutNotify(7, content.ContentTags.Explicit);

            State.UpdateTags(content.ContentTags);
        }

        public override void Dispose()
        {
            // No disposal needed for this section
        }
    }
}
#endif