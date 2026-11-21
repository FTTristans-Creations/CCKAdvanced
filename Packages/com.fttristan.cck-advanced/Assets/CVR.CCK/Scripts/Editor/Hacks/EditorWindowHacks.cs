using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace CVR.CCKEditor.Hacks
{
    public static class EditorWindowHacks
    {
        public static void FocusInspectorWindow()
        {
            var focused = EditorWindow.focusedWindow;
            if (focused && focused.GetType().Name == "InspectorWindow")
                return;

            var inspectors = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(w => w.GetType().Name == "InspectorWindow").ToList();

            if (inspectors.Count > 0)
            {
                var inspector = inspectors[0];
                inspector.Focus();
                TrySelectTab(inspector);
            }
            else
            {
                EditorWindow.GetWindow(Type.GetType("UnityEditor.InspectorWindow, UnityEditor"));
            }
        }

        private static void TrySelectTab(EditorWindow window)
        {
            var parentProp = typeof(EditorWindow).GetProperty("parent", BindingFlags.NonPublic | BindingFlags.Instance);
            var parent = parentProp?.GetValue(window);
            if (parent == null) return;

            var selectedTabProp = parent.GetType().GetProperty("selectedTab", BindingFlags.Public | BindingFlags.Instance);
            if (selectedTabProp == null) return;

            selectedTabProp.SetValue(parent, window);
        }
    }
}