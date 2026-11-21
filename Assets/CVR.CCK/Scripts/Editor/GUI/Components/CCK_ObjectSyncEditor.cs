using ABI.CCK.Components;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

[CustomEditor(typeof(CVRObjectSync))]
public class CCK_ObjectSyncEditor : Editor
{
    private SerializedProperty guidProp;
    
    private void OnEnable()
    {
        if (!target) return;
        guidProp = serializedObject.FindProperty(nameof(CVRObjectSync.guid));
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawNetworkIdField(guidProp);
        EditorGUILayout.HelpBox("This component will have this objects position and rotation synced over network.", MessageType.Info);
       
        serializedObject.ApplyModifiedProperties();
    }
}