using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem_2 / Step1 전용 로직.
/// - 장면 아이템들을 드래그해서 가운데 드래그 박스에 놓는 단계.
/// - 아이템을 드래그박스 위로 가져오면 드래그박스 바깥 흰색 선이 활성화.
/// - 드롭이 성공하면 숨겨져 있던 설명 텍스트/버튼들이 나타남.
/// - DB 저장은 이 단계에서는 사용하지 않음.
/// </summary>
public class Director_Problem2_Step1 : MonoBehaviour
{
    [Header("Drag Box")]
    [SerializeField] private RectTransform dropBoxArea;     // 드래그 박스 RectTransform
    [SerializeField] private GameObject dropBoxOutline;     // 드래그 박스 바깥 흰색 선 (SetActive로 On/Off)

    [Header("Items")]
    [SerializeField] private Director_Problem2_DragItem[] dragItems;

    [Header("UI After Drop")]
    [SerializeField] private GameObject resultPanelRoot;    // 드롭 이후에 등장할 글/버튼 전체 루트 (처음엔 비활성)

    [Header("Icon Images")]
    [SerializeField] private Image iconImageBackground;     // 원래 자리에서 계속 남아 있을 배경
    [SerializeField] private Image iconImage;               // 드래그용 아이콘(DragItem가 붙어있는 애)

    // ===== 등장 애니메이션용 루트 =====
    [Header("Intro Animation Roots")]
    [Tooltip("왼쪽에서 오른쪽으로 슬라이드 인할 루트")]
    [SerializeField] private RectTransform leftEnterRoot;

    [Tooltip("오른쪽에서 왼쪽으로 슬라이드 인할 루트")]
    [SerializeField] private RectTransform rightEnterRoot;

    [Header("Intro Animation Settings")]
    [SerializeField] private float introDuration = 0.5f;      // 양쪽 슬라이드 시간
    [SerializeField] private float leftStartOffsetX = -300f;  // 왼쪽 루트 시작 오프셋 (음수 방향)
    [SerializeField] private float rightStartOffsetX = 300f;  // 오른쪽 루트 시작 오프셋 (양수 방향)
    [SerializeField] private float introDelay = 0.1f;         // 시작 전 살짝 딜레이

    // 내부용: 원래 위치 캐시
    private bool _leftInit;
    private bool _rightInit;
    private Vector2 _leftBasePos;
    private Vector2 _rightBasePos;
    private CanvasGroup _leftCg;
    private CanvasGroup _rightCg;

    // 현재 드래그 중인 아이템
    private Director_Problem2_DragItem _currentDraggingItem;
    // 최종 드롭된 아이템(선택된 장면)
    private Director_Problem2_DragItem _selectedItem;


    private void OnEnable()
    {
        InitState();
        StartCoroutine(PlayIntroAnimationRoutine());
    }

    private void OnDisable()
    {
        // 필요하면 상태 초기화/로그 등 추가
    }

    private void InitState()
    {
        _currentDraggingItem = null;
        _selectedItem = null;

        if (dropBoxOutline != null)
            dropBoxOutline.SetActive(false);

        // 결과 패널(설명 글 + 버튼들) 숨기기
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        // 아이템 원위치/비주얼 초기화
        if (dragItems != null)
        {
            foreach (var item in dragItems)
            {
                if (item != null)
                {
                    item.ResetToOriginalState();
                    item.SetStepController(this);
                }
            }
        }

        // 등장 애니메이션용 루트 초기 위치/알파 세팅
        InitIntroRoot(leftEnterRoot, ref _leftInit, ref _leftBasePos, leftStartOffsetX, ref _leftCg);
        InitIntroRoot(rightEnterRoot, ref _rightInit, ref _rightBasePos, rightStartOffsetX, ref _rightCg);

        // 아이콘/배경 초기 상태
        if (iconImageBackground != null)
            iconImageBackground.gameObject.SetActive(true);

        if (iconImage != null)
            iconImage.gameObject.SetActive(true);
    }

    private void InitIntroRoot(
        RectTransform root,
        ref bool inited,
        ref Vector2 basePos,
        float offsetX,
        ref CanvasGroup cg
    )
    {
        if (root == null)
            return;

        if (!inited)
        {
            basePos = root.anchoredPosition;
            _ = basePos; // just to silence warnings if needed
            inited = true;

            // CanvasGroup이 없으면 추가해서 알파 컨트롤
            cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = root.gameObject.AddComponent<CanvasGroup>();
        }

        // 시작할 때: 원래 위치 + X 오프셋에서 시작, 알파 0
        root.anchoredPosition = basePos + new Vector2(offsetX, 0f);
        if (cg != null)
            cg.alpha = 0f;
    }

