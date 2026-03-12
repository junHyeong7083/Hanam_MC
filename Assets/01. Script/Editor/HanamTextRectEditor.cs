using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hanam이 붙은 오브젝트 > BGImage > 첫 번째 자식 Text의 RectTransform 일괄 조정 툴
/// Tools > Hanam Text Rect Editor
/// </summary>
public class HanamTextRectEditor : EditorWindow
{
    [MenuItem("Tools/Hanam Text Rect Editor")]
    static void Open() => GetWindow<HanamTextRectEditor>("Hanam Text Rect");

    private readonly List<RectTransform> _targets = new List<RectTransform>();

    // 적용할 RectTransform 값
    private Vector2 _anchorMin  = new Vector2(0f, 0f);
    private Vector2 _anchorMax  = new Vector2(1f, 1f);
    private Vector2 _pivot      = new Vector2(0.5f, 0.5f);
    private Vector2 _offsetMin  = Vector2.zero;  // left, bottom (anchorMin 기준)
    private Vector2 _offsetMax  = Vector2.zero;  // right, top   (anchorMax 기준, 음수가 안쪽)

    private Vector2 _scrollPos;

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Hanam > BGImage > 첫째 자식 Text RectTransform", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("씬에서 이름에 'Hanam'이 포함된 오브젝트를 찾고,\n그 자식 'BGImage'의 첫 번째 자식 Text RectTransform을 일괄 편집합니다.", MessageType.Info);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Scene", GUILayout.Height(28)))
                ScanScene();

            GUI.enabled = _targets.Count > 0;
            if (GUILayout.Button("Load Values from First", GUILayout.Height(28)))
                LoadFromFirst();
            GUI.enabled = true;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"찾은 Text: {_targets.Count}개");

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(110));
        foreach (var rt in _targets)
        {
            if (rt == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(rt, typeof(RectTransform), true);
                var cur = rt.anchorMin;
                EditorGUILayout.LabelField($"off({rt.offsetMin.x:F0},{rt.offsetMin.y:F0}) ({rt.offsetMax.x:F0},{rt.offsetMax.y:F0})", GUILayout.Width(180));
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("적용할 RectTransform 값", EditorStyles.boldLabel);

        _anchorMin = EditorGUILayout.Vector2Field("Anchor Min", _anchorMin);
        _anchorMax = EditorGUILayout.Vector2Field("Anchor Max", _anchorMax);
        _pivot     = EditorGUILayout.Vector2Field("Pivot",      _pivot);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Offset (stretch 방식)", EditorStyles.miniBoldLabel);
        _offsetMin = EditorGUILayout.Vector2Field("Offset Min (Left, Bottom)", _offsetMin);
        _offsetMax = EditorGUILayout.Vector2Field("Offset Max (Right, Top)",   _offsetMax);

        EditorGUILayout.Space(8);
        GUI.enabled = _targets.Count > 0;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("Apply to All", GUILayout.Height(32)))
            ApplyAll();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void ScanScene()
    {
        _targets.Clear();

        // 비활성 포함 전체 오브젝트 검색
        var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGos)
        {
            // 에셋(프리팹)은 제외, 씬 오브젝트만
            if (!go.scene.IsValid()) continue;

            if (!go.name.Contains("Hanam")) continue;

            // BGImage 직속 자식 찾기
            Transform bgImage = null;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (go.transform.GetChild(i).name == "BGImage")
                {
                    bgImage = go.transform.GetChild(i);
                    break;
                }
            }

            if (bgImage == null || bgImage.childCount == 0) continue;

            // 첫 번째 자식만 사용
            var firstChild = bgImage.GetChild(0);
            var rt = firstChild.GetComponent<RectTransform>();
            if (rt != null)
                _targets.Add(rt);
        }

        Repaint();
        Debug.Log($"[HanamTextRectEditor] {_targets.Count}개 대상 Text 발견");
    }

    private void LoadFromFirst()
    {
        if (_targets.Count == 0) return;
        var rt = _targets[0];
        if (rt == null) return;

        _anchorMin  = rt.anchorMin;
        _anchorMax  = rt.anchorMax;
        _pivot      = rt.pivot;
        _offsetMin  = rt.offsetMin;
        _offsetMax  = rt.offsetMax;

        Repaint();
    }

    private void ApplyAll()
    {
        int count = 0;
        foreach (var rt in _targets)
        {
            if (rt == null) continue;

            Undo.RecordObject(rt, "Hanam Text Rect Apply");
            rt.anchorMin  = _anchorMin;
            rt.anchorMax  = _anchorMax;
            rt.pivot      = _pivot;
            rt.offsetMin  = _offsetMin;
            rt.offsetMax  = _offsetMax;

            EditorUtility.SetDirty(rt);
            count++;
        }

        Debug.Log($"[HanamTextRectEditor] {count}개 RectTransform 적용 완료");
    }
}
