# EffectController 가이드

## 개요
EffectController는 DOTween을 사용하여 UI 애니메이션을 관리하는 컴포넌트입니다.
각 Problem의 Step마다 별도의 EffectController를 생성하여 해당 Step의 애니메이션을 담당합니다.

---

## 1. 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                   EffectControllerBase                       │
│  - DOTween Sequence 관리                                     │
│  - 인트로 애니메이션 시스템                                    │
│  - 공통 유틸리티 메서드                                       │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ 상속
┌─────────────────────────────────────────────────────────────┐
│              Problem{N}_Step{M}_EffectController             │
│  - 해당 Step 전용 애니메이션                                  │
│  - 인트로, 대기, 선택, 결과 등 상태별 애니메이션               │
│  - 루프 트윈 관리                                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 폴더 구조

```
Assets/01. Script/Effect/
├── Common/
│   ├── EffectControllerBase.cs    # 베이스 클래스
│   ├── AppearAnimation.cs         # 등장 애니메이션 컴포넌트
│   ├── IntroElement.cs            # 인트로 애니메이션 컴포넌트
│   ├── PopupImageDisplay.cs       # 팝업 표시 컴포넌트
│   └── ButtonHover.cs             # 호버 효과 컴포넌트
├── Problem2/
│   ├── Problem2_Step1_EffectController.cs
│   ├── Problem2_Step2_EffectController.cs
│   └── Problem2_Step3_EffectController.cs
├── Problem7/
│   └── ...
└── ...
```

---

## 3. EffectControllerBase

### 3.1 주요 기능
```csharp
public abstract class EffectControllerBase : MonoBehaviour
{
    // ===== 인트로 시스템 =====
    [Header("===== 인트로 연출 =====")]
    [SerializeField] protected IntroElement[] introElements;
    [SerializeField] protected bool playOnEnable = true;

    // ===== Sequence 관리 =====
    protected Sequence _currentSequence;
    public bool IsAnimating => _currentSequence != null && _currentSequence.IsPlaying();

    // Sequence 생성 (이전 Sequence 자동 Kill)
    protected Sequence CreateSequence();

    // 현재 Sequence Kill
    protected void KillCurrentSequence();

    // ===== 인트로 메서드 =====
    public void PlayIntro(Action onComplete = null);
    protected void ResetIntroElements();

    // ===== 유틸리티 =====
    protected Tween DOFade(CanvasGroup cg, float endValue, float duration);
    protected Tween DOAnchorPos(RectTransform rt, Vector2 endValue, float duration);
    protected Tween DOScale(Transform t, float endValue, float duration);
    protected CanvasGroup GetOrAddCanvasGroup(GameObject go);
}
```

### 3.2 IntroElement 구조
```csharp
[Serializable]
public class IntroElement
{
    public RectTransform target;
    public IntroAnimationType animationType;  // Slide, Scale
    public float delay = 0f;
    public float duration = 0.3f;

    // Slide 타입 전용
    public SlideDirection direction;  // Up, Down, Left, Right
    public float distance = 50f;

    // Scale 타입 전용
    public float startScale = 0.3f;
}
```

---

## 4. Problem별 EffectController 작성법

