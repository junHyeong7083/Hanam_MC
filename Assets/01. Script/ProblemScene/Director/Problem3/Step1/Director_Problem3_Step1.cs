using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Director / Problem_3 / Step1
/// - 인벤토리에서 '시나리오 펜'을 드래그해서 책 위로 올리는 단계.
/// - 펜 드래그 중에는 책 주변에 드롭 타겟 이펙트를 보여준다.
/// - 드랍 지점이 책 중심에서 dropRadius 이내면 '책 활성화' 처리.
/// - 활성화 후 StepCompletionGate에 완료 알림(1칸짜리 게이트).
/// </summary>
public class Director_Problem3_Step1 : ProblemStepBase
{
    [Header("책 드롭 타겟")]
    [SerializeField] private RectTransform bookDropArea;         // 책(또는 드롭 영역) RectTransform
    [SerializeField] private GameObject dropIndicatorRoot;       // 드래그 중에 보여줄 동그란 이펙트
    [SerializeField] private float dropRadius = 200f;            // 픽셀 거리 기준 허용 반경

    [Header("책 활성화 연출")]
    [SerializeField] private RectTransform bookVisualRoot;       // 실제 책 비주얼(스케일 애니메이션 대상)
    [SerializeField] private float activateScale = 1.05f;
    [SerializeField] private float activateDuration = 0.6f;
    [SerializeField] private float delayBeforeComplete = 1.5f;   // 활성화 후 게이트 완료까지 딜레이

    [Header("안내 텍스트 / 기타 루트")]
    [SerializeField] private GameObject instructionRoot;         // "펜을 드래그해서 책 위에 올려주세요" 같은 안내

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;  // completeRoot / hideRoot 제어

    // 내부 상태
    private bool _isDraggingPen;
    private bool _bookActivated;
    private bool _animPlaying;

    protected override void OnStepEnter()
    {
        ResetState();

        // 이 스텝은 "펜을 책에 한 번 성공적으로 드랍"하면 완료로 보는 1칸짜리 게이트
        if (completionGate != null)
        {
            completionGate.ResetGate(1);
        }
    }
    private void ResetState()
    {
        _isDraggingPen = false;
        _bookActivated = false;
        _animPlaying = false;

        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(false);

        if (instructionRoot != null)
            instructionRoot.SetActive(true);

        // 책 스케일 초기화
        if (bookVisualRoot != null)
            bookVisualRoot.localScale = Vector3.one;
    }

    /// <summary>
    /// 인벤토리의 '펜' 아이콘에서 드래그 시작 시 호출해줄 함수.
    /// ex) InventoryPenDrag.OnBeginDrag()에서 호출
    /// </summary>
    public void NotifyPenDragBegin()
    {
        if (_bookActivated) return;

        _isDraggingPen = true;

        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(true);
    }

    /// <summary>
    /// 인벤토리의 '펜' 아이콘에서 드래그 종료 시 호출해줄 함수.
    /// eventData.position 기준으로 책 중심과의 거리를 계산한다.
    /// </summary>
    public void NotifyPenDragEnd(PointerEventData eventData)
    {
        _isDraggingPen = false;

        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(false);

        if (_bookActivated) return;
        if (bookDropArea == null || eventData == null)
            return;

        // 1) 책 중심의 스크린 좌표
        var cam = eventData.pressEventCamera;
        Vector2 bookScreenPos = RectTransformUtility.WorldToScreenPoint(cam, bookDropArea.position);

        // 2) 드랍 지점(포인터 위치)
        Vector2 dropPos = eventData.position;

        // 3) 거리 계산
        float dist = Vector2.Distance(bookScreenPos, dropPos);

        // 4) 반경 안이면 "책 활성화" 처리
        if (dist <= dropRadius)
        {
            StartCoroutine(HandleBookActivatedRoutine());
        }
        else
        {
            // 실패 시에는 여기서 펜 위치 복귀는 "펜 드래그 스크립트" 쪽에서 처리하도록 두고,
            // 이 스텝에서는 아무것도 하지 않아도 됨.
        }
    }

    private IEnumerator HandleBookActivatedRoutine()
    {
        if (_bookActivated || _animPlaying)
            yield break;

        _bookActivated = true;
        _animPlaying = true;

        // 안내 문구는 숨기기
        if (instructionRoot != null)
            instructionRoot.SetActive(false);

        // 책 비주얼 연출 (scale 업/다운, 반짝 느낌)
        if (bookVisualRoot != null)
        {
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            Vector3 targetScale = Vector3.one * activateScale;

            while (t < activateDuration)
            {
                t += Time.deltaTime;
                float x = Mathf.Clamp01(t / activateDuration);

                // 살짝 커졌다가 다시 1로 복귀 (퐁퐁)
                float s = Mathf.Sin(x * Mathf.PI); // 0->1->0
                float scale = Mathf.Lerp(1f, activateScale, s);

                bookVisualRoot.localScale = Vector3.one * scale;

                yield return null;
            }

            bookVisualRoot.localScale = baseScale;
        }

        _animPlaying = false;

        // TS 로직의 setTimeout(onStart, 1500) 느낌 → 약간의 딜레이 후 완료 처리
        if (delayBeforeComplete > 0f)
            yield return new WaitForSeconds(delayBeforeComplete);

        // StepCompletionGate가 있으면 완료 카운트 올리기 (1/1 → completeRoot on)
        if (completionGate != null)
        {
            completionGate.MarkOneDone();
        }
    }
}
