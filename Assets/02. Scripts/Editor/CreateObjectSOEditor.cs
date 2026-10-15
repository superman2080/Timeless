using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CreateObjectSO))]
public class CreateObjectSOEditor : Editor
{
    ReorderableList list;

    void OnEnable()
    {
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("patterns"), true, true, true, true);
        list.drawHeaderCallback = DrawHeader;
        list.drawElementCallback = DrawElement;
    }

    float TotalPercent()
    {
        float sum = 0f;
        foreach (var e in ((CreateObjectSO)target).patterns)
            sum += e.percent;
        return sum;
    }

    static bool IsHundred(float sum) => Mathf.Abs(sum - 100f) < 0.01f;

    void DrawHeader(Rect rect)
    {
        float sum = TotalPercent();
        EditorGUI.LabelField(rect, "Patterns");
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };
        style.normal.textColor = IsHundred(sum) ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.35f, 0.35f);
        EditorGUI.LabelField(rect, $"합계 {sum:0.##}%", style);
    }

    void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = list.serializedProperty.GetArrayElementAtIndex(index);
        var patternProp = element.FindPropertyRelative("pattern");
        var percentProp = element.FindPropertyRelative("percent");

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        var prevColor = GUI.color;
        if (patternProp.objectReferenceValue == null)
            GUI.color = new Color(1f, 0.6f, 0.6f);
        else if (percentProp.floatValue <= 0f)
            GUI.color = new Color(1f, 1f, 1f, 0.5f);

        const float percentWidth = 60f, unitWidth = 16f, buttonWidth = 40f, gap = 4f;
        var patternRect = new Rect(rect.x, rect.y, rect.width - percentWidth - unitWidth - buttonWidth - gap * 3, rect.height);
        var percentRect = new Rect(patternRect.xMax + gap, rect.y, percentWidth, rect.height);
        var unitRect = new Rect(percentRect.xMax, rect.y, unitWidth, rect.height);
        var buttonRect = new Rect(unitRect.xMax + gap, rect.y, buttonWidth, rect.height);

        EditorGUI.PropertyField(patternRect, patternProp, GUIContent.none);
        percentProp.floatValue = Mathf.Clamp(EditorGUI.FloatField(percentRect, percentProp.floatValue), 0f, 100f);
        EditorGUI.LabelField(unitRect, "%");

        GUI.color = prevColor;
        using (new EditorGUI.DisabledScope(patternProp.objectReferenceValue == null))
        {
            if (GUI.Button(buttonRect, "편집", EditorStyles.miniButton))
                PatternEditorWindow.Open((PatternDataSO)patternProp.objectReferenceValue);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rowDistance"));
        EditorGUILayout.Space();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        var data = (CreateObjectSO)target;
        float sum = TotalPercent();
        if (!IsHundred(sum))
            EditorGUILayout.HelpBox($"확률 합계가 {sum:0.##}%입니다. 100%가 되도록 맞춰주세요.", MessageType.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(sum <= 0f || IsHundred(sum)))
            {
                if (GUILayout.Button("100%로 맞추기"))
                {
                    Undo.RecordObject(data, "Normalize Percent");
                    foreach (var e in data.patterns)
                        e.percent = e.percent * 100f / sum;
                    EditorUtility.SetDirty(data);
                }
            }

            if (GUILayout.Button("새 패턴 만들어 추가"))
                AddNewPattern(data);
        }
    }

    // 기존 패턴의 폴더/뷰모드를 따라 새 패턴 생성 후 0%로 추가
    static void AddNewPattern(CreateObjectSO data)
    {
        var reference = data.patterns.Find(e => e.pattern != null)?.pattern;
        string folder = reference != null
            ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(reference)).Replace('\\', '/')
            : "Assets/11. Datas/Patterns";
        var mode = reference != null ? reference.viewMode : ViewMode.View3D;

        var created = PatternEditorWindow.CreatePatternAsset(folder, mode);
        if (created == null)
            return;

        Undo.RecordObject(data, "Add Pattern");
        data.patterns.Add(new PatternEntry { pattern = created, percent = 0f });
        EditorUtility.SetDirty(data);
        PatternEditorWindow.Open(created);
        GUIUtility.ExitGUI();
    }
}