### 4.1 기본 구조
```csharp
/// <summary>
/// Part 8 - Step 1 스토리보드 인트로 이펙트 컨트롤러
/// - 어시스턴트 말풍선 슬라이드 업 + 페이드
/// - 스토리보드 테이블 등장
/// - 프롬프트 텍스트 펄스
/// </summary>
public class Problem8_Step1_EffectController : EffectControllerBase
{
    // ===== 1. SerializeField UI 참조 =====
    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform speechCardRect;
    [SerializeField] private CanvasGroup speechCardCanvasGroup;
    [SerializeField] private float speechSlideDistance = 30f;
    [SerializeField] private float speechAppearDuration = 0.5f;

    [Header("===== 프롬프트 텍스트 =====")]
    [SerializeField] private CanvasGroup promptTextCanvasGroup;
    [SerializeField] private float promptMinAlpha = 0.6f;
    [SerializeField] private float promptMaxAlpha = 1f;
    [SerializeField] private float promptPulseDuration = 2f;

    // ===== 2. 루프 트윈 변수 =====
    private Tween _promptPulseTween;
    private Tween _floatTween;
    private Vector2 _basePosition;
    private bool _initialized;

    // ===== 3. 초기화 =====
    private void Awake()
    {
        SaveInitialState();
    }

    private void SaveInitialState()
    {
        if (_initialized) return;
        if (someRect != null)
            _basePosition = someRect.anchoredPosition;
        _initialized = true;
    }

    // ===== 4. Public API =====
    #region Public API - 인트로
    public void PlayIntroAnimation(Action onComplete = null) { }
    #endregion

    #region Public API - 대기 애니메이션
    public void StartIdleAnimations() { }
    public void StopIdleAnimations() { }
    #endregion

    #region Public API - 클릭 애니메이션
    public void PlayClickAnimation(Action onComplete = null) { }
    #endregion

    #region Reset
    public void ResetAll() { }
    #endregion

    // ===== 5. 정리 =====
    protected override void OnDisable()
    {
        base.OnDisable();
        StopIdleAnimations();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopIdleAnimations();
    }
}
```

### 4.2 인트로 애니메이션 구현
```csharp
public void PlayIntroAnimation(Action onComplete = null)
{
    SaveInitialState();

    var seq = CreateSequence();

    // 1. 말풍선 슬라이드 업 + 페이드
    if (speechCardRect != null && speechCardCanvasGroup != null)
    {
        Vector2 basePos = speechCardRect.anchoredPosition;
        speechCardRect.anchoredPosition = basePos + Vector2.down * speechSlideDistance;
        speechCardCanvasGroup.alpha = 0f;

        seq.Append(speechCardRect
            .DOAnchorPos(basePos, speechAppearDuration)
            .SetEase(Ease.OutQuad));
        seq.Join(speechCardCanvasGroup.DOFade(1f, speechAppearDuration));
    }

    // 2. 테이블 카드 (딜레이 0.6초)
    if (tableCardRect != null && tableCardCanvasGroup != null)
    {
        Vector2 basePos = tableCardRect.anchoredPosition;
        tableCardRect.anchoredPosition = basePos + Vector2.down * tableSlideDistance;
        tableCardCanvasGroup.alpha = 0f;

        seq.Insert(0.6f, tableCardRect
            .DOAnchorPos(basePos, speechAppearDuration)
            .SetEase(Ease.OutQuad));
        seq.Insert(0.6f, tableCardCanvasGroup.DOFade(1f, speechAppearDuration));
    }

    // 3. 완료 콜백
    seq.OnComplete(() =>
    {
        StartIdleAnimations();
        onComplete?.Invoke();
    });
}
```

### 4.3 루프 애니메이션 구현
```csharp
public void StartIdleAnimations()
{
    StopIdleAnimations();  // 기존 트윈 정리

    // 펄스: opacity [0.6, 1]
    if (promptTextCanvasGroup != null)
    {
        promptTextCanvasGroup.alpha = promptMinAlpha;
        _promptPulseTween = promptTextCanvasGroup
            .DOFade(promptMaxAlpha, promptPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // 플로팅: y 위치 변화
    if (floatRect != null)
    {
        _floatTween = floatRect
            .DOAnchorPosY(_basePosition.y + floatDistance, floatDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // 스케일 펄스: [1, 1.1]
    if (badgeRect != null)
    {
        _badgeScaleTween = badgeRect
            .DOScale(badgeMaxScale, badgePulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(Vector3.one * badgeMinScale);
    }
}

public void StopIdleAnimations()
{
    _promptPulseTween?.Kill();
    _floatTween?.Kill();
    _badgeScaleTween?.Kill();

    _promptPulseTween = null;
    _floatTween = null;
    _badgeScaleTween = null;
}
```

