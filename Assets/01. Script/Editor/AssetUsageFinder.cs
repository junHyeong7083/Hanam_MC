using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AssetUsageFinder - 스크립트/머티리얼/쉐이더/텍스쳐 에셋이 프로젝트 어디서 사용되는지 검색하는 에디터 윈도우
///
/// 【사용법】 Unity 메뉴 → Tools → Asset Usage Finder
///           1) 검색 모드 선택 (스크립트 / 머티리얼 / 쉐이더 / 텍스쳐)
///           2) 에셋을 드래그하거나 이름을 입력
///           3) "검색" 버튼 클릭
/// 【검색 방식】
///   - 스크립트: 씬 내 오브젝트 (하이라키 하이라이트) + GUID 검색 + 코드 참조
///   - 머티리얼: 씬 내 Renderer/Graphic에서 사용 중인 오브젝트 + GUID 검색
///   - 쉐이더:  머티리얼에서 사용 중인지 검색 + GUID 검색
///   - 텍스쳐:  머티리얼에서 텍스쳐 참조 검색 + GUID 검색 + 씬 내 UI RawImage 검색
/// </summary>
public class AssetUsageFinder : EditorWindow
{
    private enum SearchMode { Script, Material, Shader, Texture }

    private SearchMode searchMode = SearchMode.Script;

    // 스크립트 검색용
    private MonoScript targetScript;
    private string scriptName = "";

    // 머티리얼 검색용
    private Material targetMaterial;
    private string materialName = "";

    // 쉐이더 검색용
    private Shader targetShader;
    private string shaderName = "";

    // 텍스쳐 검색용
    private Texture targetTexture;
    private string textureName = "";

    private Vector2 scrollPos;
    private List<ResultEntry> results = new List<ResultEntry>();
    private bool searched = false;
    private bool searching = false;

    // 씬 내 발견된 GameObject 목록 (하이라키 하이라이트용)
    private List<GameObject> foundSceneObjects = new List<GameObject>();

    private struct ResultEntry
    {
        public string category;
        public string path;
        public UnityEngine.Object asset;
        public bool isSceneObject;
    }

