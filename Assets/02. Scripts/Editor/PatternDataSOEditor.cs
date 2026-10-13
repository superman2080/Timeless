using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PatternDataSO))]
public class PatternDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var pattern = (PatternDataSO)target;

        if (GUILayout.Button("Pattern Editor 열기", GUILayout.Height(28)))
            PatternEditorWindow.Open(pattern);

        EditorGUILayout.LabelField("View Mode", pattern.viewMode.ToString());
        EditorGUILayout.Space();
        PatternEditorWindow.DrawGrid(pattern, false);
    }
}
