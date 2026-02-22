using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IntroStepController : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button nextDialogueButton;
    [SerializeField] private Button proceedButton;

    [Header("Flow Mode")]
    [Tooltip("true: Next로 대사 진행 후 Proceed 버튼이 활성화됨\nfalse: Proceed 없이 마지막 Next를 누르면 바로 완료 처리됨")]
    [SerializeField] private bool useProceedButton = true;

    [Header("Auto Finish (optional)")]
    [Tooltip("true면 부모 체인에서 StepFlowController를 찾아 nextStepMethodName을 자동 호출")]
    [SerializeField] private bool autoCallNextStep = true;
    [SerializeField] private string stepFlowControllerTypeName = "StepFlowController";
    [SerializeField] private string nextStepMethodName = "NextStep";

    [Header("Callbacks (Fallback)")]
    [Tooltip("autoCallNextStep가 실패했을 때 호출됨 (다음 패널 켜기 등)")]
    [SerializeField] private UnityEvent onFinished;

    [Header("Dialogue Source (textId list)")]
    [Tooltip("usePlaceholder=false일 때 사용. CSV의 index(textId)들을 순서대로 넣기")]
    [SerializeField] private List<int> dialogueTextIds = new List<int>();

    [Header("Placeholder Mode (temporary)")]
    [Tooltip("true면 placeholderTextId 하나를 placeholderCount번 반복 출력")]
    [SerializeField] private bool usePlaceholder = true;
    [SerializeField] private int placeholderTextId = 0;
    [SerializeField] private int placeholderCount = 3;

    private int _cursor = 0;
    private int _total = 0;

    private bool _listenersBound = false;

    // 자동 NextStep 호출용(리플렉션은 내부 캐시)
    private Component _flowController;
    private System.Reflection.MethodInfo _nextStepMethod;

    private void OnEnable()
    {
        BindListenersOnce();
        CacheStepFlowControllerIfNeeded();
        StartIntro();
    }

    private void OnDisable()
    {
        UnbindListenersIfNeeded();
    }

    private void BindListenersOnce()
    {
        if (_listenersBound) return;
        _listenersBound = true;

        if (nextDialogueButton != null)
            nextDialogueButton.onClick.AddListener(OnClickNext);

        if (proceedButton != null)
            proceedButton.onClick.AddListener(OnClickProceed);
    }

    private void UnbindListenersIfNeeded()
    {
        if (!_listenersBound) return;
        _listenersBound = false;

        if (nextDialogueButton != null)
            nextDialogueButton.onClick.RemoveListener(OnClickNext);

        if (proceedButton != null)
            proceedButton.onClick.RemoveListener(OnClickProceed);
    }

    public void StartIntro()
    {
        _cursor = 0;

        if (usePlaceholder)
            _total = Mathf.Max(1, placeholderCount);
        else
            _total = Mathf.Max(1, dialogueTextIds != null ? dialogueTextIds.Count : 0);

        if (nextDialogueButton != null)
        {
            nextDialogueButton.gameObject.SetActive(true);
            nextDialogueButton.interactable = true;
        }

        if (proceedButton != null)
        {
            proceedButton.gameObject.SetActive(false);
            proceedButton.interactable = false;
        }

        RenderCurrent();
    }
    private void OnClickNext()
    {
        // 다음 대사가 더 없으면(=이미 마지막을 보고 있음) 그냥 완료 처리
        if (_cursor >= _total - 1)
        {
            FinishIntroByMode();
            return;
        }

        // 다음 대사로 이동
        _cursor++;
        RenderCurrent();

        // 방금 출력한 대사가 마지막이면 즉시 완료 처리
        if (_cursor >= _total - 1)
        {
            // Proceed 모드면 Next 숨기고 Proceed 띄우는 걸 원하면 여기서 처리
            if (useProceedButton)
            {
                if (nextDialogueButton != null)
                {
                    nextDialogueButton.interactable = false;
                    nextDialogueButton.gameObject.SetActive(false);
                }

                if (proceedButton != null)
                {
                    proceedButton.gameObject.SetActive(true);
                    proceedButton.interactable = true;
                }
            }
            else
            {
                // Proceed 없이 마지막 Next가 바로 다음 패널 역할인 모드
                if (nextDialogueButton != null)
                    nextDialogueButton.interactable = false;

                InvokeFinish(); // 너 코드에 있는 finish 호출
            }
        }
    }
 

    private void FinishIntroByMode()
    {
        if (useProceedButton)
        {
            if (nextDialogueButton != null)
            {
                nextDialogueButton.interactable = false;
                nextDialogueButton.gameObject.SetActive(false);
            }

            if (proceedButton != null)
            {
                proceedButton.gameObject.SetActive(true);
                proceedButton.interactable = true;
            }
        }
        else
        {
            if (nextDialogueButton != null)
                nextDialogueButton.interactable = false;

            InvokeFinish();
        }
    }

    private void OnClickProceed()
    {
        InvokeFinish();
    }

    private void InvokeFinish()
    {
        if (autoCallNextStep && TryCallNextStep())
            return;

        onFinished?.Invoke();
    }

    private void RenderCurrent()
    {
        if (dialogueText == null) return;

        int textId = GetTextIdAt(_cursor);
        dialogueText.text = ProblemRuntime.L(textId);
    }

    private int GetTextIdAt(int idx)
    {
        if (usePlaceholder)
            return placeholderTextId;

        if (dialogueTextIds == null || dialogueTextIds.Count == 0)
            return placeholderTextId;

        if (idx < 0) idx = 0;
        if (idx >= dialogueTextIds.Count) idx = dialogueTextIds.Count - 1;
        return dialogueTextIds[idx];
    }

    private void CacheStepFlowControllerIfNeeded()
    {
        if (!autoCallNextStep) return;

        _flowController = null;
        _nextStepMethod = null;

        var monos = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
        {
            var mb = monos[i];
            if (mb == null) continue;

            var t = mb.GetType();
            if (t.Name != stepFlowControllerTypeName) continue;

            var mi = t.GetMethod(nextStepMethodName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

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
            CacheStepFlowControllerIfNeeded();
            if (_flowController == null || _nextStepMethod == null)
                return false;
        }

        _nextStepMethod.Invoke(_flowController, null);
        return true;
    }
}