    [MenuItem("Tools/Asset Usage Finder")]
    static void Open()
    {
        var w = GetWindow<AssetUsageFinder>("Asset Usage Finder");
        w.minSize = new Vector2(500, 350);
        w.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("에셋 사용처 검색", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // ── 검색 모드 탭 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(searchMode == SearchMode.Script, "스크립트", EditorStyles.toolbarButton))
            searchMode = SearchMode.Script;
        if (GUILayout.Toggle(searchMode == SearchMode.Material, "머티리얼", EditorStyles.toolbarButton))
            searchMode = SearchMode.Material;
        if (GUILayout.Toggle(searchMode == SearchMode.Shader, "쉐이더", EditorStyles.toolbarButton))
            searchMode = SearchMode.Shader;
        if (GUILayout.Toggle(searchMode == SearchMode.Texture, "텍스쳐", EditorStyles.toolbarButton))
            searchMode = SearchMode.Texture;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        bool canSearch = false;

        switch (searchMode)
        {
            case SearchMode.Script:
                DrawScriptFields();
                canSearch = !string.IsNullOrWhiteSpace(scriptName);
                break;
            case SearchMode.Material:
                DrawMaterialFields();
                canSearch = targetMaterial != null || !string.IsNullOrWhiteSpace(materialName);
                break;
            case SearchMode.Shader:
                DrawShaderFields();
                canSearch = targetShader != null || !string.IsNullOrWhiteSpace(shaderName);
                break;
            case SearchMode.Texture:
                DrawTextureFields();
                canSearch = targetTexture != null || !string.IsNullOrWhiteSpace(textureName);
                break;
        }

        GUILayout.Space(5);

        EditorGUI.BeginDisabledGroup(searching || !canSearch);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("검색", GUILayout.Height(30)))
        {
            Search();
        }
        if (foundSceneObjects.Count > 0)
        {
            if (GUILayout.Button($"씬 오브젝트 모두 선택 ({foundSceneObjects.Count})", GUILayout.Height(30), GUILayout.Width(200)))
            {
                Selection.objects = foundSceneObjects.ToArray();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        if (!searched) return;

        // 결과 표시
        string searchTarget = searchMode switch
        {
            SearchMode.Script => scriptName,
            SearchMode.Material => materialName,
            SearchMode.Shader => shaderName,
            SearchMode.Texture => textureName,
            _ => ""
        };

        if (results.Count == 0)
        {
            EditorGUILayout.HelpBox($"'{searchTarget}' 사용처를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"총 {results.Count}개 발견", EditorStyles.boldLabel);
        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        string lastCategory = "";
        foreach (var r in results)
        {
            if (r.category != lastCategory)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField($"── {r.category} ──", EditorStyles.boldLabel);
                lastCategory = r.category;
            }

            EditorGUILayout.BeginHorizontal();

            if (r.isSceneObject && r.asset != null)
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.95f, 0.5f);
                if (GUILayout.Button(r.path, GUILayout.Height(20)))
                {
                    EditorGUIUtility.PingObject(r.asset);
                    Selection.activeGameObject = (r.asset as GameObject) ?? (r.asset as Component)?.gameObject;
                    EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
                }
                GUI.backgroundColor = prevColor;
            }
            else if (r.asset != null)
            {
                if (GUILayout.Button(r.path, EditorStyles.linkLabel))
                {
                    EditorGUIUtility.PingObject(r.asset);
                    Selection.activeObject = r.asset;
                }
            }
            else
            {
                EditorGUILayout.LabelField(r.path);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════════
    //  UI 필드
    // ══════════════════════════════════════════════════════

    void DrawScriptFields()
    {
        EditorGUI.BeginChangeCheck();
        targetScript = (MonoScript)EditorGUILayout.ObjectField(
            "스크립트 파일", targetScript, typeof(MonoScript), false);
        if (EditorGUI.EndChangeCheck() && targetScript != null)
            scriptName = targetScript.GetClass()?.Name ?? targetScript.name;

        scriptName = EditorGUILayout.TextField("또는 클래스명 입력", scriptName);
    }

    void DrawMaterialFields()
    {
        EditorGUI.BeginChangeCheck();
        targetMaterial = (Material)EditorGUILayout.ObjectField(
            "머티리얼 파일", targetMaterial, typeof(Material), false);
        if (EditorGUI.EndChangeCheck() && targetMaterial != null)
            materialName = targetMaterial.name;

        materialName = EditorGUILayout.TextField("또는 머티리얼명 입력", materialName);
    }

    void DrawShaderFields()
    {
        EditorGUI.BeginChangeCheck();
        targetShader = (Shader)EditorGUILayout.ObjectField(
            "쉐이더 파일", targetShader, typeof(Shader), false);
        if (EditorGUI.EndChangeCheck() && targetShader != null)
            shaderName = targetShader.name;

        shaderName = EditorGUILayout.TextField("또는 쉐이더명 입력", shaderName);
    }

    void DrawTextureFields()
    {
        EditorGUI.BeginChangeCheck();
        targetTexture = (Texture)EditorGUILayout.ObjectField(
            "텍스쳐 파일", targetTexture, typeof(Texture), false);
        if (EditorGUI.EndChangeCheck() && targetTexture != null)
            textureName = targetTexture.name;

        textureName = EditorGUILayout.TextField("또는 텍스쳐명 입력", textureName);
    }

    // ══════════════════════════════════════════════════════
    //  검색 로직
    // ══════════════════════════════════════════════════════

    void Search()
    {
        results.Clear();
        foundSceneObjects.Clear();
        searched = true;
        searching = true;

        try
        {
            switch (searchMode)
            {
                case SearchMode.Script:   SearchScript();   break;
                case SearchMode.Material: SearchMaterial();  break;
                case SearchMode.Shader:   SearchShader();    break;
                case SearchMode.Texture:  SearchTexture();   break;
            }
        }
        finally
        {
            searching = false;
            Repaint();
        }
    }

    // ── 스크립트 검색 ────────────────────────────────────

    void SearchScript()
    {
        SearchScriptInCurrentScene();

        string guid = ResolveScriptGUID();
        if (!string.IsNullOrEmpty(guid))
            SearchGUIDInFiles(guid, null);

        SearchInCode();

        SortAndPing("씬 내 오브젝트", "씬", "프리팹", "에셋", "코드 참조");
    }

    void SearchScriptInCurrentScene()
    {
        Type scriptType = null;

        if (targetScript != null)
        {
            scriptType = targetScript.GetClass();
        }
        else
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && (ms.GetClass()?.Name == scriptName || ms.name == scriptName))
                {
                    targetScript = ms;
                    scriptType = ms.GetClass();
                    break;
                }
            }
        }

        if (scriptType == null) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var components = root.GetComponentsInChildren(scriptType, true);
                foreach (var comp in components)
                    AddSceneObjectResult(scene.name, comp.gameObject);
            }
        }
    }

    string ResolveScriptGUID()
    {
        if (targetScript != null)
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetScript));

        var guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms != null && (ms.GetClass()?.Name == scriptName || ms.name == scriptName))
            {
                targetScript = ms;
                return g;
            }
        }
        return null;
    }

    void SearchInCode()
    {
        string className = scriptName;
        if (string.IsNullOrEmpty(className)) return;

        var assetsPath = Application.dataPath;
        var csFiles = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

        string selfPath = "";
        if (targetScript != null)
            selfPath = Path.GetFullPath(AssetDatabase.GetAssetPath(targetScript)).Replace('\\', '/');

        foreach (var file in csFiles)
        {
            var fullPath = file.Replace('\\', '/');
            if (fullPath == selfPath) continue;

            var content = File.ReadAllText(file);
            if (ContainsClassName(content, className))
            {
                var relativePath = "Assets" + file.Replace(assetsPath, "").Replace('\\', '/');
                var asset = AssetDatabase.LoadMainAssetAtPath(relativePath);
                results.Add(new ResultEntry
                {
                    category = "코드 참조",
                    path = relativePath,
                    asset = asset
                });
            }
        }
    }

    // ── 머티리얼 검색 ────────────────────────────────────

    void SearchMaterial()
    {
        ResolveMaterial();
        SearchMaterialInCurrentScene();

        if (targetMaterial != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetMaterial));
            if (!string.IsNullOrEmpty(guid))
                SearchGUIDInFiles(guid, AssetDatabase.GetAssetPath(targetMaterial));
        }

        SortAndPing("씬 내 오브젝트", "씬", "프리팹", "에셋");
    }

    void ResolveMaterial()
    {
        if (targetMaterial != null) return;
        if (string.IsNullOrWhiteSpace(materialName)) return;

        var guids = AssetDatabase.FindAssets($"t:Material {materialName}");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.name == materialName)
            {
                targetMaterial = mat;
                return;
            }
        }
    }

    void SearchMaterialInCurrentScene()
    {
        if (targetMaterial == null) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (ArrayContains(renderer.sharedMaterials, targetMaterial))
                        AddSceneObjectResult(scene.name, renderer.gameObject);
                }

                var graphics = root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var graphic in graphics)
                {
                    if (graphic.material != null && graphic.material == targetMaterial)
                        AddSceneObjectResult(scene.name, graphic.gameObject);
                }
            }
        }
    }

    // ── 쉐이더 검색 ─────────────────────────────────────

    void SearchShader()
    {
        ResolveShader();

        // 1) 어떤 머티리얼이 이 쉐이더를 사용하는지
        SearchShaderInMaterials();

        // 2) 씬 내 Renderer에서 이 쉐이더를 사용하는 머티리얼을 가진 오브젝트
        SearchShaderInCurrentScene();

        // 3) GUID 기반 파일 검색
        if (targetShader != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetShader));
            if (!string.IsNullOrEmpty(guid))
                SearchGUIDInFiles(guid, AssetDatabase.GetAssetPath(targetShader));
        }

        SortAndPing("씬 내 오브젝트", "머티리얼", "씬", "프리팹", "에셋");
    }

    void ResolveShader()
    {
        if (targetShader != null) return;
        if (string.IsNullOrWhiteSpace(shaderName)) return;

        // 빌트인 쉐이더 이름으로 찾기
        targetShader = Shader.Find(shaderName);
        if (targetShader != null) return;

        // 에셋 검색
        var guids = AssetDatabase.FindAssets($"t:Shader {shaderName}");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null && (shader.name == shaderName || shader.name.EndsWith("/" + shaderName)))
            {
                targetShader = shader;
                return;
            }
        }
    }

    void SearchShaderInMaterials()
    {
        if (targetShader == null) return;

        var matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var g in matGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader == targetShader)
            {
                results.Add(new ResultEntry
                {
                    category = "머티리얼",
                    path = path,
                    asset = mat
                });
            }
        }
    }

    void SearchShaderInCurrentScene()
    {
        if (targetShader == null) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterials != null)
                    {
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            if (mat != null && mat.shader == targetShader)
                            {
                                AddSceneObjectResult(scene.name, renderer.gameObject);
                                break;
                            }
                        }
                    }
                }

                var graphics = root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var graphic in graphics)
                {
                    if (graphic.material != null && graphic.material.shader == targetShader)
                        AddSceneObjectResult(scene.name, graphic.gameObject);
                }
            }
        }
    }

    // ── 텍스쳐 검색 ─────────────────────────────────────

    void SearchTexture()
    {
        ResolveTexture();

        // 1) 어떤 머티리얼이 이 텍스쳐를 사용하는지
        SearchTextureInMaterials();

        // 2) 씬 내 UI (RawImage, Image 등)에서 텍스쳐 참조
        SearchTextureInCurrentScene();

        // 3) GUID 기반 파일 검색
        if (targetTexture != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(targetTexture));
            if (!string.IsNullOrEmpty(guid))
                SearchGUIDInFiles(guid, AssetDatabase.GetAssetPath(targetTexture));
        }

        SortAndPing("씬 내 오브젝트", "머티리얼", "씬", "프리팹", "에셋");
    }

    void ResolveTexture()
    {
        if (targetTexture != null) return;
        if (string.IsNullOrWhiteSpace(textureName)) return;

        var guids = AssetDatabase.FindAssets($"t:Texture {textureName}");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (tex != null && tex.name == textureName)
            {
                targetTexture = tex;
                return;
            }
        }
    }

    void SearchTextureInMaterials()
    {
        if (targetTexture == null) return;

        var matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var g in matGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // 머티리얼의 모든 텍스쳐 프로퍼티 검사
            var shader = mat.shader;
            int propCount = ShaderUtil.GetPropertyCount(shader);
            for (int p = 0; p < propCount; p++)
            {
                if (ShaderUtil.GetPropertyType(shader, p) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propName = ShaderUtil.GetPropertyName(shader, p);
                    Texture tex = mat.GetTexture(propName);
                    if (tex == targetTexture)
                    {
                        results.Add(new ResultEntry
                        {
                            category = "머티리얼",
                            path = path,
                            asset = mat
                        });
                        break; // 이 머티리얼에서 하나만 찾으면 충분
                    }
                }
            }
        }
    }

    void SearchTextureInCurrentScene()
    {
        if (targetTexture == null) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                // RawImage에서 texture 직접 참조 확인
                var rawImages = root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
                foreach (var ri in rawImages)
                {
                    if (ri.texture == targetTexture)
                        AddSceneObjectResult(scene.name, ri.gameObject);
                }

                // Image에서 sprite.texture 확인
                var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (img.sprite != null && img.sprite.texture == targetTexture)
                        AddSceneObjectResult(scene.name, img.gameObject);
                }

                // Renderer 머티리얼의 텍스쳐도 확인
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterials == null) continue;
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;
                        var shader = mat.shader;
                        int propCount = ShaderUtil.GetPropertyCount(shader);
                        bool found = false;
                        for (int p = 0; p < propCount; p++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, p) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                string propName = ShaderUtil.GetPropertyName(shader, p);
                                if (mat.GetTexture(propName) == targetTexture)
                                {
                                    AddSceneObjectResult(scene.name, renderer.gameObject);
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (found) break;
                    }
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════
    //  공통 유틸
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// GUID로 씬/프리팹/에셋/머티리얼 파일 내 참조를 검색
    /// </summary>
    void SearchGUIDInFiles(string guid, string selfAssetPath)
    {
        var extensions = new[] { "*.unity", "*.prefab", "*.asset", "*.mat" };
        var assetsPath = Application.dataPath;

        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(assetsPath, ext, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                if (content.Contains(guid))
                {
                    var relativePath = "Assets" + file.Replace(assetsPath, "").Replace('\\', '/');

                    // 자기 자신은 제외
                    if (!string.IsNullOrEmpty(selfAssetPath) && relativePath == selfAssetPath)
                        continue;

                    string category = ext switch
                    {
                        "*.unity" => "씬",
                        "*.prefab" => "프리팹",
                        _ => "에셋"
                    };

                    var asset = AssetDatabase.LoadMainAssetAtPath(relativePath);
                    results.Add(new ResultEntry
                    {
                        category = category,
                        path = relativePath,
                        asset = asset
                    });
                }
            }
        }
    }

    void AddSceneObjectResult(string sceneName, GameObject go)
    {
        if (foundSceneObjects.Contains(go)) return;

        foundSceneObjects.Add(go);
        results.Add(new ResultEntry
        {
            category = "씬 내 오브젝트",
            path = $"[{sceneName}] {GetHierarchyPath(go.transform)}",
            asset = go,
            isSceneObject = true
        });
    }

    void SortAndPing(params string[] order)
    {
        results = results
            .OrderBy(r => Array.IndexOf(order, r.category))
            .ThenBy(r => r.path)
            .ToList();

        if (foundSceneObjects.Count > 0)
        {
            Selection.objects = foundSceneObjects.ToArray();
            EditorGUIUtility.PingObject(foundSceneObjects[0]);
        }
    }

    static bool ArrayContains<T>(T[] array, T item) where T : UnityEngine.Object
    {
        if (array == null) return false;
        foreach (var element in array)
        {
            if (element == item) return true;
        }
        return false;
    }

    static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    static bool ContainsClassName(string content, string className)
    {
        int index = 0;
        while ((index = content.IndexOf(className, index, StringComparison.Ordinal)) >= 0)
        {
            if (index > 0 && IsIdentChar(content[index - 1]))
            {
                index += className.Length;
                continue;
            }

            int after = index + className.Length;
            if (after < content.Length && IsIdentChar(content[after]))
            {
                index += className.Length;
                continue;
            }

            return true;
        }
        return false;
    }

    static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
