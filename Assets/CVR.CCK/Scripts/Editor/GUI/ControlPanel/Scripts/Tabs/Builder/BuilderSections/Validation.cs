#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CVR.CCKEditor.ContentBuilder.Features;
using UnityEngine;
using UnityEngine.UIElements;
using CVR.CCKEditor.Validations;

public partial class BuilderTab
{
    private class ValidationSection : BuilderUISection
    {
        private Label _infoCountLabel;
        private Label _warningCountLabel;
        private Label _errorCountLabel;
        private VisualElement _validationList;
        private VisualTreeAsset _validationItemTemplate;
        private VisualElement _validationProgressBar;
        private CancellationTokenSource _validationCancellationSource;

        public event Action OnRequestRevalidate;
        public bool HasErrors { get; private set; }
        public bool IsValidating { get; private set; }

        public ValidationSection(VisualElement root, BuilderState state) : base(root, state)
        {
            CacheUIElements();
            LoadTemplate();
            ClearValidationList();
        }

        private void CacheUIElements()
        {
            var validationRoot = Root.Q("Validations");

            _infoCountLabel = validationRoot.Q<Label>("label-info-count");
            _warningCountLabel = validationRoot.Q<Label>("label-warning-count");
            _errorCountLabel = validationRoot.Q<Label>("label-error-count");
            _validationList = validationRoot.Q<VisualElement>("FoldoutBody");
            validationRoot.Q<Button>("btn-validations-refresh").clicked += () => OnRequestRevalidate?.Invoke();
        }

        private void LoadTemplate()
        {
            _validationItemTemplate = Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/Elements/ValidationItem");
            if (_validationItemTemplate == null)
            {
                Debug.LogError("[CCK] Failed to load ValidationItem template!");
            }
        }

        public void StartValidation()
        {
            // Cancel any existing validation
            CancelCurrentValidation();
            
            // Reset everything
            ClearValidationList();
            ResetCounters();
            
            // Create new cancellation token
            _validationCancellationSource = new CancellationTokenSource();
            IsValidating = true;
            
            // Show progress bar
            UpdateRefreshButton();
            ShowValidationProgressBar();
        }

        public void CancelCurrentValidation()
        {
            if (_validationCancellationSource != null)
            {
                _validationCancellationSource.Cancel();
                _validationCancellationSource.Dispose();
                _validationCancellationSource = null;
            }
            IsValidating = false;
        }

        public CancellationToken GetCancellationToken()
        {
            return _validationCancellationSource?.Token ?? CancellationToken.None;
        }

        private void ShowValidationProgressBar()
        {
            // Remove any existing progress bar
            RemoveValidationProgressBar();

            var barTemplate = Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/Elements/ValidationProgressBar");
            if (!barTemplate) return;
            
            _validationProgressBar = barTemplate.CloneTree();
            _validationList.Add(_validationProgressBar);

            var progress = _validationProgressBar.Q<ProgressBar>();
            if (progress != null)
            {
                progress.value = 0f;
                progress.title = "Starting validation...";
            }
        }

        private void RemoveValidationProgressBar()
        {
            if (_validationProgressBar == null) return;
            
            _validationProgressBar.RemoveFromHierarchy();
            _validationProgressBar = null;
        }
        
        public void UpdateValidationProgress(float progress01)
        {
            if (_validationProgressBar == null || !IsValidating) return;

            var progressBar = _validationProgressBar.Q<ProgressBar>();
            if (progressBar != null)
            {
                var clampedProgress = Mathf.Clamp01(progress01);
                progressBar.value = clampedProgress * 100f;
                progressBar.title = $"Validating... {Mathf.RoundToInt(progressBar.value)}%";
            }
        }

        public void CompleteValidation(List<ValidationResult> results)
        {
            // Mark validation as complete
            IsValidating = false;
            UpdateRefreshButton();
            
            // Remove progress bar
            RemoveValidationProgressBar();
            
            // Clear the list (but don't reset counters yet)
            _validationList?.Clear();
            
            // Process and display results
            ProcessValidationResults(results);
            
            // Clean up cancellation token
            if (_validationCancellationSource != null)
            {
                _validationCancellationSource.Dispose();
                _validationCancellationSource = null;
            }
        }

