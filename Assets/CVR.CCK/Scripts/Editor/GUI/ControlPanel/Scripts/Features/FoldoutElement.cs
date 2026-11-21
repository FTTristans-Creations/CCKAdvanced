using UnityEngine.UIElements;

public class FoldoutHeader : VisualElement {
    private readonly VisualElement _arrow;
    private VisualElement _body;
    private bool _folded;
    
    public bool Folded {
        get => _folded;
        set {
            if (_folded == value) return;
            _folded = value;
            UpdateFoldedState();
        }
    }
    
    public FoldoutHeader() {
       style.flexDirection = FlexDirection.Row;
       _arrow = new() {name = "arrow"};
       _arrow.AddToClassList("cck-foldout-arrow");
       _arrow.AddToClassList("cck-foldout-arrow-on");
       Add(_arrow);
       
       RegisterCallback<GeometryChangedEvent>(FindBody);
       RegisterCallback<MouseDownEvent>(OnClick);
    }

    private void FindBody(GeometryChangedEvent _) {
       foreach (VisualElement child in parent.Children()) {
          if (child is FoldoutBody) {
              _body = child;
              break;
          }
       }
       UnregisterCallback<GeometryChangedEvent>(FindBody);
       UpdateFoldedState();
    }

    private void OnClick(MouseDownEvent e) {
       if (_body == null) return;
       e.StopPropagation();
       Folded = !Folded;
    }
    
    private void UpdateFoldedState() {
       if (_body == null) return;
       
       if (_folded) {
          _arrow.RemoveFromClassList("cck-foldout-arrow-on");
          _body.style.display = DisplayStyle.None;
       } else {
          _arrow.AddToClassList("cck-foldout-arrow-on");
          _body.style.display = DisplayStyle.Flex;
       }
    }
    
    public class FoldoutHeaderFactory : UxmlFactory<FoldoutHeader, FoldoutHeaderTraits> {}
    
    public class FoldoutHeaderTraits : UxmlTraits {
        private readonly UxmlBoolAttributeDescription _folded = new()
        {
            name = "folded",
            defaultValue = false
        };
        
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            if (ve is FoldoutHeader foldoutHeader) foldoutHeader.Folded = _folded.GetValueFromBag(bag, cc);
        }
    }
}

public class FoldoutBody : VisualElement {
	public class FoldoutBodyFactory : UxmlFactory<FoldoutBody> {}
}