using UnityEngine;

public class ProblemRuntime : MonoBehaviour
{
    public static ProblemRuntime I { get; private set; }

    [Header("CSV (Resources path, no extension)")]
    [SerializeField] private string localizedPath = "CSV/MC_DataTable_v01";

    [Header("Language")]
    [SerializeField] private bool korean = true;

    public LocalizedTable Localized { get; private set; }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        // 이 씬 안에서만 쓰는 전역이면 DontDestroyOnLoad 굳이 필요 없음
        // 씬 넘어가도 유지 원하면 아래 주석 해제
        // DontDestroyOnLoad(gameObject);

        LoadLocalized();
    }

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

    // ====== 접근 편의 함수들 ======
    public string GetText(int textId)
    {
        if (Localized == null) return $"<no LocalizedTable> (textId:{textId})";
        return Localized.Get(textId, korean);
    }

    public string GetText(int textId, bool isKorean)
    {
        if (Localized == null) return $"<no LocalizedTable> (textId:{textId})";
        return Localized.Get(textId, isKorean);
    }

    // ====== static shortcut ======
    public static string L(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId);
    }

    public static string LK(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId, true);
    }

    public static string LE(int textId)
    {
        if (I == null) return $"<no ProblemRuntime> (textId:{textId})";
        return I.GetText(textId, false);
    }

    // 필요하면 런타임에서 언어 바꾸는 API도 추가 가능
    public void SetLanguageKorean(bool isKorean)
    {
        korean = isKorean;
    }
}