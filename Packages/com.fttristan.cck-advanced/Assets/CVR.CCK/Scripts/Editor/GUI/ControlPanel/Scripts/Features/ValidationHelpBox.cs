using CVR.CCKEditor.Validations;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace CVR.CCKEditor.ContentBuilder.Features
{
    public class ValidationHelpBoxElement : VisualElement
    {
        private readonly Button _btnSelect;
        private readonly Button _btnAutofix;
        private readonly Button _btnDocs;

        public ValidationHelpBoxElement(DetailedValidationResult detailed, System.Action revalidateCallback = null)
        {
            var tree = Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/Elements/ValidationItem2");
            if (!tree) return;

            tree.CloneTree(this);

            var messageLabel = this.Q<Label>("label-validation-message");
            var iconElement = this.Q<VisualElement>("image-validation-severity");
            _btnSelect = this.Q<Button>("btn-select");
            _btnAutofix = this.Q<Button>("btn-autofix");
            _btnDocs = this.Q<Button>("btn-docs");
            var foldoutBody = this.Q<VisualElement>("FoldoutBody");

            messageLabel.text = detailed.Message;
            iconElement.style.backgroundImage = new StyleBackground(GetIcon(detailed.Severity));

            // TODO: This currently cannot be hidden because of how the UXML is structured.
            var usageTree = new ValidationUsageTreeItem(detailed.Hierarchy, detailed.RootObjects);
            foldoutBody.Add(usageTree);

            _btnSelect.style.display = detailed.DisplayShowAllButton ? DisplayStyle.Flex : DisplayStyle.None;
            _btnAutofix.style.display = detailed.DisplayAutoFixButton ? DisplayStyle.Flex : DisplayStyle.None;
            _btnDocs.style.display = detailed.DisplayOpenDocsButton ? DisplayStyle.Flex : DisplayStyle.None;

            if (detailed.DisplayShowAllButton)
                _btnSelect.clicked += detailed.SelectAll;

            if (detailed.DisplayAutoFixButton)
                _btnAutofix.clicked += () =>
                {
                    detailed.AutoFix?.Invoke();
                    revalidateCallback?.Invoke();
                };

            if (detailed.DisplayOpenDocsButton)
                _btnDocs.clicked += detailed.OpenDocs;
        }

        private static Texture2D GetIcon(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Info => EditorGUIUtility.IconContent("console.infoicon").image as Texture2D,
                ValidationSeverity.Warning => EditorGUIUtility.IconContent("console.warnicon").image as Texture2D,
                ValidationSeverity.Error => EditorGUIUtility.IconContent("console.erroricon").image as Texture2D,
                _ => null
            };
        }
    }
}