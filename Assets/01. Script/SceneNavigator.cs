using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 앱에서 사용하는 화면(씬) 식별자.
/// SceneNavigator.GoTo()에 전달하여 씬 전환을 수행한다.
/// </summary>
public enum ScreenId { REGISTER, HOME, PROBLEM, RESULT }

/// <summary>
/// SceneNavigator - 씬 전환 관리자 (싱글톤, DontDestroyOnLoad)
///
/// 【역할】 ScreenId 기반의 씬 전환을 담당한다. 페이드 인/아웃 애니메이션을 지원하며,
///          씬 이동 이력(history)을 스택으로 관리하여 GoBack() 기능을 제공한다.
///          인증이 필요한 씬(HOME, PROBLEM, RESULT)은 로그인 상태를 자동 검증한다.
/// 【참조하는 곳】 Bootstrap (초기 씬 전환), GameManager (홈/로그아웃), ProblemScene 각종 컨트롤러
/// 【참조되는 곳】 SessionManager (인증 상태 확인), UnityEngine.SceneManagement (실제 씬 로드)
/// 【흐름】 GoTo(ScreenId) → IsAllowed(인증 검증) → Fade(1) → LoadSceneAsync → Fade(0)
/// </summary>
public class SceneNavigator : MonoBehaviour
{
    /// <summary>전역 싱글톤 인스턴스</summary>
    public static SceneNavigator Instance { get; private set; }

    /// <summary>회원가입/로그인 씬 이름</summary>
    [Header("Scene Names")]
    [SerializeField] string registerScene = "RegisterScene";
    /// <summary>홈(온보딩) 씬 이름</summary>
    [SerializeField] string homeScene = "HomeScene";
    /// <summary>문제 풀이 씬 이름. 모든 문제가 이 씬 하나에서 진행됨</summary>
    [SerializeField] string problemScene = "ProblemScene";
    /// <summary>결과 화면 씬 이름</summary>
    [SerializeField] string resultScene = "ResultScene";

    /// <summary>씬 전환 시 페이드 인/아웃에 사용하는 CanvasGroup. DontDestroyOnLoad과 함께 유지됨</summary>
    [Header("Optional Fade")]
    [SerializeField] CanvasGroup fade;
    /// <summary>페이드 애니메이션 속도. 값이 클수록 빠르게 전환됨</summary>
    [SerializeField] float fadeSpeed = 7f;

    /// <summary>씬 이동 이력 스택. GoBack()에서 이전 씬으로 돌아갈 때 사용</summary>
    readonly Stack<ScreenId> history = new();
    /// <summary>현재 표시 중인 화면 ID</summary>
    ScreenId current = ScreenId.REGISTER;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>지정된 화면으로 씬 전환을 시작한다. 페이드 인/아웃이 적용됨</summary>
    /// <param name="id">이동할 화면 ID (REGISTER, HOME, PROBLEM, RESULT)</param>
    public void GoTo(ScreenId id) => StartCoroutine(CoGoTo(id));

    /// <summary>이전 화면으로 돌아간다. history 스택이 비어있으면 아무 동작 안함</summary>
    public void GoBack()
    {
        if (history.Count == 0) return;
        GoTo(history.Pop());
    }

    /// <summary>
    /// 실제 씬 전환 코루틴. 인증 검증 → 페이드 아웃 → 씬 로드 → 페이드 인 순서로 진행.
    /// 인증이 필요한 씬인데 미로그인이면 REGISTER로 강제 이동한다.
    /// </summary>
    IEnumerator CoGoTo(ScreenId id)
    {
        if (!IsAllowed(id))
            id = ScreenId.REGISTER;

        yield return Fade(1f);

        var name = SceneNameOf(id);
        var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        history.Push(current);
        current = id;

        yield return Fade(0f);
    }

    /// <summary>
    /// 대상 씬에 대한 접근 권한 검사. HOME, PROBLEM, RESULT는 로그인 필수.
    /// 미로그인 시 false를 반환하여 CoGoTo에서 REGISTER로 리다이렉트된다.
    /// </summary>
    bool IsAllowed(ScreenId target)
    {
        bool needAuth = (target == ScreenId.HOME || target == ScreenId.PROBLEM || target == ScreenId.RESULT);
        return !needAuth || (SessionManager.Instance != null && SessionManager.Instance.IsSignedIn);
    }

    /// <summary>ScreenId를 실제 Unity 씬 이름 문자열로 변환</summary>
    string SceneNameOf(ScreenId id) => id switch
    {
        ScreenId.REGISTER => registerScene,
        ScreenId.HOME => homeScene,
        ScreenId.PROBLEM => problemScene,
        ScreenId.RESULT => resultScene,
        _ => registerScene
    };

    /// <summary>
    /// 페이드 애니메이션 코루틴. target=1이면 화면을 가리고, target=0이면 화면을 보여준다.
    /// fade CanvasGroup이 할당되지 않았으면 즉시 완료된다.
    /// </summary>
    IEnumerator Fade(float target)
    {
        if (!fade) yield break;
        fade.gameObject.SetActive(true);
        while (!Mathf.Approximately(fade.alpha, target))
        {
            fade.alpha = Mathf.MoveTowards(fade.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        fade.blocksRaycasts = target > 0.01f;
        if (target == 0f) fade.gameObject.SetActive(false);
    }
}
