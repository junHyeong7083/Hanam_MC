using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroStepController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button nextDialogueButton;
    [SerializeField] private Button proceedButton;

    [Header("Localization")]
    [SerializeField] private bool korean = true;

    [Header("Dialogue Source")]
    [SerializeField] private List<int> dialogueTextIds = new List<int>();

    [Header("Placeholder Mode")]
    [SerializeField] private bool usePlaceholder = true;
    [SerializeField] private int placeholderTextId = 0;
    [SerializeField] private int placeholderCount = 3;

    private LocalizedTable _localized;
    private int _cursor = 0;
    private int _total = 0;

    public void BindLocalizedTable(LocalizedTable table)
    {
        _localized = table;
    }

    private void Awake()
    {
        if (nextDialogueButton != null)
            nextDialogueButton.onClick.AddListener(OnClickNext);

        if (proceedButton != null)
        {
            proceedButton.onClick.AddListener(OnClickProceed);
            proceedButton.gameObject.SetActive(false);
            proceedButton.interactable = false;
        }
    }

    private void OnEnable()
    {
        StartIntro();
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
        _cursor++;

        if (_cursor >= _total)
        {
            FinishIntro();
            return;
        }

        RenderCurrent();
    }

    private void FinishIntro()
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

    private void RenderCurrent()
    {
        if (dialogueText == null) return;

        int textId = GetTextIdAt(_cursor);
        dialogueText.text = ResolveText(textId);
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

    private string ResolveText(int textId)
    {
        if (_localized == null)
            return $"<no LocalizedTable bound> (textId:{textId})";

        return _localized.Get(textId, korean);
    }

    private void OnClickProceed()
    {
        // TODO: 여기서 Step1(드래그앤드롭) 진입 호출을 걸면 됨.
        // 예: StepFlowController.EnterStep1();
        gameObject.SetActive(false);
    }
}