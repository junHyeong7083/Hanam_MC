using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Bootstrap - 앱 최초 진입점 (Bootstrap 씬에서 실행)
///
/// 【역할】 앱이 시작될 때 가장 먼저 실행되어, 전역 초기화를 수행하는 엔트리 포인트.
///          DB 인덱스 생성, CSV 데이터 테이블 로드, 초기 씬 전환을 담당한다.
/// 【참조하는 곳】 없음 (Bootstrap 씬에 배치되어 자동 실행됨)
/// 【참조되는 곳】 DatabaseInitializer (DB 인덱스 초기화), LocalizedTable (CSV 텍스트 테이블),
///                SceneNavigator (씬 전환), SessionManager (로그인 상태 확인)
/// 【흐름】 Awake() → DatabaseInitializer.InitializeIndexes() → LoadTables(CSV)
///         → InitRoutine() 코루틴 → SceneNavigator.GoTo(HOME 또는 REGISTER)
/// </summary>
public class Bootstrap : MonoBehaviour
{
    /// <summary>중복 초기화 방지용 정적 플래그. 씬 재로드 시에도 한 번만 실행되도록 보장</summary>
    private static bool s_Initialized = false;

    /// <summary>전역 싱글톤 접근자. Bootstrap.I.Localized 로 텍스트 테이블에 접근 가능</summary>
    public static Bootstrap I { get; private set; }

    /// <summary>CSV에서 로드한 텍스트 데이터 테이블. ProblemRuntime.L(textId)에서 내부적으로 사용됨</summary>
    public LocalizedTable Localized { get; private set; }

    /// <summary>Resources 폴더 내 CSV 파일 경로 (확장자 제외). 기본값: "CSV/MC_DataTable_v01"</summary>
    [Header("Resources CSV Path (no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01";

    void Awake()
    {
        // 중복 방지: 이미 초기화된 경우 자신을 파괴하고 리턴
        if (s_Initialized)
        {
            Destroy(gameObject);
            return;
        }
        s_Initialized = true;

        I = this;
        DontDestroyOnLoad(gameObject);

        // 데이터베이스 인덱스 초기화 (앱 시작 시 한 번만)
        // → LiteDB의 각 컬렉션(users, results, inventory 등)에 인덱스 생성
        DatabaseInitializer.InitializeIndexes();

        // CSV 로드 (앱 시작 시 한 번만)
        // → Resources/CSV/MC_DataTable_v01.csv 파일을 LocalizedTable로 파싱
        LoadTables();

        // SceneNavigator 준비될 때까지 기다린 후 초기 씬으로 전환
        StartCoroutine(InitRoutine());
    }

    /// <summary>
    /// Resources 폴더에서 CSV 파일을 로드하여 LocalizedTable에 파싱한다.
    /// CSV에는 textId별 한국어 텍스트가 들어있으며, ProblemRuntime.L(textId)로 조회된다.
    /// </summary>
    private void LoadTables()
    {
        var csv = Resources.Load<TextAsset>(localizedPath);
        if (csv == null)
        {
            Debug.LogError($"[Bootstrap] TextAsset not found: Resources/{localizedPath}");
            Localized = new LocalizedTable();
            return;
        }

        Localized = new LocalizedTable();
        Localized.Load(csv);
    }

    /// <summary>
    /// SceneNavigator 싱글톤이 준비될 때까지 대기한 후,
    /// 세션 상태에 따라 HOME(로그인됨) 또는 REGISTER(미로그인) 씬으로 이동한다.
    /// SessionManager에 저장된 세션이 있으면 자동 로그인 처리됨.
    /// </summary>
    IEnumerator InitRoutine()
    {
        // SceneNavigator는 DontDestroyOnLoad 오브젝트이므로, 같은 프레임에 생성되지 않을 수 있음
        while (SceneNavigator.Instance == null)
            yield return null;

        var session = SessionManager.Instance;
        bool authed = (session != null && session.IsSignedIn);

        // 로그인 상태면 홈 화면, 아니면 회원가입/로그인 화면으로 이동
        SceneNavigator.Instance.GoTo(authed ? ScreenId.HOME : ScreenId.REGISTER);
    }
}