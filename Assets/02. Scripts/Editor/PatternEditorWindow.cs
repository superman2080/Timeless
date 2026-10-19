using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class PatternEditorWindow : EditorWindow
{
    const float CellWidth = 90f;
    const float LowerHeight = 34f;
    const float UpperHeight = 24f;
    const float LabelWidth = 64f;

    [SerializeField] PatternDataSO pattern;
    Vector2 scroll;

    [MenuItem("Tools/Timeless/Pattern Editor")]
    static void OpenMenu() => Open(null);

    public static void Open(PatternDataSO target)
    {
        var window = GetWindow<PatternEditorWindow>("Pattern Editor");
        if (target != null)
            window.pattern = target;
        window.Show();
    }

    // PatternDataSO 에셋 더블클릭 시 이 창으로 열기
    [OnOpenAsset]
    static bool OnOpenAsset(EntityId entityId, int line)
    {
        if (EditorUtility.EntityIdToObject(entityId) is PatternDataSO target)
        {
            Open(target);
            return true;
        }
        return false;
    }

    void OnEnable()
    {
        wantsMouseMove = true;
        Undo.undoRedoPerformed += Repaint;
    }

    void OnDisable() => Undo.undoRedoPerformed -= Repaint;

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            pattern = (PatternDataSO)EditorGUILayout.ObjectField("Pattern", pattern, typeof(PatternDataSO), false);
            if (GUILayout.Button("새 패턴", GUILayout.Width(70)))
            {
                var created = CreatePatternAsset("Assets/11. Datas/Patterns", ViewMode.View3D);
                if (created != null)
                    pattern = created;
            }
        }

        if (pattern == null)
        {
            EditorGUILayout.HelpBox("편집할 PatternData를 선택하거나 새로 만드세요.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        var mode = (ViewMode)EditorGUILayout.EnumPopup("View Mode", pattern.viewMode);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pattern, "Change View Mode");
            pattern.viewMode = mode;
            if (mode == ViewMode.View2D)
                Clear2DHiddenLanes(pattern);
            EditorUtility.SetDirty(pattern);
        }

        EditorGUILayout.HelpBox("칸 클릭: 오브젝트 선택 / 우클릭: 비우기. 아래 행이 먼저 도착합니다.", MessageType.None);

        if (GUILayout.Button("+ 행 추가 (위로)", GUILayout.Height(26)))
        {
            Undo.RecordObject(pattern, "Add Row");
            pattern.rows.Add(new PatternRow());
            EditorUtility.SetDirty(pattern);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawGrid(pattern, true);
        EditorGUILayout.EndScrollView();

        if (Event.current.type == EventType.MouseMove)
            Repaint();
    }

    // 2D에선 플레이어가 마지막 레인에 고정되므로 나머지 레인은 비움
    static void Clear2DHiddenLanes(PatternDataSO target)
    {
        foreach (var row in target.rows)
        {
            for (int i = 0; i < PatternDataSO.LaneCount - 1; i++)
            {
                row.lower[i] = ObjectType.NONE;
                row.upper[i] = ObjectType.NONE;
            }
        }
    }

    public static PatternDataSO CreatePatternAsset(string defaultFolder, ViewMode viewMode)
    {
        string path = EditorUtility.SaveFilePanelInProject("새 패턴", "Pattern", "asset", "패턴 저장 위치", defaultFolder);
        if (string.IsNullOrEmpty(path))
            return null;

        var created = CreateInstance<PatternDataSO>();
        created.viewMode = viewMode;
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        return created;
    }

    // 행을 위에서 아래로(마지막 행 → rows[0]) 그림. editable이 false면 미리보기
    public static void DrawGrid(PatternDataSO target, bool editable)
    {
        int[] visibleLanes = target.viewMode == ViewMode.View2D
            ? new[] { PatternDataSO.LaneCount - 1 }
            : new[] { 0, 1, 2 };

        // 헤더
        var header = GUILayoutUtility.GetRect(LabelWidth + CellWidth * visibleLanes.Length, 18f, GUILayout.ExpandWidth(false));
        for (int c = 0; c < visibleLanes.Length; c++)
        {
            var r = new Rect(header.x + LabelWidth + CellWidth * c, header.y, CellWidth, header.height);
            EditorGUI.LabelField(r, "Lane " + visibleLanes[c], EditorStyles.centeredGreyMiniLabel);
        }

        for (int rowIndex = target.rows.Count - 1; rowIndex >= 0; rowIndex--)
        {
            var row = target.rows[rowIndex];
            EnsureLaneArrays(row);

            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = GUILayoutUtility.GetRect(LabelWidth + CellWidth * visibleLanes.Length, UpperHeight + LowerHeight + 2f, GUILayout.ExpandWidth(false));
                var upperRect = new Rect(rect.x, rect.y, rect.width, UpperHeight);
                var lowerRect = new Rect(rect.x, rect.y + UpperHeight + 1f, rect.width, LowerHeight);

                GUI.Label(new Rect(rect.x, rect.y, LabelWidth, rect.height), "Row " + rowIndex, EditorStyles.boldLabel);
                GUI.Label(new Rect(rect.x + LabelWidth - 18f, upperRect.y + 4f, 18f, 16f), "상", EditorStyles.miniLabel);
                GUI.Label(new Rect(rect.x + LabelWidth - 18f, lowerRect.y + 9f, 18f, 16f), "하", EditorStyles.miniLabel);

                for (int c = 0; c < visibleLanes.Length; c++)
                {
                    int lane = visibleLanes[c];
                    float x = rect.x + LabelWidth + CellWidth * c;
                    DrawCell(target, row.upper, lane, new Rect(x + 1f, upperRect.y, CellWidth - 2f, UpperHeight), true, editable);
                    DrawCell(target, row.lower, lane, new Rect(x + 1f, lowerRect.y, CellWidth - 2f, LowerHeight), false, editable);
                }

                if (editable)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(60)))
                    {
                        int captured = rowIndex;
                        if (GUILayout.Button(new GUIContent("복제", "바로 위에 복제"), EditorStyles.miniButton))
                        {
                            Undo.RecordObject(target, "Duplicate Row");
                            var src = target.rows[captured];
                            target.rows.Insert(captured + 1, new PatternRow { lower = (ObjectType[])src.lower.Clone(), upper = (ObjectType[])src.upper.Clone() });
                            EditorUtility.SetDirty(target);
                            GUIUtility.ExitGUI();
                        }
                        if (GUILayout.Button("삭제", EditorStyles.miniButton))
                        {
                            Undo.RecordObject(target, "Delete Row");
                            target.rows.RemoveAt(captured);
                            EditorUtility.SetDirty(target);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            GUILayout.Space(4f);
        }
    }

    static void EnsureLaneArrays(PatternRow row)
    {
        if (row.lower == null || row.lower.Length != PatternDataSO.LaneCount)
            Array.Resize(ref row.lower, PatternDataSO.LaneCount);
        if (row.upper == null || row.upper.Length != PatternDataSO.LaneCount)
            Array.Resize(ref row.upper, PatternDataSO.LaneCount);
    }

    static void DrawCell(PatternDataSO target, ObjectType[] cells, int lane, Rect rect, bool upper, bool editable)
    {
        var type = cells[lane];
        var e = Event.current;
        bool hover = editable && rect.Contains(e.mousePosition);

        Color bg = type == ObjectType.NONE ? new Color(0.3f, 0.3f, 0.3f) : GetColor(type);
        if (upper)
            bg = Color.Lerp(bg, Color.white, 0.15f);
        if (hover)
            bg = Color.Lerp(bg, Color.white, 0.2f);
        EditorGUI.DrawRect(rect, bg);

        if (type != ObjectType.NONE)
        {
            var style = new GUIStyle(EditorStyles.whiteBoldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = upper ? 10 : 11 };
            GUI.Label(rect, type.ToString(), style);
        }

        if (!editable || e.type != EventType.MouseDown || !rect.Contains(e.mousePosition))
            return;

        if (e.button == 1)
        {
            SetCell(target, cells, lane, ObjectType.NONE);
        }
        else if (e.button == 0)
        {
            var menu = new GenericMenu();
            if (type != ObjectType.NONE)
            {
                menu.AddItem(new GUIContent("비우기"), false, () => SetCell(target, cells, lane, ObjectType.NONE));
                menu.AddSeparator("");
            }
            foreach (ObjectType t in Enum.GetValues(typeof(ObjectType)))
            {
                if (t == ObjectType.NONE)
                    continue;
                var captured = t;
                menu.AddItem(new GUIContent(t.ToString()), t == type, () => SetCell(target, cells, lane, captured));
            }
            menu.DropDown(rect);
        }
        e.Use();
    }

    static void SetCell(PatternDataSO target, ObjectType[] cells, int lane, ObjectType type)
    {
        Undo.RecordObject(target, "Set Pattern Cell");
        cells[lane] = type;
        EditorUtility.SetDirty(target);
    }

    static Color GetColor(ObjectType type) => type switch
    {
        ObjectType.LASER => new Color(0.85f, 0.2f, 0.25f),
        ObjectType.BARREL => new Color(0.85f, 0.5f, 0.15f),
        ObjectType.BLOCKADE => new Color(0.5f, 0.38f, 0.3f),
        ObjectType.FAN => new Color(0.55f, 0.3f, 0.75f),
        ObjectType.ROBOT_ARM => new Color(0.6f, 0.15f, 0.15f),
        ObjectType.HP => new Color(0.2f, 0.65f, 0.3f),
        ObjectType.KEY_CARD => new Color(0.8f, 0.7f, 0.1f),
        ObjectType.LAKITU => new Color(0.75f, 0.75f, 0.2f),
        ObjectType.SPIKE_BALL => new Color(0.9f, 0.3f, 0.5f),
        ObjectType.WHOMP => new Color(0.45f, 0.45f, 0.5f),
        ObjectType.STONE_FRAGMENT => new Color(0.35f, 0.33f, 0.3f),
        ObjectType.BOO => new Color(0.7f, 0.72f, 0.85f),
        ObjectType.OCTOOMBA => new Color(0.5f, 0.2f, 0.6f),
        ObjectType.SUPER_MUSHROOM => new Color(0.9f, 0.25f, 0.1f),
        ObjectType.MINI_MUSHROOM => new Color(0.2f, 0.45f, 0.95f),
        _ => new Color(0.35f, 0.45f, 0.6f),
    };
}
