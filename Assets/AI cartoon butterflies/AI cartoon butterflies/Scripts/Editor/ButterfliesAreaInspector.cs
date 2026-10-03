using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ButterfliesArea))]
public class ButterfliesAreaInspector : Editor {
    public override void OnInspectorGUI()
    {
        ButterfliesArea area = (ButterfliesArea)target;
        EditorGUI.BeginChangeCheck();
        area.Count = EditorGUILayout.IntField("Count", area.Count);
        SerializedProperty prop = serializedObject.FindProperty("prefabs");
        SerializedProperty speed = serializedObject.FindProperty("speed");

        EditorGUILayout.PropertyField(prop, true);
        EditorGUILayout.PropertyField(speed, true);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(target, "Changed field");
        }
        EditorUtility.SetDirty(target);
        if (GUILayout.Button("Spawn"))
            area.SpawnButterflies();
        if (GUILayout.Button("Remove all"))
            area.RemoveButterflies();
        if (!Application.isPlaying)
        EditorSceneManager.MarkAllScenesDirty();
    }
}
