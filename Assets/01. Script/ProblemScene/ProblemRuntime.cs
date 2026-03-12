using UnityEngine;

/// <summary>
/// ProblemRuntime - ProblemScene 전체의 싱글턴 런타임 매니저
///
/// 【역할】 CSV 기반 다국어 텍스트 테이블(LocalizedTable)을 로드하고,
///          정적 메서드 L() / LK() / LE()를 통해 프로젝트 전역에서 textId로 텍스트를 조회할 수 있게 해준다.
///          DontDestroyOnLoad로 씬 전환 시에도 유지된다.
/// 【참조하는 곳】 프로젝트 내 거의 모든 스크립트에서 ProblemRuntime.L(textId) 형태로 텍스트를 가져옴
///                (StartStep, CommonRewardStep, 각 Problem Director Logic 등)
/// 【참조되는 곳】 LocalizedTable (CSV 파싱/조회 담당)
/// 【흐름】 Bootstrap 씬에서 Awake() → CSV 로드 → 이후 어떤 씬에서든 L(textId)로 텍스트 사용
/// </summary>
public class ProblemRuntime : MonoBehaviour
{
    /// <summary>싱글턴 인스턴스. ProblemRuntime.I 로 접근</summary>
    public static ProblemRuntime I { get; private set; }

    [Header("CSV (Resources path, no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01"; // CSV 파일의 Resources 경로 (확장자 제외)

    [Header("Language")]
    [SerializeField] private bool korean = true; // 기본 언어 설정 (true = 한국어, false = 영어)

    /// <summary>파싱된 다국어 텍스트 테이블. textId → 텍스트 매핑</summary>
    public LocalizedTable Localized { get; private set; }

    /// <summary>
    /// 싱글턴 초기화. 중복 인스턴스 방지 후 CSV 로드.
    /// ※ 이 클래스는 Bootstrap 씬의 루트 오브젝트에 붙어있어 Awake() 사용이 허용됨
    /// </summary>
    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        else
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        LoadLocalized();
    }

    /// <summary>
    /// Resources 폴더에서 CSV TextAsset을 로드하여 LocalizedTable에 파싱한다.
    /// CSV 경로: Assets/Resources/CSV/MC_DataTable_v01.csv
    /// </summary>
    private void LoadLocalized()
    {
        var csv = Resources.Load<TextAsset>(localizedPath);
        Localized = new LocalizedTable();

        if (csv == null)
        {
            Debug.LogError($"[ProblemRuntime] Missing TextAsset: Resources/{localizedPath}");
            return;
        }

        Localized.Load(csv);
    }

    // ====== 인스턴스 텍스트 조회 함수들 ======

    /// <summary>
    /// 현재 설정된 언어(korean 필드)로 textId에 해당하는 텍스트를 반환한다.
    /// </summary>
    /// <param name="textId">CSV DataTable의 텍스트 ID (예: 101000055)</param>
    /// <returns>해당 언어의 텍스트 문자열</returns>
    public string GetText(int textId)
    {
        if (Localized == null) return $"<no LocalizedTable> (textId:{textId})";
        return Localized.Get(textId, korean);
    }

    /// <summary>
    /// 언어를 명시적으로 지정하여 textId에 해당하는 텍스트를 반환한다.
    /// </summary>
    /// <param name="textId">CSV DataTable의 텍스트 ID</param>
    /// <param name="isKorean">true = 한국어, false = 영어</param>
    public string GetText(int textId, bool isKorean)
    {
        if (Localized == null) return $"<no LocalizedTable> (textId:{textId})";
        return Localized.Get(textId, isKorean);
    }

    // ====== static shortcut (전역에서 편리하게 호출) ======

    /// <summary>
    /// 【가장 많이 사용되는 메서드】 현재 언어 설정으로 텍스트를 조회한다.
    /// 사용 예: ProblemRuntime.L(101000055)
    /// </summary>
    /// <param name="textId">CSV DataTable의 텍스트 ID</param>
    public static string L(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId);
    }

    /// <summary>
    /// 강제로 한국어 텍스트를 조회한다. (Korean 고정)
    /// </summary>
    /// <param name="textId">CSV DataTable의 텍스트 ID</param>
    public static string LK(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId, true);
    }

    /// <summary>
    /// 강제로 영어 텍스트를 조회한다. (English 고정)
    /// </summary>
    /// <param name="textId">CSV DataTable의 텍스트 ID</param>
    public static string LE(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId, false);
    }

    /// <summary>
    /// 런타임에서 언어를 전환하는 API.
    /// </summary>
    /// <param name="isKorean">true = 한국어, false = 영어</param>
    public void SetLanguageKorean(bool isKorean)
    {
        korean = isKorean;
    }
}