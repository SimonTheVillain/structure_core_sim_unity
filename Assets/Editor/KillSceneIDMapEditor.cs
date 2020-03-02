using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



#if UNITY_EDITOR

[CustomEditor(typeof(KillSceneIDMap))]
public class KillSceneIDMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Find and Kill SceneIDMap"))
        {
            Selection.activeGameObject = GameObject.Find("SceneIDMap");
            DestroyImmediate(Selection.activeGameObject);
        }
    }
}

#endif