### 4.4 리셋 구현
```csharp
public void ResetAll()
{
    // 1. 모든 트윈 Kill
    KillCurrentSequence();
    StopIdleAnimations();
    SaveInitialState();

    // 2. 말풍선 리셋
    if (speechCardCanvasGroup != null)
    {
        DOTween.Kill(speechCardRect);
        DOTween.Kill(speechCardCanvasGroup);
        speechCardCanvasGroup.alpha = 0f;
    }

    // 3. 스토리보드 리셋
    if (storyboardRect != null)
    {
        DOTween.Kill(storyboardRect);
        storyboardRect.anchoredPosition = _basePosition;
        storyboardRect.localScale = Vector3.one;
        storyboardRect.localRotation = Quaternion.identity;
    }

    // 4. 뱃지 리셋
    if (newBadgeRect != null)
    {
        DOTween.Kill(newBadgeRect);
        newBadgeRect.localScale = Vector3.one;
        newBadgeRect.gameObject.SetActive(true);
    }
}
```

---

## 5. DOTween 사용 규칙

### 5.1 Sequence 관리
```csharp
// ✅ 올바른 방법: CreateSequence() 사용
public void PlayAnimation()
{
    var seq = CreateSequence();  // 이전 시퀀스 자동 Kill
    seq.Append(...);
}

// ❌ 잘못된 방법: 직접 생성
public void PlayAnimation()
{
    var seq = DOTween.Sequence();  // 누수 위험!
    seq.Append(...);
}
```

### 5.2 루프 트윈 관리
```csharp
// 루프 트윈은 별도 변수로 관리
private Tween _pulseTween;

public void StartPulse()
{
    StopPulse();  // 반드시 먼저 정리
    _pulseTween = target
        .DOFade(1f, 1f)
        .SetLoops(-1, LoopType.Yoyo);
}

public void StopPulse()
{
    _pulseTween?.Kill();
    _pulseTween = null;
}
```

### 5.3 정리 패턴
```csharp
protected override void OnDisable()
{
    base.OnDisable();  // EffectControllerBase 정리
    StopIdleAnimations();
    StopGlowPulse();
}

protected override void OnDestroy()
{
    base.OnDestroy();
    StopIdleAnimations();
    StopGlowPulse();
}
```

### 5.4 DOTween.Kill 사용
```csharp
// 특정 대상의 모든 트윈 Kill
DOTween.Kill(speechCardRect);
DOTween.Kill(speechCardCanvasGroup);

// 또는 target 지정하여 Kill
speechCardRect?.DOKill();
```

---

## 6. 공통 애니메이션 패턴

### 6.1 슬라이드 + 페이드
```csharp
// 아래에서 위로 등장
void SlideUpFade(Sequence seq, RectTransform rect, CanvasGroup cg,
    float distance, float duration, float delay = 0f)
{
    if (rect == null || cg == null) return;

    Vector2 basePos = rect.anchoredPosition;
    rect.anchoredPosition = basePos + Vector2.down * distance;
    cg.alpha = 0f;

    seq.Insert(delay, rect.DOAnchorPos(basePos, duration).SetEase(Ease.OutQuad));
    seq.Insert(delay, cg.DOFade(1f, duration));
}
```

### 6.2 스케일 팝
```csharp
// 작게 → 크게(오버슈트) → 원래 크기
void ScalePop(RectTransform rect, float duration, Action onComplete = null)
{
    if (rect == null) { onComplete?.Invoke(); return; }

    rect.localScale = Vector3.zero;
    rect.gameObject.SetActive(true);

    rect.DOScale(1f, duration)
        .SetEase(Ease.OutBack, 2f)
        .OnComplete(() => onComplete?.Invoke());
}
```

### 6.3 플립 애니메이션
```csharp
void PlayFlip(RectTransform rect, float duration, Action onComplete = null)
{
    if (rect == null) { onComplete?.Invoke(); return; }

    var seq = DOTween.Sequence();

    // 스케일 펀치: 1 → 1.2 → 1
    seq.Append(rect.DOScale(1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
    seq.Append(rect.DOScale(1f, duration * 0.7f).SetEase(Ease.OutQuad));

    // Y축 회전
    seq.Insert(0f, rect
        .DORotate(new Vector3(0, 360, 0), duration, RotateMode.FastBeyond360)
        .SetEase(Ease.OutQuad));

    seq.OnComplete(() => onComplete?.Invoke());
}
```

