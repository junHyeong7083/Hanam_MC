using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Bootstrap : MonoBehaviour
{
    private static bool s_Initialized = false;

    public static Bootstrap I { get; private set; }

    // 여기서 전역 접근 가능
    public LocalizedTable Localized { get; private set; }

    [Header("Resources CSV Path (no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01";

    void Awake()
    {
        // 중복 방지
        if (s_Initialized)
        {
            Destroy(gameObject);
            return;
        }
        s_Initialized = true;

        I = this;
        DontDestroyOnLoad(gameObject);

        // 데이터베이스 인덱스 초기화 (앱 시작 시 한 번만)
        DatabaseInitializer.InitializeIndexes();

        // CSV 로드 (앱 시작 시 한 번만)
        LoadTables();

        // SceneNavigator 준비될 때까지 기다리기
        StartCoroutine(InitRoutine());
    }

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

    IEnumerator InitRoutine()
    {
        while (SceneNavigator.Instance == null)
            yield return null;

        var session = SessionManager.Instance;
        bool authed = (session != null && session.IsSignedIn);

        SceneNavigator.Instance.GoTo(authed ? ScreenId.HOME : ScreenId.REGISTER);
    }
}