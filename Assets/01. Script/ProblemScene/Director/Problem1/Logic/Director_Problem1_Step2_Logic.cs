using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class Director_Problem1_Step2_Logic : ProblemStepBase
{
    [System.Serializable]
    public class FilmFragment
    {
        public int id;
        public GameObject checkMark;
        public GameObject flashOverlay;
        public Graphic dimTarget;
        public Text buttonText;
        public FilmCardWiggle wiggle;
    }

    protected abstract FilmFragment[] Films { get; }
    protected abstract float DimAlpha { get; }
    protected abstract float NormalAlpha { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Guide Text (Localized)")]
    [SerializeField] private Text guideText;
    [Tooltip("스텝 진입 시 안내 문구 textId")]
    [SerializeField] private int guideTextIdOnEnter = 0;
    [Tooltip("필름 전부 선택 완료 시 안내 문구 textId")]
    [SerializeField] private int guideTextIdOnCompleted = 0;

    [Header("Next Shoot Button")]
    [SerializeField] private GameObject nextShootButtonRoot;
    [SerializeField] private Button nextShootButton;

    [Header("NextStep Auto Call")]
    [SerializeField] private bool autoCallNextStep = true;
    [SerializeField] private string stepFlowControllerTypeName = "StepFlowController";
    [SerializeField] private string nextStepMethodName = "NextStep";

    [Header("Fallback Callback")]
    [SerializeField] private UnityEvent onClickNextShootFallback;

    private readonly Dictionary<int, FilmFragment> _filmMap = new Dictionary<int, FilmFragment>();
    private readonly HashSet<int> _checkedIds = new HashSet<int>();

    private bool _completed = false;

    private Component _flowController;
    private MethodInfo _nextStepMethod;

    protected override void OnStepEnter()
    {
        BuildFilmMap();
        ResetState();

        ApplyGuideText(guideTextIdOnEnter);
        SetNextShootButtonVisible(false);

        CacheStepFlowController();
        BindNextShootButton();
    }

    protected override void OnStepExit()
    {
        UnbindNextShootButton();

        _checkedIds.Clear();
        _filmMap.Clear();
        _completed = false;
    }

    private void BuildFilmMap()
    {
        _filmMap.Clear();

        var films = Films;
        if (films == null) return;

        foreach (var f in films)
        {
            if (f == null) continue;
            if (!_filmMap.ContainsKey(f.id))
                _filmMap.Add(f.id, f);
        }
    }

    private void ResetState()
    {
        _checkedIds.Clear();
        _completed = false;

        var films = Films;
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f == null) continue;

                if (f.checkMark != null) f.checkMark.SetActive(false);
                if (f.flashOverlay != null) f.flashOverlay.SetActive(false);
                if (f.buttonText != null) f.buttonText.gameObject.SetActive(false);

                if (f.dimTarget != null)
                {
                    var c = f.dimTarget.color;
                    c.a = DimAlpha;
                    f.dimTarget.color = c;
                }

                if (f.wiggle != null)
                    f.wiggle.SetRandomRotationImmediate();
            }
        }

        var gate = CompletionGate;
        if (gate != null)
        {
            int total = (films != null) ? films.Length : 0;
            gate.ResetGate(total);
        }
    }

    public void OnFilmClicked(int id)
    {
        if (!_filmMap.TryGetValue(id, out var fragment))
            return;

        if (fragment.flashOverlay != null)
            StartCoroutine(FlashRoutine(fragment.flashOverlay, 0.1f));

        if (_checkedIds.Contains(id))
            return;

        _checkedIds.Add(id);

        if (fragment.checkMark != null) fragment.checkMark.SetActive(true);
        if (fragment.buttonText != null) fragment.buttonText.gameObject.SetActive(true);

        if (fragment.dimTarget != null)
        {
            var c = fragment.dimTarget.color;
            c.a = NormalAlpha;
            fragment.dimTarget.color = c;
        }

        var films = Films;
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f != null && f.wiggle != null)
                    f.wiggle.SetRandomRotation();
            }
        }

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();

        TryHandleCompleted();
    }

    private void TryHandleCompleted()
    {
        if (_completed) return;

        int total = (Films != null) ? Films.Length : 0;
        if (total <= 0) return;

        if (_checkedIds.Count < total) return;

        _completed = true;

        ApplyGuideText(guideTextIdOnCompleted);
        SetNextShootButtonVisible(true);
    }

    private void ApplyGuideText(int textId)
    {
        if (guideText == null) return;
        guideText.text = ProblemRuntime.L(textId);
    }

    private void SetNextShootButtonVisible(bool visible)
    {
        if (nextShootButtonRoot != null)
            nextShootButtonRoot.SetActive(visible);

        if (nextShootButton != null)
            nextShootButton.interactable = visible;
    }

    private void BindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
        nextShootButton.onClick.AddListener(OnClickNextShoot);
    }

    private void UnbindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
    }

    private void OnClickNextShoot()
    {
        if (!_completed) return;

        if (autoCallNextStep && TryCallNextStep())
            return;

        onClickNextShootFallback?.Invoke();
    }

    private void CacheStepFlowController()
    {
        _flowController = null;
        _nextStepMethod = null;

        if (!autoCallNextStep) return;

        var monos = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
        {
            var mb = monos[i];
            if (mb == null) continue;

            var t = mb.GetType();
            if (t.Name != stepFlowControllerTypeName) continue;

            var mi = t.GetMethod(nextStepMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi == null) continue;
            if (mi.GetParameters().Length != 0) continue;

            _flowController = mb;
            _nextStepMethod = mi;
            break;
        }
    }

    private bool TryCallNextStep()
    {
        if (_flowController == null || _nextStepMethod == null)
        {
            CacheStepFlowController();
            if (_flowController == null || _nextStepMethod == null)
                return false;
        }

        _nextStepMethod.Invoke(_flowController, null);
        return true;
    }

    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}