using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;

public class ValidationUsageTreeItem : VisualElement
{
    private const int IndentPerLevel = 20;
    private const int ButtonSize = 20;

    private readonly VisualElement _root;

    private readonly Dictionary<Object, HashSet<Object>> _parentToChildren;
    private readonly HashSet<Object> _rootObjects;

    public ValidationUsageTreeItem(Dictionary<Object, HashSet<Object>> parentToChildren, HashSet<Object> rootObjects = null)
    {
        _parentToChildren = parentToChildren ?? new Dictionary<Object, HashSet<Object>>();
        _rootObjects = rootObjects ?? FindRootObjects(_parentToChildren);

        _root = new VisualElement
        {
            name = "UsageTreeRoot",
            style =
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1,
                width = Length.Percent(100),
                overflow = Overflow.Hidden,
                maxWidth = Length.Percent(100)
            }
        };
        Add(_root);

        style.flexDirection = FlexDirection.Column;
        style.flexGrow = 1;
        style.overflow = Overflow.Hidden;
        style.width = Length.Percent(100);
        style.maxWidth = Length.Percent(100);

        BuildUI();
    }

    private static HashSet<Object> FindRootObjects(Dictionary<Object, HashSet<Object>> map)
    {
        var allChildren = new HashSet<Object>(map.Values.SelectMany(v => v));
        var roots = new HashSet<Object>(map.Keys);
        roots.ExceptWith(allChildren);
        return roots;
    }

    private void BuildUI()
    {
        _root.Clear();
        foreach (var rootObj in _rootObjects/*.OrderBy(GetSortKey)*/)
            CreateUIForSubtree(rootObj, _root, 0, isRoot: true);
    }

    private void CreateUIForSubtree(Object obj, VisualElement parentElement, int depth, bool isRoot)
    {
        var row = CreateRow(obj, depth, isRoot, out var toggle, out var iconElement, out var childrenContainer);
        parentElement.Add(row);

        if (_parentToChildren.TryGetValue(obj, out var children) && children.Count > 0)
        {
            var firstChild = children.FirstOrDefault();
            if (firstChild)
            {
                var content = EditorGUIUtility.ObjectContent(firstChild, firstChild.GetType());
                if (content.image) iconElement.style.backgroundImage = (StyleBackground)content.image;
            }

            childrenContainer.style.display = DisplayStyle.None;

            toggle.clicked += () =>
            {
                bool collapsed = childrenContainer.style.display == DisplayStyle.None;
                childrenContainer.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            };

            foreach (var child in children/*.OrderBy(GetSortKey)*/)
                CreateUIForSubtree(child, childrenContainer, depth + 1, isRoot: false);
        }
        else
        {
            toggle.style.display = DisplayStyle.None;
        }
    }

    private static VisualElement CreateRow(Object obj, int depth, bool isRoot, out Button toggle, out VisualElement iconElement, out VisualElement childrenContainer)
    {
        var container = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                maxWidth = Length.Percent(100),
                overflow = Overflow.Hidden
            }
        };

        int topMargin = isRoot ? 4 : 1;
        int adjustedDepth = isRoot ? 0 : (depth - 1);

        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = topMargin,
                marginLeft = (adjustedDepth * IndentPerLevel) + adjustedDepth*4,
                minHeight = ButtonSize,
                width = Length.Percent(100),
                maxWidth = Length.Percent(100),
                overflow = Overflow.Hidden
            }
        };

        if (!isRoot)
        {
            var arrowContainer = new VisualElement
            {
                style =
                {
                    width = ButtonSize,
                    height = ButtonSize,
                    position = Position.Relative,
                    marginLeft = 0,
                    marginRight = 4,
                    marginTop = 0,
                    marginBottom = 0,
                }
            };

            var arrow = new Label("↳")
            {
                style =
                {
                    fontSize = 14,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    position = Position.Absolute,
                    left = (ButtonSize / 2f) - 6,
                    top = 2,
                    width = ButtonSize,
                    height = ButtonSize,
                    color = new Color(0.7f, 0.7f, 0.7f, 1f)
                }
            };

            arrowContainer.Add(arrow);
            row.Add(arrowContainer);
        }

        toggle = new Button
        {
            style =
            {
                width = ButtonSize,
                height = ButtonSize,
                marginLeft = 0,
                marginRight = 4,
                marginTop = 0,
                marginBottom = 0,
            }
        };

        iconElement = new VisualElement
        {
            style =
            {
                width = ButtonSize - 4,
                height = ButtonSize - 4,
                marginLeft = 0,
                marginRight = 0,
                alignSelf = Align.Center,
                justifyContent = Justify.Center,
                alignItems = Align.Center,
                unityBackgroundScaleMode = ScaleMode.ScaleToFit,
            }
        };

        toggle.Add(iconElement);

        var field = new ObjectField
        {
            value = obj,
            allowSceneObjects = true,
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                minWidth = 100,
                maxWidth = Length.Percent(100),
                overflow = Overflow.Hidden,
                marginLeft = 0,
                marginRight = 0,
                marginTop = 0,
                marginBottom = 0,
            }
        };

        row.Add(toggle);
        row.Add(field);

        childrenContainer = new VisualElement
        {
            name = "Children",
            style =
            {
                flexDirection = FlexDirection.Column,
                display = DisplayStyle.None,
                maxWidth = Length.Percent(100),
                overflow = Overflow.Hidden
            }
        };

        container.Add(row);
        container.Add(childrenContainer);
        return container;
    }

    public void UpdateData(Dictionary<Object, HashSet<Object>> newParentToChildren, HashSet<Object> newRootObjects = null)
    {
        _parentToChildren.Clear();
        foreach (var kvp in newParentToChildren)
            _parentToChildren[kvp.Key] = kvp.Value;

        _rootObjects.Clear();
        var roots = newRootObjects ?? FindRootObjects(_parentToChildren);
        foreach (var r in roots)
            _rootObjects.Add(r);

        BuildUI();
    }

    public void ClearTree()
    {
        _root.Clear();
        _parentToChildren.Clear();
        _rootObjects.Clear();
    }
}