        private void ProcessValidationResults(List<ValidationResult> results)
        {
            int infoCount = 0, warningCount = 0, errorCount = 0;
            HasErrors = false;

            foreach (var result in results)
            {
                if (result.Severity == ValidationSeverity.None)
                    continue;

                if (result is DetailedValidationResult detailed)
                    _validationList.Add(new ValidationHelpBoxElement(detailed, () => { OnRequestRevalidate?.Invoke(); }));
                else
                    AddValidationItem(result);

                switch (result.Severity)
                {
                    case ValidationSeverity.Info:
                        infoCount += result.IssueCount;
                        break;
                    case ValidationSeverity.Warning:
                        warningCount += result.IssueCount;
                        break;
                    case ValidationSeverity.Error:
                        errorCount += result.IssueCount;
                        HasErrors = true;
                        break;
                }
            }

            // If no errors, show success message
            if (errorCount == 0)
            {
                infoCount++;
                DisplayAllGoodMessage();
            }

            UpdateCounters(infoCount, warningCount, errorCount);
        }

        private void AddValidationItem(ValidationResult result)
        {
            if (!_validationItemTemplate) return;

            var item = _validationItemTemplate.CloneTree();
            var helpBox = item.Q<HelpBox>("validationHelpBox");

            ConfigureHelpBox(helpBox, result);
            ConfigureActionButtons(item, result);

            _validationList.Add(item);
        }

        private void ConfigureHelpBox(HelpBox helpBox, ValidationResult result)
        {
            if (helpBox == null) return;
            
            helpBox.text = result.Message;
            helpBox.messageType = result.Severity switch
            {
                ValidationSeverity.Info => HelpBoxMessageType.Info,
                ValidationSeverity.Warning => HelpBoxMessageType.Warning,
                ValidationSeverity.Error => HelpBoxMessageType.Error,
                _ => HelpBoxMessageType.None
            };
        }

        private void ConfigureActionButtons(VisualElement item, ValidationResult result)
        {
            var autoFixButton = item.Q<Button>("btn-autofix");
            var selectButton = item.Q<Button>("btn-select");

            // TODO: This validation element has no usages where it makes use of these buttons anymore
            if (autoFixButton != null) autoFixButton.style.display = DisplayStyle.None;
            if (selectButton != null) selectButton.style.display = DisplayStyle.None;
        }

        private void UpdateCounters(int info, int warning, int error)
        {
            if (_infoCountLabel != null)
            {
                _infoCountLabel.text = info.ToString();
                _infoCountLabel.EnableInClassList("has-count", info > 0);
            }

            if (_warningCountLabel != null)
            {
                _warningCountLabel.text = warning.ToString();
                _warningCountLabel.EnableInClassList("has-count", warning > 0);
            }

            if (_errorCountLabel != null)
            {
                _errorCountLabel.text = error.ToString();
                _errorCountLabel.EnableInClassList("has-count", error > 0);
            }
        }

        private void ResetCounters()
        {
            UpdateCounters(0, 0, 0);
            HasErrors = false;
        }

        private void DisplayAllGoodMessage()
        {
            if (!_validationItemTemplate) return;

            var item = _validationItemTemplate.CloneTree();
            var helpBox = item.Q<HelpBox>("validationHelpBox");

            if (helpBox != null)
            {
                helpBox.text = CVR.CCKEditor.Localization.CCKLocalizationManager.GetString("Validations.ALL_GOOD_MESSAGE");
                helpBox.messageType = HelpBoxMessageType.Info;
            }

            var autoFixButton = item.Q<Button>("btn-autofix");
            var selectButton = item.Q<Button>("btn-select");
            
            if (autoFixButton != null)
                autoFixButton.style.display = DisplayStyle.None;
            
            if (selectButton != null)
                selectButton.style.display = DisplayStyle.None;

            _validationList.Insert(0, item);
        }

        private void UpdateRefreshButton(bool locked = false)
        {
            var validationRoot = Root.Q("Validations");
            var refreshButton = validationRoot?.Q<Button>("btn-validations-refresh");
            refreshButton?.SetEnabled(!locked && !IsValidating);
        }

        public override void LockInterface(bool locked)
        {
            UpdateRefreshButton(locked);
        }

        public override void ClearFields()
        {
            CancelCurrentValidation();
            ClearValidationList();
            ResetCounters();
        }

        private void ClearValidationList()
        {
            HasErrors = false;
            IsValidating = false;
            _validationList?.Clear();
            RemoveValidationProgressBar();
        }

        public override void Dispose()
        {
            CancelCurrentValidation();
            ClearValidationList();
            _validationItemTemplate = null;
        }
    }
}
#endif