### 6.4 펄스 (Yoyo 루프)
```csharp
// 알파 펄스
Tween AlphaPulse(CanvasGroup cg, float min, float max, float duration)
{
    cg.alpha = min;
    return cg.DOFade(max, duration * 0.5f)
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);
}

// 스케일 펄스
Tween ScalePulse(RectTransform rect, float min, float max, float duration)
{
    return rect.DOScale(max, duration * 0.5f)
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo)
        .From(Vector3.one * min);
}
```

---

## 7. Header 그룹 규칙

```csharp
[Header("===== 인트로 카드 =====")]
[SerializeField] private RectTransform introCardRect;
[SerializeField] private CanvasGroup introCardCanvasGroup;
[SerializeField] private float introSlideDistance = 30f;

[Header("===== 액션 선택 =====")]
[SerializeField] private float actionHoverScale = 1.02f;
[SerializeField] private float actionTapScale = 0.98f;

[Header("===== 결과 화면 =====")]
[SerializeField] private RectTransform resultCardRect;
[SerializeField] private CanvasGroup resultCardCanvasGroup;
```

---

## 8. Region 구조

```csharp
public class Problem8_Step3_EffectController : EffectControllerBase
{
    // SerializeField들...

    #region Public API - 인트로
    public void PlayIntroAnimation(Action onComplete = null) { }
    public void PlayActionButtonsAppear(...) { }
    #endregion

    #region Public API - 액션 선택
    public void PlayActionHover(RectTransform actionRect) { }
    public void PlayActionUnhover(RectTransform actionRect, Vector2 originalPos) { }
    public void PlayActionTap(RectTransform actionRect) { }
    public void PlayActionSelect(RectTransform checkmarkRect, Action onComplete = null) { }
    public void StartSelectedGlowPulse(CanvasGroup glowCanvasGroup) { }
    public void StopSelectedGlowPulse() { }
    #endregion

    #region Public API - 결과 화면
    public void PlayResultAnimation(Action onComplete = null) { }
    #endregion

    #region Reset
    public void ResetAll() { }
    #endregion
}
```

---

## 9. 호버 효과 주의사항

### 9.1 호버는 ButtonHover 사용
```csharp
// ❌ 잘못된 방법: EffectController에서 호버 구현
public class Problem8_Step1_EffectController : EffectControllerBase
{
    private bool _hoverDisabled;

    public void PlayStoryboardHover(RectTransform rect)
    {
        if (_hoverDisabled) return;
        rect.DOScale(1.05f, 0.1f);
    }
}

// ✅ 올바른 방법: ButtonHover 컴포넌트 사용
// Director에서:
var buttonHover = button.GetComponent<ButtonHover>();
buttonHover?.SetInteractable(false);
```

### 9.2 참고: HoverGuide.md
호버 관련 상세 가이드는 `Assets/01. Script/md/HoverGuide.md` 참조

---

## 10. 새 EffectController 생성 가이드

### Step 1: 파일 생성
```
Assets/01. Script/Effect/Problem{N}/Problem{N}_Step{M}_EffectController.cs
```

### Step 2: 기본 구조 작성
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part {N} - Step {M} 이펙트 컨트롤러
/// - {기능1}
/// - {기능2}
/// </summary>
public class Problem{N}_Step{M}_EffectController : EffectControllerBase
{
    [Header("===== 주요 UI =====")]
    [SerializeField] private RectTransform mainRect;
    [SerializeField] private CanvasGroup mainCanvasGroup;

    private Tween _loopTween;
    private Vector2 _basePosition;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    private void SaveInitialState()
    {
        if (_initialized) return;
        if (mainRect != null)
            _basePosition = mainRect.anchoredPosition;
        _initialized = true;
    }

    #region Public API
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();
        // 애니메이션 로직...
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void ResetAll()
    {
        KillCurrentSequence();
        _loopTween?.Kill();
        _loopTween = null;
        SaveInitialState();
        // 리셋 로직...
    }
    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        _loopTween?.Kill();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _loopTween?.Kill();
    }
}
```

### Step 3: Scene에서 연결
1. Step Root에 EffectController 컴포넌트 추가
2. Inspector에서 UI 참조 연결
3. Director에서 EffectController 참조 및 호출
