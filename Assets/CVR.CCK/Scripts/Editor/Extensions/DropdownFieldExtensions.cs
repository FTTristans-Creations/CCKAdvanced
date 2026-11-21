using UnityEngine.UIElements;
using System.Reflection;

namespace CCK.CE.Hacks
{
    public static class DropdownFieldExtensions
    {
        private static readonly FieldInfo IndexFieldInfo = typeof(DropdownField).GetField("m_Index", 
                BindingFlags.NonPublic | BindingFlags.Instance);

        // Thanks unity for being super awesome.
        public static void SetValueWithoutNotify(this DropdownField dropdown, int newIndex)
        {
            var choices = dropdown.choices;
            if (newIndex < 0 || newIndex >= choices.Count) 
                return;

            // Set internal index directly
            IndexFieldInfo?.SetValue(dropdown, newIndex);
            dropdown.SetValueWithoutNotify(choices[newIndex]);
        }
    }
}