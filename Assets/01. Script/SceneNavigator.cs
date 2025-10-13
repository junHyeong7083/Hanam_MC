// SceneNavigator.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum ScreenId { REGISTER, HOME, PROBLEM, RESULT }

public interface IScreenNavigator
{
    void GoTo(ScreenId id);
    void GoBack();
}


public class SceneNavigator : MonoBehaviour, IScreenNavigator
{
    [Header("Scene Names")]
    [SerializeField] string registerScene = "RegisterScene";
    [SerializeField] string homeScene = "HomeScene";
    [SerializeField] string problemScene = "ProblemScene";
    [SerializeField] string resultScene = "ResultScene";

    [Header("Optional Fade")]
    [SerializeField] CanvasGroup fade;   // Bootstrap의 CanvasGroup
    [SerializeField] float fadeSpeed = 7f;

    // (선택) 인증 가드
    public bool IsAuthenticated { get; set; }

    readonly Stack<ScreenId> history = new();
    ScreenId current;

    public void GoTo(ScreenId id) => StartCoroutine(CoGoTo(id));

    public void GoBack()
    {
        if (history.Count == 0) return;
        var prev = history.Pop();
        StartCoroutine(CoGoTo(prev, pushHistory: false));
    }

    IEnumerator CoGoTo(ScreenId id, bool pushHistory = true)
    {
        // 라우팅 가드: 인증 필요 화면 보호
        if (!IsAuthenticated && (id == ScreenId.HOME || id == ScreenId.PROBLEM || id == ScreenId.RESULT))
            id = ScreenId.REGISTER;

        yield return Fade(1f);

        string scene = id switch
        {
            ScreenId.REGISTER => registerScene,
            ScreenId.HOME => homeScene,
            ScreenId.PROBLEM => problemScene,
            ScreenId.RESULT => resultScene,
            _ => registerScene
        };

        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        if (pushHistory) history.Push(current);
        current = id;

        yield return Fade(0f);
    }

    IEnumerator Fade(float target)
    {
        if (!fade) yield break;
        fade.gameObject.SetActive(true);
        while (!Mathf.Approximately(fade.alpha, target))
        {
            fade.alpha = Mathf.MoveTowards(fade.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        fade.blocksRaycasts = target > 0.01f;   // 전환 중 클릭 방지
        if (target == 0f) fade.gameObject.SetActive(false);
    }
}
