using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Director / Problem_3 / Step1
/// - 인벤토리에서 '시나리오 펜'을 드래그해서 책 위로 올리는 단계.
/// </summary>
public class Director_Problem3_Step1 : ProblemStepBase, IStepInventoryDragHandler
{
    [Header("책 드롭 타겟")]
    [SerializeField] private RectTransform bookDropArea;
    [SerializeField] private GameObject dropIndicatorRoot;
    [SerializeField] private float dropRadius = 200f;

    [Header("책 활성화 연출")]
    [SerializeField] private RectTransform bookVisualRoot;
    [SerializeField] private float activateScale = 1.05f;
    [SerializeField] private float activateDuration = 0.6f;
    [SerializeField] private float delayBeforeComplete = 1.5f;

    [Header("안내 텍스트 / 기타 루트")]
    [SerializeField] private GameObject instructionRoot;

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이 스텝에서 사용할 펜 아이템 ID")]
    [SerializeField] private string penItemId = "pen";   // DB ItemId와 맞추기

    private bool _bookActivated;
    private bool _animPlaying;

    protected override void OnStepEnter()
    {
        ResetState();

        if (completionGate != null)
            completionGate.ResetGate(1);
    }

    private void ResetState()
    {
        _bookActivated = false;
        _animPlaying = false;

        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(false);

        if (instructionRoot != null)
            instructionRoot.SetActive(true);

        if (bookVisualRoot != null)
            bookVisualRoot.localScale = Vector3.one;
    }

    protected override void OnStepExit()
    {
    }

    // ==============================
    // IStepInventoryDragHandler 구현
    // ==============================

    public void OnInventoryDragBegin(StepInventoryItem item, PointerEventData eventData)
    {
        // 이 스텝에서는 'pen' 아이템만 처리
        if (_bookActivated) return;
        if (item == null) return;
        if (!item.IsDraggableThisStep)
            return;

        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(true);
    }

    public void OnInventoryDragging(StepInventoryItem item, PointerEventData eventData)
    {
        // 필요하면 드래그 중 이펙트 갱신. 지금은 안 써도 됨.
    }

    public void OnInventoryDragEnd(StepInventoryItem item, PointerEventData eventData)
    {
        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(false);

        if (item == null || eventData == null)
        {
            item?.ReturnToSlot();
            return;
        }

        if (_bookActivated || bookDropArea == null)
        {
            item.ReturnToSlot();
            return;
        }

        // 1) 책 중심 스크린 좌표
        var cam = eventData.pressEventCamera;
        Vector2 bookScreenPos = RectTransformUtility.WorldToScreenPoint(cam, bookDropArea.position);

        // 2) 드롭 위치
        Vector2 dropPos = eventData.position;

        // 3) 거리
        float dist = Vector2.Distance(bookScreenPos, dropPos);

        if (dist <= dropRadius)
        {
            // 성공: 고스트만 남기고 아이콘 숨김
            item.HideIconKeepGhost();
            StartCoroutine(HandleBookActivatedRoutine());
        }
        else
        {
            // 실패: 슬롯으로 복귀
            item.ReturnToSlot();
        }
    }

    private IEnumerator HandleBookActivatedRoutine()
    {
        if (_bookActivated || _animPlaying)
            yield break;

        _bookActivated = true;
        _animPlaying = true;

        if (instructionRoot != null)
            instructionRoot.SetActive(false);

        if (bookVisualRoot != null)
        {
            float t = 0f;
            Vector3 baseScale = Vector3.one;

            while (t < activateDuration)
            {
                t += Time.deltaTime;
                float x = Mathf.Clamp01(t / activateDuration);
                float s = Mathf.Sin(x * Mathf.PI);
                float scale = Mathf.Lerp(1f, activateScale, s);

                bookVisualRoot.localScale = Vector3.one * scale;
                yield return null;
            }

            bookVisualRoot.localScale = baseScale;
        }

        _animPlaying = false;

        if (delayBeforeComplete > 0f)
            yield return new WaitForSeconds(delayBeforeComplete);

        if (completionGate != null)
            completionGate.MarkOneDone();
    }
}