    private System.Collections.IEnumerator PlayIntroAnimationRoutine()
    {
        if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        float t = 0f;

        Vector2 leftStartPos = Vector2.zero;
        Vector2 leftEndPos = Vector2.zero;
        Vector2 rightStartPos = Vector2.zero;
        Vector2 rightEndPos = Vector2.zero;

        if (leftEnterRoot != null && _leftInit)
        {
            leftEndPos = _leftBasePos;
            leftStartPos = _leftBasePos + new Vector2(leftStartOffsetX, 0f);
            leftEnterRoot.anchoredPosition = leftStartPos;
            if (_leftCg != null) _leftCg.alpha = 0f;
        }

        if (rightEnterRoot != null && _rightInit)
        {
            rightEndPos = _rightBasePos;
            rightStartPos = _rightBasePos + new Vector2(rightStartOffsetX, 0f);
            rightEnterRoot.anchoredPosition = rightStartPos;
            if (_rightCg != null) _rightCg.alpha = 0f;
        }

        while (t < introDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / introDuration);
            float eased = Mathf.SmoothStep(0f, 1f, x);

            if (leftEnterRoot != null && _leftInit)
            {
                leftEnterRoot.anchoredPosition = Vector2.Lerp(leftStartPos, leftEndPos, eased);
                if (_leftCg != null) _leftCg.alpha = x;
            }

            if (rightEnterRoot != null && _rightInit)
            {
                rightEnterRoot.anchoredPosition = Vector2.Lerp(rightStartPos, rightEndPos, eased);
                if (_rightCg != null) _rightCg.alpha = x;
            }

            yield return null;
        }

        // 마지막 값 보정
        if (leftEnterRoot != null && _leftInit)
        {
            leftEnterRoot.anchoredPosition = _leftBasePos;
            if (_leftCg != null) _leftCg.alpha = 1f;
        }

        if (rightEnterRoot != null && _rightInit)
        {
            rightEnterRoot.anchoredPosition = _rightBasePos;
            if (_rightCg != null) _rightCg.alpha = 1f;
        }
    }

    /// <summary>
    /// DraggableItem에서 드래그 시작 시 호출
    /// </summary>
    public void NotifyDragBegin(Director_Problem2_DragItem item)
    {
        _currentDraggingItem = item;

        // 드래그 시작 시에는 아웃라인 끔,
        // 드래그 중에 dropBox 위로 올라올 때만 켜짐.
        if (dropBoxOutline != null)
            dropBoxOutline.SetActive(false);
    }

    /// <summary>
    /// 드래그 중 – 포인터 위치에 따라 드래그박스 위에 있는지 검사.
    /// </summary>
    public void NotifyDragging(Director_Problem2_DragItem item, PointerEventData eventData)
    {
        if (dropBoxArea == null || dropBoxOutline == null)
            return;

        bool isOver =
            RectTransformUtility.RectangleContainsScreenPoint(
                dropBoxArea,
                eventData.position,
                eventData.pressEventCamera
            );

        // 3. 아이템이 드래그박스 "위로 들어간 순간" 아웃라인 on/off
        dropBoxOutline.SetActive(isOver);
    }

    /// <summary>
    /// 드래그 종료 – 드롭 성공/실패 판단.
    /// </summary>
    public void NotifyDragEnd(Director_Problem2_DragItem item, PointerEventData eventData)
    {
        if (dropBoxArea == null || dropBoxOutline == null)
            return;

        bool isOver =
            RectTransformUtility.RectangleContainsScreenPoint(
                dropBoxArea,
                eventData.position,
                eventData.pressEventCamera
            );

        // 드래그 종료 후에는 기본적으로 흰 선 끄기
        dropBoxOutline.SetActive(false);

        if (isOver)
        {
            // 드롭 성공
            OnItemDroppedIntoBox(item);
        }
        else
        {
            // 드롭 실패 → 원 위치로 복귀 + 고스트/알파 복원
            item.ReturnToOriginalPosition();
            item.RestoreOriginalAlpha();
        }

        _currentDraggingItem = null;
    }

    /// <summary>
    /// 실제로 드래그 박스 안에 아이템이 떨어졌을 때 처리.
    /// </summary>
    private void OnItemDroppedIntoBox(Director_Problem2_DragItem item)
    {
        _selectedItem = item;

        // 아이템을 놓은 순간 → 숨겨져 있던 글/버튼 등장
        if (resultPanelRoot != null)
        {
            resultPanelRoot.SetActive(true);
            if (dropBoxOutline != null)
                dropBoxOutline.gameObject.SetActive(false);
        }

        // 드래그 박스 안의 위치로 정렬
        item.SnapToDropBoxCenter(dropBoxArea);

        // 드롭 성공 시: 원래 자리에서 드래그용 아이콘만 숨기기
        // iconImageBackground는 남기고, iconImage만 비활성화

        iconImage.gameObject.SetActive(false);
        iconImageBackground.gameObject.SetActive(true);
    }
}
