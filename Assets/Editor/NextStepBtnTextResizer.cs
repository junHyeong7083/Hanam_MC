using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NextStepBtn 자식 Text 폰트 사이즈 일괄 조정 에디터 윈도우
/// Menu: Tools > NextStepBtn Text Resizer
/// </summary>
public class NextStepBtnTextResizer : EditorWindow
{
    private string _searchName = "NextStepBtn";
    private int _newFontSize = 28;
    private bool _includeInactive = true;

    private List<Text> _found = new List<Text>();
    private Vector2 _scroll;

    [MenuItem("Tools/NextStepBtn Text Resizer")]
    public static void Open()
    {
        GetWindow<NextStepBtnTextResizer>("NextStepBtn Text Resizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("NextStepBtn 자식 Text 폰트 크기 일괄 조정", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _searchName = EditorGUILayout.TextField("검색할 버튼 이름", _searchName);
        _includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", _includeInactive);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("씬에서 검색"))
            Search();

        EditorGUILayout.Space(6);

        if (_found.Count > 0)
        {
            EditorGUILayout.LabelField($"발견된 Text: {_found.Count}개", EditorStyles.helpBox);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(200));
            foreach (var t in _found)
            {
                if (t == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(t, typeof(Text), true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField($"현재: {t.fontSize}", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            _newFontSize = EditorGUILayout.IntField("변경할 폰트 크기", _newFontSize);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
            if (GUILayout.Button($"일괄 적용 ({_found.Count}개)", GUILayout.Height(36)))
                Apply();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            EditorGUILayout.HelpBox("검색 버튼을 눌러주세요.", MessageType.Info);
        }
    }

    private void Search()
    {
        _found.Clear();

        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(_includeInactive);
            foreach (var tr in transforms)
            {
                if (tr.name != _searchName) continue;

                // 자식 Text만 (자기 자신 제외)
                var texts = tr.GetComponentsInChildren<Text>(_includeInactive);
                foreach (var t in texts)
                {
                    if (t.transform != tr)
                        _found.Add(t);
                }
            }
        }

        if (_found.Count == 0)
            Debug.Log($"[NextStepBtnTextResizer] '{_searchName}' 이름의 버튼을 찾지 못했습니다.");
        else
            Debug.Log($"[NextStepBtnTextResizer] Text {_found.Count}개 발견.");
    }

    private void Apply()
    {
        int count = 0;
        foreach (var t in _found)
        {
            if (t == null) continue;
            Undo.RecordObject(t, "Resize NextStepBtn Text");
            t.fontSize = _newFontSize;
            EditorUtility.SetDirty(t);
            count++;
        }

        Debug.Log($"[NextStepBtnTextResizer] {count}개 Text에 fontSize={_newFontSize} 적용 완료.");
        Repaint();
    }
}
