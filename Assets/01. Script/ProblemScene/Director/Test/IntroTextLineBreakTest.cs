using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// IntroTextLineBreakTest - CSV 텍스트 줄바꿈 미리보기 테스트용 스크립트
///
/// 【역할】 에디터/런타임 모두에서 CSV의 textId로 텍스트를 읽어 UI Text에 표시한다.
///          주로 인트로 화면의 텍스트가 줄바꿈이 올바르게 되는지 확인하는 데 사용한다.
///          [ExecuteAlways] 속성으로 에디터 편집 모드에서도 실시간으로 미리보기가 가능하다.
/// 【패턴】 독립 MonoBehaviour (테스트/디버그 전용)
/// 【참조되는 곳】 ProblemRuntime (런타임 시), LocalizedTable (에디터/비플레이 시 직접 CSV 로드)
/// 【위치】 테스트용 씬 또는 인트로 화면에 부착
/// 【흐름】 OnEnable/OnValidate → CSV에서 텍스트 읽기 → mainText, topText에 표시
/// </summary>
[ExecuteAlways]
public class IntroTextLineBreakTest : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] private Text mainText;    // 메인 텍스트 UI (본문 영역)
    [SerializeField] private Text topText;     // 상단 텍스트 UI (제목 영역)

    [Header("CSV (Resources path, no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01"; // CSV 리소스 경로 (확장자 제외)

    [Header("Language")]
    [SerializeField] private bool korean = true; // true: 한국어, false: 영어

    [Header("CSV Text Id")]
    [SerializeField] private int mainTextId;   // 메인 텍스트의 CSV textId
    [SerializeField] private int topTextId;    // 상단 텍스트의 CSV textId

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor = true; // 에디터 편집 모드에서 미리보기 활성화

    private LocalizedTable _localized;         // CSV 파싱 결과를 보관하는 로컬라이즈 테이블
    private string _loadedPathCache;           // CSV 경로 변경 감지용 캐시
    private bool _loadedLangCache;             // 언어 변경 감지용 캐시

    /// <summary>에디터 모드에서 활성화 시 미리보기 텍스트를 적용한다.</summary>
    private void OnEnable()
    {
        if (!Application.isPlaying && previewInEditor)
            ApplyTextsEditorSafe();
    }

    /// <summary>런타임 시작 시 텍스트를 적용한다.</summary>
    private void Start()
    {
        ApplyTextsEditorSafe();
    }

    /// <summary>인스펙터 값 변경 시 미리보기를 갱신한다.</summary>
    private void OnValidate()
    {
        if (!previewInEditor)
            return;

        ApplyTextsEditorSafe();
    }

    /// <summary>
    /// CSV에서 텍스트를 읽어 mainText, topText에 적용한다.
    /// 런타임에서는 ProblemRuntime.L을 우선 사용하고,
    /// 에디터/비플레이에서는 직접 CSV를 로드하여 표시한다.
    /// </summary>
    [ContextMenu("Apply Texts")]
    public void ApplyTexts()
    {
        // �÷��� ���̰� ProblemRuntime�� ������ �װ� �켱 ���
        if (Application.isPlaying && ProblemRuntime.I != null)
        {
            if (mainText != null) mainText.text = ProblemRuntime.L(mainTextId);
            if (topText != null) topText.text = ProblemRuntime.L(topTextId);
            return;
        }

        // ������/��Ÿ�� ����: ���� CSV �ε��ؼ� ǥ��
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

    /// <summary>
    /// ApplyTexts를 try-catch로 감싸 안전하게 호출한다.
    /// LocalizedTable 초기화 타이밍 문제 등으로 에러가 발생하면 대체 텍스트를 표시한다.
    /// </summary>
    private void ApplyTextsEditorSafe()
    {
        try
        {
            ApplyTexts();
        }
        catch
        {
            // LocalizedTable �ʱ�ȭ Ÿ�̹�/������ ���� �� ������ ��Ȳ���� �Ͻ� ���� ����
            if (mainText != null) mainText.text = $"<preview unavailable> (id:{mainTextId})";
            if (topText != null) topText.text = $"<preview unavailable> (id:{topTextId})";
        }
    }

    private void EnsureLocalizedLoaded()
    {
        bool needReload =
            _localized == null ||
            _loadedPathCache != localizedPath ||
            _loadedLangCache != korean; // ���� Get���� ó�������� �ν����� ���� �� ���� ������ �ǵ�

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