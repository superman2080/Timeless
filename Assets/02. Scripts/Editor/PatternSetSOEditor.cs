using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(PatternSetSO))]
public class PatternSetSOEditor : Editor
{
    ReorderableList list3D, list2D;

    static readonly Color[] TierColors =
    {
        new Color(0.35f, 0.6f, 0.9f), new Color(0.9f, 0.55f, 0.3f), new Color(0.45f, 0.8f, 0.45f),
        new Color(0.8f, 0.45f, 0.8f), new Color(0.9f, 0.8f, 0.35f), new Color(0.4f, 0.8f, 0.8f),
    };

    PatternSetSO Data => (PatternSetSO)target;

    void OnEnable()
    {
        list3D = CreateList("tiers3D", ViewMode.View3D);
        list2D = CreateList("tiers2D", ViewMode.View2D);
    }

    ReorderableList CreateList(string propName, ViewMode mode)
    {
        var list = new ReorderableList(serializedObject, serializedObject.FindProperty(propName), true, true, true, true);
        float line = EditorGUIUtility.singleLineHeight;
        list.elementHeight = line * 2f + 6f;
        list.drawHeaderCallback = rect =>
        {
            float from = Data.ToGlobalRatio(mode, 0f), to = Data.ToGlobalRatio(mode, 1f);
            EditorGUI.LabelField(rect, $"{(mode == ViewMode.View2D ? "2D" : "3D")} 구간 (전체 HP {from:0.00} ~ {to:0.00})");
        };
        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var maxProp = element.FindPropertyRelative("maxRatio");
            var dataProp = element.FindPropertyRelative("dataSet");

            rect.y += 2f;
            var top = new Rect(rect.x, rect.y, rect.width, line);
            const float colorWidth = 6f, gap = 4f;
            float sliderWidth = (top.width - colorWidth - gap * 2) * 0.55f;
            EditorGUI.DrawRect(new Rect(top.x, top.y, colorWidth, line * 2f + 2f), TierColors[index % TierColors.Length]);

            float prevLabel = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 32f;
            EditorGUI.Slider(new Rect(top.x + colorWidth + gap, top.y, sliderWidth, line), maxProp, 0f, 1f, "상한");
            EditorGUIUtility.labelWidth = prevLabel;

            var prevColor = GUI.color;
            if (dataProp.objectReferenceValue == null)
                GUI.color = new Color(1f, 0.6f, 0.6f);
            EditorGUI.PropertyField(new Rect(top.x + colorWidth + gap * 2 + sliderWidth, top.y, top.width - sliderWidth - colorWidth - gap * 2, line), dataProp, GUIContent.none);
            GUI.color = prevColor;

            // 구간 계산은 런타임과 같은 규칙을 쓰도록 SO 객체 기준 (편집 값은 다음 repaint에 반영)
            var tiers = Data.GetTiers(mode);
            if (index >= tiers.Count)
                return;
            var tier = tiers[index];
            var bottom = new Rect(top.x + colorWidth + gap, top.y + line + 2f, top.width - colorWidth - gap, line);
            string text;
            if (tier.dataSet == null)
                text = "데이터셋이 비어 있어 사용되지 않음";
            else if (IsShadowed(tiers, tier))
                text = "상한이 같은 앞 항목 때문에 사용되지 않음";
            else
            {
                float lower = PatternSetSO.GetLowerBound(tiers, tier);
                text = $"로컬 {lower:0.00} ~ {tier.maxRatio:0.00}  →  전체 HP {Data.ToGlobalRatio(mode, lower):0.00} ~ {Data.ToGlobalRatio(mode, tier.maxRatio):0.00}";
            }
            EditorGUI.LabelField(bottom, text, EditorStyles.miniLabel);
        };
        return list;
    }

    // 상한이 같은 유효 항목이 앞에 있으면 Select에서 선택되지 않음
    static bool IsShadowed(List<PatternSetTier> tiers, PatternSetTier tier)
    {
        foreach (var t in tiers)
        {
            if (t == tier)
                return false;
            if (t.dataSet != null && Mathf.Approximately(t.maxRatio, tier.maxRatio))
                return true;
        }
        return false;
    }

    void DrawBar(ViewMode mode)
    {
        var tiers = Data.GetTiers(mode);
        var rect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
        rect.xMin += 34f;
        rect.xMax -= 34f;
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        var centered = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
        centered.normal.textColor = Color.black;
        for (int i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.dataSet == null || IsShadowed(tiers, tier))
                continue;
            float lower = PatternSetSO.GetLowerBound(tiers, tier);
            var seg = new Rect(rect.x + rect.width * lower, rect.y, rect.width * (tier.maxRatio - lower), rect.height);
            EditorGUI.DrawRect(seg, TierColors[i % TierColors.Length]);
            if (seg.width > 40f)
                EditorGUI.LabelField(seg, tier.dataSet.name, centered);
        }

        var left = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        EditorGUI.LabelField(new Rect(rect.x - 34f, rect.y, 30f, rect.height), $"{Data.ToGlobalRatio(mode, 0f):0.00}", left);
        EditorGUI.LabelField(new Rect(rect.xMax + 4f, rect.y, 30f, rect.height), $"{Data.ToGlobalRatio(mode, 1f):0.00}", EditorStyles.miniLabel);
    }

    void DrawWarnings(ViewMode mode)
    {
        var tiers = Data.GetTiers(mode);
        bool hasTop = false, hasEmpty = false, hasShadowed = false;
        foreach (var t in tiers)
        {
            if (t.dataSet == null) { hasEmpty = true; continue; }
            if (t.maxRatio >= 1f) hasTop = true;
            if (IsShadowed(tiers, t)) hasShadowed = true;
        }
        if (!hasTop)
            EditorGUILayout.HelpBox("상한 1.0인 항목이 없어 위쪽 HP 구간에서 패턴이 나오지 않습니다.", MessageType.Warning);
        if (hasShadowed)
            EditorGUILayout.HelpBox("상한이 같은 항목이 있습니다. 앞의 항목만 사용됩니다.", MessageType.Warning);
        if (hasEmpty)
            EditorGUILayout.HelpBox("데이터셋이 비어 있는 항목이 있습니다.", MessageType.Warning);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold3DView"), new GUIContent("2D → 3D 전환 (HP ≥)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold2DView"), new GUIContent("3D → 2D 전환 (HP ≤)"));
        serializedObject.ApplyModifiedProperties();

        foreach (var (list, mode) in new[] { (list3D, ViewMode.View3D), (list2D, ViewMode.View2D) })
        {
            EditorGUILayout.Space();
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            DrawBar(mode);
            DrawWarnings(mode);
        }
    }
}
