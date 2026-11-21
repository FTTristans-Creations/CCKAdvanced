#if UNITY_EDITOR
using System.Collections.Generic;
using ABI.CCK.Components;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(CVRDataStore.KeyValueData))]
public class KeyValueDataDrawer : PropertyDrawer
{
    private static readonly Dictionary<string, ReorderableList> s_listCache = new();
    private static readonly string[] s_vector4Labels = { "X", "Y", "Z", "W" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty keyProp = property.FindPropertyRelative("key");
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty isArrayProp = property.FindPropertyRelative("isArray");
        float lh = EditorGUIUtility.singleLineHeight;

        // Key / Type / Array row
        Rect row = new(position.x, position.y, position.width, lh);
        const float spacing = 4f;
        const float arrayToggleW = 60f;
        float avail = row.width - arrayToggleW - spacing * 2;
        Rect keyRect = new(row.x, row.y, avail * .6f, lh);
        Rect typeRect = new(keyRect.xMax + spacing, row.y, avail * .4f, lh);
        Rect arrRect = new(typeRect.xMax + spacing, row.y, arrayToggleW, lh);
        keyProp.stringValue = EditorGUI.TextField(keyRect, keyProp.stringValue);
        typeProp.enumValueIndex = EditorGUI.Popup(typeRect, typeProp.enumValueIndex, typeProp.enumDisplayNames);
        isArrayProp.boolValue = EditorGUI.ToggleLeft(arrRect, "Array", isArrayProp.boolValue);

        // Value area
        position.y += lh + 2;
        CVRDataStore.EditorDataType editorDataType = (CVRDataStore.EditorDataType)typeProp.enumValueIndex;
        if (!TryGetArrayPropertyName(editorDataType, out var arrayName))
        {
            EditorGUI.EndProperty();
            return;
        }

        SerializedProperty arrayProp = property.FindPropertyRelative(arrayName);
        if (arrayProp == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        if (!isArrayProp.boolValue)
        {
            EnsureArraySize(arrayProp, 1);
            DrawElementField(new Rect(position.x, position.y, position.width, GetFieldHeight(editorDataType)), arrayProp, editorDataType, 0);
        }
        else
        {
            DrawReorderableList(new Rect(position.x, position.y, position.width, GetListHeightEstimate(arrayProp, editorDataType)), arrayProp, editorDataType);
        }

        EditorGUI.EndProperty();
    }

    private void DrawReorderableList(Rect rect, SerializedProperty arrayProp, CVRDataStore.EditorDataType editorDataType)
    {
        string key = $"{arrayProp.serializedObject.targetObject.GetInstanceID()}_{arrayProp.propertyPath}";
        if (!s_listCache.TryGetValue(key, out ReorderableList list))
        {
            list = new ReorderableList(arrayProp.serializedObject, arrayProp, true, true, true, true)
            {
                drawHeaderCallback = (r) =>
                {
                    SerializedProperty prop = list.serializedProperty;
                    float labelW = r.width - 80f;
                    Rect labelR = new(r.x, r.y, labelW, r.height);
                    Rect sizeR = new(labelR.xMax + 5f, r.y, 70f, r.height);
                    EditorGUI.LabelField(labelR, $"Array ({editorDataType})");
                    EditorGUI.BeginChangeCheck();
                    int newSize = EditorGUI.IntField(sizeR, prop.arraySize);
                    if (EditorGUI.EndChangeCheck() && newSize >= 0 && newSize != prop.arraySize)
                    {
                        prop.arraySize = newSize;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                },
                drawElementCallback = (elementRect, index, _, _) =>
                {
                    SerializedProperty prop = list.serializedProperty;
                    if (index >= prop.arraySize) return;
                    Rect r = elementRect; r.y += 1; r.height = GetFieldHeight(editorDataType);
                    DrawElementField(r, prop, editorDataType, index);
                },
                elementHeightCallback = _ => GetFieldHeight(editorDataType) + 2
            };
            s_listCache[key] = list;
        }

        // Ensure it points to the current SerializedProperty instance
        list.serializedProperty = arrayProp;
        list.DoList(rect);
    }

    private void DrawElementField(Rect rect, SerializedProperty arrayProp, CVRDataStore.EditorDataType editorDataType, int index)
    {
        if (index >= arrayProp.arraySize) return;
        switch (editorDataType)
        {
            case CVRDataStore.EditorDataType.UnityObject:
            {
                SerializedProperty el = arrayProp.GetArrayElementAtIndex(index);
                bool showComp = el.objectReferenceValue is GameObject or Component;
                Rect objRect = rect;
                if (showComp)
                {
                    const float btnW = 20f;
                    objRect.width -= btnW + 2;
                    Rect btnRect = new(objRect.xMax + 2, rect.y, btnW, rect.height);
                    if (GUI.Button(btnRect, EditorGUIUtility.IconContent("cs Script Icon"), EditorStyles.miniButtonMid)) ShowComponentMenu(el);
                }
                el.objectReferenceValue = EditorGUI.ObjectField(objRect, el.objectReferenceValue, typeof(Object), true);
                break;
            }
            case CVRDataStore.EditorDataType.String:
            case CVRDataStore.EditorDataType.Int:
            case CVRDataStore.EditorDataType.Float:
            case CVRDataStore.EditorDataType.Bool:
            case CVRDataStore.EditorDataType.Vector2:
            case CVRDataStore.EditorDataType.Vector2Int:
            case CVRDataStore.EditorDataType.Vector3:
            case CVRDataStore.EditorDataType.Vector3Int:
            case CVRDataStore.EditorDataType.Quaternion:
            case CVRDataStore.EditorDataType.Rect:
            case CVRDataStore.EditorDataType.RectInt:
            case CVRDataStore.EditorDataType.Bounds:
            case CVRDataStore.EditorDataType.BoundsInt:
            case CVRDataStore.EditorDataType.Color:
            case CVRDataStore.EditorDataType.LayerMask:
                EditorGUI.PropertyField(rect, arrayProp.GetArrayElementAtIndex(index), GUIContent.none);
                break;
            case CVRDataStore.EditorDataType.Vector4:
                DrawVector4Field(rect, arrayProp.GetArrayElementAtIndex(index));
                break;
            default:
                EditorGUI.LabelField(rect, "Unsupported Type");
                break;
        }
    }

    private void DrawVector4Field(Rect rect, SerializedProperty vec4Prop)
    {
        float colW = rect.width * 0.25f;
        float lblW = 12f;
        Vector4 cur = vec4Prop.vector4Value;
        Vector4 nv = cur;
        for (int i = 0; i < 4; i++)
        {
            float x = rect.x + colW * i;
            Rect l = new(x, rect.y, lblW, rect.height);
            Rect f = new(x + lblW, rect.y, colW - lblW - 2f, rect.height);
            EditorGUI.LabelField(l, s_vector4Labels[i]);
            nv[i] = EditorGUI.FloatField(f, cur[i]);
        }
        if (nv != cur) vec4Prop.vector4Value = nv;
    }

    private static void ShowComponentMenu(SerializedProperty objectReference)
    {
        int instanceId = objectReference.objectReferenceInstanceIDValue;
        GameObject gameObject = objectReference.objectReferenceValue switch
        {
            GameObject go => go,
            Component component => component.gameObject,
            _ => null
        };

        if (!gameObject) 
            return;
            
        // collect components on the GameObject and show a dropdown menu
            
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("GameObject"), gameObject.GetInstanceID() == instanceId, () =>
        {
            objectReference.objectReferenceValue = gameObject;
            objectReference.serializedObject.ApplyModifiedProperties();
        });
            
        menu.AddSeparator(string.Empty); // looks nice to separate root object from components
            
        var components = gameObject.GetComponents<Component>();
        for (var i = 0; i < components.Length; i++)
        {
            Component component = components[i]; // appending index so object with multiple of same component can be selected
            menu.AddItem(new GUIContent($"{i}. {component.GetType().Name}"), component.GetInstanceID() == instanceId, () =>
            {
                objectReference.objectReferenceValue = component;
                objectReference.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    private static void EnsureArraySize(SerializedProperty arr, int min)
    {
        while (arr.arraySize < min) arr.InsertArrayElementAtIndex(arr.arraySize);
    }

    private float GetFieldHeight(CVRDataStore.EditorDataType dt) =>
        dt is CVRDataStore.EditorDataType.Rect or CVRDataStore.EditorDataType.Bounds or CVRDataStore.EditorDataType.RectInt or CVRDataStore.EditorDataType.BoundsInt
            ? EditorGUIUtility.singleLineHeight * 2
            : EditorGUIUtility.singleLineHeight;

    private float GetListHeightEstimate(SerializedProperty arr, CVRDataStore.EditorDataType dt)
    {
        string key = $"{arr.serializedObject.targetObject.GetInstanceID()}_{arr.propertyPath}";
        if (s_listCache.TryGetValue(key, out ReorderableList cached))
        {
            cached.serializedProperty = arr;
            return cached.GetHeight();
        }
        int size = arr.arraySize;
        float header = EditorGUIUtility.singleLineHeight;
        float elem = GetFieldHeight(dt) + 2;
        float footer = EditorGUIUtility.singleLineHeight;
        return header + Mathf.Max(0, size) * elem + 4f + footer;
    }

    private static bool TryGetArrayPropertyName(CVRDataStore.EditorDataType dt, out string name)
    {
        switch (dt)
        {
            case CVRDataStore.EditorDataType.UnityObject: name = "unityObjectArray"; return true;
            case CVRDataStore.EditorDataType.String: name = "stringArray"; return true;
            case CVRDataStore.EditorDataType.Int: name = "intArray"; return true;
            case CVRDataStore.EditorDataType.Float: name = "floatArray"; return true;
            case CVRDataStore.EditorDataType.Bool: name = "boolArray"; return true;
            case CVRDataStore.EditorDataType.Color: name = "colorArray"; return true;
            case CVRDataStore.EditorDataType.LayerMask: name = "layerMaskArray"; return true;
            case CVRDataStore.EditorDataType.Vector2: name = "vector2Array"; return true;
            case CVRDataStore.EditorDataType.Vector3: name = "vector3Array"; return true;
            case CVRDataStore.EditorDataType.Rect: name = "rectArray"; return true;
            case CVRDataStore.EditorDataType.Bounds: name = "boundsArray"; return true;
            case CVRDataStore.EditorDataType.Quaternion: name = "quaternionArray"; return true;
            case CVRDataStore.EditorDataType.Vector2Int: name = "vector2IntArray"; return true;
            case CVRDataStore.EditorDataType.Vector3Int: name = "vector3IntArray"; return true;
            case CVRDataStore.EditorDataType.RectInt: name = "rectIntArray"; return true;
            case CVRDataStore.EditorDataType.BoundsInt: name = "boundsIntArray"; return true;
            case CVRDataStore.EditorDataType.Vector4: name = "vector4Array"; return true;
            default: name = null; return false;
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + 2; // Key/Type/Array row
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty isArrayProp = property.FindPropertyRelative("isArray");
        CVRDataStore.EditorDataType editorDataType = (CVRDataStore.EditorDataType)typeProp.enumValueIndex;
        if (!TryGetArrayPropertyName(editorDataType, out var arrayName))
            return height;
        SerializedProperty arrayProp = property.FindPropertyRelative(arrayName);
        if (arrayProp == null) return height;
        if (!isArrayProp.boolValue)
            height += GetFieldHeight(editorDataType);
        else
            height += GetListHeightEstimate(arrayProp, editorDataType);
        return height + 4f;
    }
}

[CustomEditor(typeof(CVRDataStore))]
public class CVRDataStoreEditor : Editor
{
    private ReorderableList _list;
    private void OnEnable()
    {
        if (!target) return;
        SerializedProperty prop = serializedObject.FindProperty("editorData");
        if (prop == null) return; // doesn't exist at runtime
        
        _list = new ReorderableList(serializedObject, prop, true, true, true, true)
        {
            drawHeaderCallback = r => EditorGUI.LabelField(r, "Data Store Items"),
            drawElementCallback = (r, i, a, f) => { SerializedProperty e = prop.GetArrayElementAtIndex(i); r.y += 1; EditorGUI.PropertyField(r, e, GUIContent.none, true); },
            elementHeightCallback = i => EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(i)) + 6,
            onAddCallback = l =>
            {
                int idx = prop.arraySize;
                prop.InsertArrayElementAtIndex(idx);
                SerializedProperty ne = prop.GetArrayElementAtIndex(idx);
                ne.FindPropertyRelative("key").stringValue = "NewKey";
                ne.FindPropertyRelative("type").enumValueIndex = 0;
                ne.FindPropertyRelative("isArray").boolValue = false;
                SerializedProperty arr = ne.FindPropertyRelative("unityObjectArray");
                if (arr is { arraySize: 0 }) arr.InsertArrayElementAtIndex(0);
                serializedObject.ApplyModifiedProperties();
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
#endif