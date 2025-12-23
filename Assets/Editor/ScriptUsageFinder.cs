using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ScriptUsageFinder : EditorWindow
{
    private MonoScript _targetScript;
    private Vector2 _scrollPosition;
    private List<GameObject> _foundObjects = new List<GameObject>();
    private bool _includeInactive = true;
    private bool _searchPrefabs = false;

    [MenuItem("Window/Script Usage Finder")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptUsageFinder>("Script Usage Finder");
        window.minSize = new Vector2(300, 200);
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Usage Finder", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox("스크립트를 드래그 앤 드롭하면 현재 씬에서 사용 중인 오브젝트를 찾습니다.", MessageType.Info);
        GUILayout.Space(10);

        // 드래그 앤 드롭 영역
        var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, _targetScript == null ? "여기에 스크립트 드래그 앤 드롭" : _targetScript.name, EditorStyles.helpBox);

        HandleDragAndDrop(dropArea);

        GUILayout.Space(5);

        // 옵션
        _includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", _includeInactive);
        _searchPrefabs = EditorGUILayout.Toggle("프로젝트 프리팹도 검색", _searchPrefabs);

        GUILayout.Space(5);

        // 스크립트 필드 (수동 선택용)
        var newScript = (MonoScript)EditorGUILayout.ObjectField("Target Script", _targetScript, typeof(MonoScript), false);
        if (newScript != _targetScript)
        {
            _targetScript = newScript;
            if (_targetScript != null)
                FindUsages();
        }

        GUILayout.Space(5);

        // 검색 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("현재 씬에서 찾기", GUILayout.Height(30)))
        {
            FindUsages();
        }
        if (GUILayout.Button("결과 초기화", GUILayout.Height(30)))
        {
            _foundObjects.Clear();
            _targetScript = null;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 결과 표시
        if (_targetScript != null)
        {
            var scriptClass = _targetScript.GetClass();
            string className = scriptClass != null ? scriptClass.Name : _targetScript.name;

            EditorGUILayout.LabelField($"검색 대상: {className}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"발견된 오브젝트: {_foundObjects.Count}개");

            GUILayout.Space(5);

            if (_foundObjects.Count > 0)
            {
                // 전체 선택 버튼
                if (GUILayout.Button($"전체 {_foundObjects.Count}개 선택"))
                {
                    Selection.objects = _foundObjects.Where(o => o != null).ToArray();
                }

                GUILayout.Space(5);

                // 결과 리스트
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                for (int i = 0; i < _foundObjects.Count; i++)
                {
                    var obj = _foundObjects[i];
                    if (obj == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // 오브젝트 활성화 상태 표시
                    string prefix = obj.activeInHierarchy ? "●" : "○";

                    // 오브젝트 경로
                    string path = GetGameObjectPath(obj);

                    if (GUILayout.Button($"{prefix} {path}", EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }

                    // Ping 버튼
                    if (GUILayout.Button("→", GUILayout.Width(25)))
                    {
                        EditorGUIUtility.PingObject(obj);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            else if (_targetScript != null)
            {
                EditorGUILayout.HelpBox("현재 씬에서 이 스크립트를 사용하는 오브젝트가 없습니다.", MessageType.Warning);
            }
        }

        // 프리팹 검색 결과
        if (_searchPrefabs && _targetScript != null)
        {
            GUILayout.Space(10);
            if (GUILayout.Button("프로젝트 프리팹에서 찾기"))
            {
                FindInPrefabs();
            }
        }
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is MonoScript script)
                        {
                            _targetScript = script;
                            FindUsages();
                            break;
                        }
                    }
                }
                evt.Use();
                break;
        }
    }

    private void FindUsages()
    {
        _foundObjects.Clear();

        if (_targetScript == null) return;

        var scriptClass = _targetScript.GetClass();
        if (scriptClass == null)
        {
            Debug.LogWarning($"[ScriptUsageFinder] {_targetScript.name}의 클래스를 찾을 수 없습니다.");
            return;
        }

        // 현재 씬의 모든 오브젝트 검색
        var allObjects = _includeInactive
            ? Resources.FindObjectsOfTypeAll<GameObject>()
            : Object.FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            // 씬 오브젝트만 (프리팹 제외)
            if (!IsSceneObject(obj)) continue;

            if (obj.GetComponent(scriptClass) != null)
            {
                _foundObjects.Add(obj);
            }
        }

        // 이름순 정렬
        _foundObjects = _foundObjects.OrderBy(o => GetGameObjectPath(o)).ToList();

        Debug.Log($"[ScriptUsageFinder] {scriptClass.Name}: {_foundObjects.Count}개 발견");
    }

    private void FindInPrefabs()
    {
        if (_targetScript == null) return;

        var scriptClass = _targetScript.GetClass();
        if (scriptClass == null) return;

        var guids = AssetDatabase.FindAssets("t:Prefab");
        var prefabsWithScript = new List<string>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.GetComponentInChildren(scriptClass, true) != null)
            {
                prefabsWithScript.Add(path);
            }
        }

        if (prefabsWithScript.Count > 0)
        {
            Debug.Log($"[ScriptUsageFinder] 프리팹에서 {scriptClass.Name} 발견: {prefabsWithScript.Count}개");
            foreach (var path in prefabsWithScript)
            {
                Debug.Log($"  - {path}");
            }
        }
        else
        {
            Debug.Log($"[ScriptUsageFinder] 프리팹에서 {scriptClass.Name}을 사용하는 프리팹이 없습니다.");
        }
    }

    private bool IsSceneObject(GameObject obj)
    {
        // 씬에 있는 오브젝트인지 확인
        return obj.scene.IsValid() && !EditorUtility.IsPersistent(obj);
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
