using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public IntroElement introElement;
        public ShakeTrigger shakeTrigger;
    }

    protected abstract FilmFragment[] Films { get; }
    protected abstract float DimAlpha { get; }
    protected abstract float NormalAlpha { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("필름 터치 효과음")]
    [SerializeField] private string filmClickSfx = "SFX_C01_S01_filmClick";

    private readonly Dictionary<int, FilmFragment> _filmMap = new Dictionary<int, FilmFragment>();
    private readonly HashSet<int> _checkedIds = new HashSet<int>();
    private readonly List<(IntroElement intro, System.Action handler)> _shakeBindings
        = new List<(IntroElement, System.Action)>();

    private bool _completed = false;
    private bool _interactionLocked = true;

    protected override void OnStepEnter()
    {
        BuildFilmMap();
        ResetState();

        BindShakeTriggers();

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        UnbindShakeTriggers();

        _checkedIds.Clear();
        _filmMap.Clear();
        _completed = false;
        _interactionLocked = true;
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
        if (_interactionLocked) return;

        if (!_filmMap.TryGetValue(id, out var fragment))
            return;

        // 효과음 재생
        if (!string.IsNullOrEmpty(filmClickSfx))
        {
            var sm = SoundManager.Instance;
            if (sm != null) sm.PlaySFX(filmClickSfx);
        }

        if (fragment.flashOverlay != null)
            StartCoroutine(FlashRoutine(fragment.flashOverlay, 0.1f));

        if (_checkedIds.Contains(id))
            return;

        _checkedIds.Add(id);

        // 터치했으니 떨림 정지
        if (fragment.shakeTrigger != null)
            fragment.shakeTrigger.StopShake();

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

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    private void BindShakeTriggers()
    {
        UnbindShakeTriggers();

        var films = Films;
        if (films == null) return;

        foreach (var f in films)
        {
            if (f == null || f.introElement == null || f.shakeTrigger == null) continue;

            var shake = f.shakeTrigger;
            System.Action handler = () => shake.StartShake();
            f.introElement.OnArrived += handler;
            _shakeBindings.Add((f.introElement, handler));
        }
    }

    private void UnbindShakeTriggers()
    {
        foreach (var (intro, handler) in _shakeBindings)
        {
            if (intro != null)
                intro.OnArrived -= handler;
        }
        _shakeBindings.Clear();
    }

    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}
