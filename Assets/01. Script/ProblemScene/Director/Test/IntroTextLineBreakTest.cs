using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class IntroTextLineBreakTest : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] private Text mainText;
    [SerializeField] private Text topText;

    [Header("CSV (Resources path, no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01";

    [Header("Language")]
    [SerializeField] private bool korean = true;

    [Header("CSV Text Id")]
    [SerializeField] private int mainTextId;
    [SerializeField] private int topTextId;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor = true;

    private LocalizedTable _localized;
    private string _loadedPathCache;
    private bool _loadedLangCache;

    private void OnEnable()
    {
        if (!Application.isPlaying && previewInEditor)
            ApplyTextsEditorSafe();
    }

    private void Start()
    {
        ApplyTextsEditorSafe();
    }

    private void OnValidate()
    {
        if (!previewInEditor)
            return;

        ApplyTextsEditorSafe();
    }

    [ContextMenu("Apply Texts")]
    public void ApplyTexts()
    {
        // 플레이 중이고 ProblemRuntime이 있으면 그걸 우선 사용
        if (Application.isPlaying && ProblemRuntime.I != null)
        {
            if (mainText != null) mainText.text = ProblemRuntime.L(mainTextId);
            if (topText != null) topText.text = ProblemRuntime.L(topTextId);
            return;
        }

        // 에디터/런타임 공용: 직접 CSV 로드해서 표시
        EnsureLocalizedLoaded();

        if (_localized == null)
        {
            if (mainText != null) mainText.text = $"<CSV load fail> (id:{mainTextId})";
            if (topText != null) topText.text = $"<CSV load fail> (id:{topTextId})";
            return;
        }

        if (mainText != null) mainText.text = _localized.Get(mainTextId, korean);
        if (topText != null) topText.text = _localized.Get(topTextId, korean);
    }

    private void ApplyTextsEditorSafe()
    {
        try
        {
            ApplyTexts();
        }
        catch
        {
            // LocalizedTable 초기화 타이밍/컴파일 직후 등 에디터 상황에서 일시 오류 무시
            if (mainText != null) mainText.text = $"<preview unavailable> (id:{mainTextId})";
            if (topText != null) topText.text = $"<preview unavailable> (id:{topTextId})";
        }
    }

    private void EnsureLocalizedLoaded()
    {
        bool needReload =
            _localized == null ||
            _loadedPathCache != localizedPath ||
            _loadedLangCache != korean; // 언어는 Get에서 처리되지만 인스펙터 변경 시 강제 재적용 의도

        if (!needReload)
            return;

        _localized = null;
        _loadedPathCache = localizedPath;
        _loadedLangCache = korean;

        var csv = Resources.Load<TextAsset>(localizedPath);
        if (csv == null)
        {
            Debug.LogError($"[IntroTextLineBreakEditorTest] Missing TextAsset: Resources/{localizedPath}");
            return;
        }

        _localized = new LocalizedTable();
        _localized.Load(csv);
    }
}