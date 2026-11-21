using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripts.Editor
{
    public static class EnumFilter
    {
        public static TEnum FilteredEnumPopup<TEnum, TAttribute>(TEnum enumValue)
            where TEnum : struct, Enum
            where TAttribute : Attribute
        {
            return FilteredEnumPopup<TEnum, TAttribute>(enumValue, $"{typeof(TEnum).Name}:");
        }

        private static TEnum FilteredEnumPopup<TEnum, TAttribute>(TEnum enumValue, string label)
            where TEnum : struct, Enum
            where TAttribute : Attribute
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an enumerated type");

            // Get fields in declaration order, excluding any special fields
            var fields = typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(TEnum));

            // Filter fields based on attribute
            var filteredFields = fields
                .Where(field => field.GetCustomAttributes(typeof(TAttribute), false).Length > 0)
                .ToList();

            var filteredValues = filteredFields.Select(field => (TEnum)field.GetValue(null)).ToList();
            var filteredNames = filteredFields.Select(field => field.Name).ToList();

            if (!filteredValues.Contains(enumValue))
            {
                filteredValues.Add(enumValue);
                filteredNames.Add(enumValue.ToString());
                EditorGUILayout.HelpBox($"The current selection ({enumValue}) is not valid for the current context!", MessageType.Warning);
            }

            int currentIndex = filteredNames.IndexOf(enumValue.ToString());
            int selectedIndex = EditorGUILayout.Popup(label, currentIndex, filteredNames.ToArray());

            return selectedIndex >= 0 && selectedIndex < filteredValues.Count
                ? filteredValues[selectedIndex]
                : enumValue;
        }
        
        public static TEnum FilteredEnumPopup<TEnum>(Rect position, TEnum enumValue, List<TEnum> supportedTypes)
            where TEnum : struct, Enum
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an enumerated type");

            // Get all enum fields in declaration order
            var fields = typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(TEnum))
                .ToList();

            // Create a lookup of enum values to their declaration order
            var orderLookup = fields
                .Select((field, index) => ((TEnum)field.GetValue(null), index))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2);

            // Sort supportedTypes based on declaration order
            supportedTypes = supportedTypes
                .OrderBy(value => orderLookup.GetValueOrDefault(value, int.MaxValue))
                .ToList();

            // If enumValue is not in supportedTypes, default to the first supported type
            if (!supportedTypes.Contains(enumValue))
                enumValue = supportedTypes[0];

            var displayNames = supportedTypes.Select(e => e.ToString()).ToArray();
            int currentIndex = Array.IndexOf(displayNames, enumValue.ToString());

            int selectedIndex = EditorGUI.Popup(position, currentIndex, displayNames);

            return selectedIndex >= 0 && selectedIndex < supportedTypes.Count 
                ? supportedTypes[selectedIndex] 
                : enumValue;
        }
        
             
        /// <summary>
        /// Returns string to value mapping for all enum values of type TEnum that have the attribute TAttribute.
        /// Keeps the declaration order of the enum values.
        /// </summary>
        public static (string[], TEnum[]) GetFilteredEnumValues<TEnum, TAttribute>()
            where TEnum : struct, Enum
            where TAttribute : Attribute
        {
            Type enumType = typeof(TEnum);

            var fieldInfos = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

            var filteredValues = new List<TEnum>();
            var filteredNames = new List<string>();

            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (fieldInfo.GetCustomAttribute<TAttribute>(false) == null)
                    continue;

                string name = fieldInfo.Name;
                TEnum value = (TEnum)fieldInfo.GetValue(null);
                
                // Get pretty name (add spaces between words, excluding all caps words, but last one)
                // TestCOOLThing -> Test COOL Thing
                // TestThingCOOL -> Test Thing COOL
                
                string prettyName = string.Empty;
                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];
                    if (char.IsUpper(c) && i > 0 && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && !char.IsUpper(name[i + 1]))))
                        prettyName += " " + c;
                    else
                        prettyName += c;
                }
                
                filteredNames.Add(prettyName);
                filteredValues.Add(value);
            }

            return (filteredNames.ToArray(), filteredValues.ToArray());
        }
    }
}