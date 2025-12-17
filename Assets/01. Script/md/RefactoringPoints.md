# Hanam_MC 리팩토링 포인트

## 개요
코드베이스 분석 결과 발견된 리팩토링이 필요한 부분들을 우선순위별로 정리합니다.

---

## 1. 높은 우선순위 (즉시 개선 권장)

### 1.1 Director 폴더 구조 불일치
**문제:** 일부 Problem은 Logic/Binder 폴더 분리가 되어 있지 않음

**현재 상태:**
```
Director/
├── Problem1/
│   ├── Logic/           ✅ 분리됨
│   └── Binder/          ✅ 분리됨
├── Problem2/
│   ├── Logic/           ✅ 분리됨
│   └── Director_Problem2_Step1.cs  ❌ 루트에 Binder 존재
├── Problem3/
│   └── Director_Problem3_Step1.cs  ❌ 분리 안됨
├── Problem4/
│   └── Director_Problem4_Step1.cs  ❌ 분리 안됨
└── Problem7-10/
    ├── Logic/           ✅ 분리됨
    └── Binder/          ✅ 분리됨
```

**개선 방향:**
1. Problem2~6의 Binder 파일들을 `Binder/` 폴더로 이동
2. Logic이 없는 경우 Logic 클래스 추출 또는 현재 구조 유지 결정

**예상 작업:**
```
Director/Problem3/Director_Problem3_Step1.cs
→ Director/Problem3/Binder/Director_Problem3_Step1.cs
```

---

### 1.2 EffectController 중복 코드
**문제:** 각 Problem의 EffectController에서 인트로 애니메이션 로직이 중복됨

**현재 상태:**
```csharp
// Problem8_Step1_EffectController.cs
if (speechCardRect != null && speechCardCanvasGroup != null)
{
    Vector2 basePos = speechCardRect.anchoredPosition;
    speechCardRect.anchoredPosition = basePos + Vector2.down * speechSlideDistance;
    speechCardCanvasGroup.alpha = 0f;
    seq.Append(speechCardRect.DOAnchorPos(basePos, speechAppearDuration).SetEase(Ease.OutQuad));
    seq.Join(speechCardCanvasGroup.DOFade(1f, speechAppearDuration));
}

// Problem8_Step3_EffectController.cs (거의 동일)
if (instructionCardRect != null && instructionCardCanvasGroup != null)
{
    Vector2 basePos = instructionCardRect.anchoredPosition;
    instructionCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
    instructionCardCanvasGroup.alpha = 0f;
    seq.Append(instructionCardRect.DOAnchorPos(basePos, introAppearDuration).SetEase(Ease.OutQuad));
    seq.Join(instructionCardCanvasGroup.DOFade(1f, introAppearDuration));
}
```

**개선 방향:**
1. EffectControllerBase의 IntroElement 시스템 적극 활용
2. 또는 공통 헬퍼 메서드 추가:

```csharp
// EffectControllerBase에 추가
protected void AppendSlideUpFade(Sequence seq, RectTransform rect, CanvasGroup cg,
    float slideDistance, float duration, float delay = 0f)
{
    if (rect == null || cg == null) return;

    Vector2 basePos = rect.anchoredPosition;
    rect.anchoredPosition = basePos + Vector2.down * slideDistance;
    cg.alpha = 0f;

    seq.Insert(delay, rect.DOAnchorPos(basePos, duration).SetEase(Ease.OutQuad));
    seq.Insert(delay, cg.DOFade(1f, duration));
}
```

---

### 1.3 인코딩 문제 (한글 깨짐)
**문제:** 일부 파일에서 한글 주석/문자열이 깨져 있음

**영향받는 파일들:**
- `UserRepository.cs` - 주석이 ??? 문자로 표시
- `HomeSceneUI.cs` - 한글 문자열 깨짐
- `MultipleChoiceStepBase.cs` - Header 속성 한글 깨짐

**개선 방향:**
1. 파일을 UTF-8 with BOM으로 다시 저장
2. Unity Editor 설정에서 스크립트 인코딩 확인

---

### 1.4 StepBase 상속 체계 정리
**문제:** 일부 Director가 StepBase를 직접 상속하지 않고 있음

**현재 상태:**
- `Director_Problem3_Step1.cs` - ProblemStepBase 직접 상속하지 않는 경우 있음
- 일부 Step에서 OnStepEnter/Exit 패턴 미사용

**개선 방향:**
1. 모든 Director가 적절한 StepBase 상속하도록 통일
2. OnStepEnter/Exit 생명주기 패턴 일관성 유지

---

## 2. 중간 우선순위 (개선 권장)

### 2.1 AppearAnimation과 IntroElement 중복
**문제:** 기능이 유사한 두 컴포넌트가 존재

**현재:**
```csharp
// AppearAnimation.cs - OnEnable에서 자동 재생
public class AppearAnimation : MonoBehaviour
{
    [SerializeField] private float delay = 0f;
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Bottom;

    private void OnEnable()
    {
        PlayAnimation();  // 자동 재생
    }
}

// IntroElement.cs - 수동 호출 가능
public class IntroElement : MonoBehaviour
{
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Down;

    public void Play(Action onComplete = null);  // 수동 호출 지원
}
```

**개선 방향:**
1. IntroElement로 통합 (더 유연함)
2. AppearAnimation은 Deprecated 처리 후 점진적 교체

---

### 2.2 Repository 메서드 중복
**문제:** 여러 Repository에서 유사한 패턴 반복

**현재:**
```csharp
// UserRepository.cs
public User FindUserById(string id)
{
    if (string.IsNullOrWhiteSpace(id)) return null;
    return _db.WithDb(db => {
        var col = db.GetCollection<User>("users");
        col.EnsureIndex(x => x.Id, true);
        return col.FindById(id);
    });
}

// ResultRepository.cs (유사한 패턴)
public ResultDoc FindById(string id)
{
    if (string.IsNullOrWhiteSpace(id)) return null;
    return _db.WithDb(db => {
        var col = db.GetCollection<ResultDoc>("results");
        col.EnsureIndex(x => x.Id, true);
        return col.FindById(id);
    });
}
```

**개선 방향:**
제네릭 Repository Base 클래스 도입:

```csharp
public abstract class RepositoryBase<T> where T : class
{
    protected readonly IDBGateway _db;
    protected abstract string CollectionName { get; }

    public T FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _db.WithDb(db => {
            var col = db.GetCollection<T>(CollectionName);
            return col.FindById(id);
        });
    }
}
```

---

### 2.3 UI 클래스 이벤트 패턴 불일치
**문제:** UI 클래스마다 이벤트 처리 방식이 다름

**현재:**
```csharp
// HomeSceneUI - Action 이벤트 사용
public event Action OnStartProblemRequested;

// ThemePanelUI - Action<int> 이벤트 사용
public event Action<int> OnProblemClicked;

// 일부 UI - UnityEvent 사용
public UnityEvent onComplete;
```

**개선 방향:**
1. Action 이벤트로 통일 (UnityEvent는 인스펙터 바인딩 필요시만)
2. 이벤트 네이밍 규칙 통일: `On{동작}{상태}` (예: OnStartRequested, OnItemClicked)

---

### 2.4 STTManager 분리
**문제:** STTManager가 너무 많은 책임을 가짐

**현재 책임:**
- Whisper 모델 로딩
- 마이크 녹음
- 실시간 처리
- 스레드 관리
- 이벤트 발행

**개선 방향:**
```
STTManager (Facade)
├── WhisperContext (모델 관리)
├── MicrophoneRecorder (녹음)
├── RealtimeProcessor (실시간 처리)
└── STTEventDispatcher (이벤트)
```

---

## 3. 낮은 우선순위 (장기 개선)

### 3.1 Magic Number 제거
**문제:** 하드코딩된 숫자값들

```csharp
// 현재
seq.Insert(0.3f, speechBubbleRect...);  // 0.3f?
float delay = 0.5f + i * 0.1f;          // 0.5f, 0.1f?
protected override float DropRadius => 250f;  // 250f?

// 개선
private const float SpeechBubbleDelay = 0.3f;
private const float BaseDelay = 0.5f;
private const float ItemDelayInterval = 0.1f;
```

---

### 3.2 Enum 분리
**문제:** Model.cs에 모든 Enum이 정의됨

**현재:**
```csharp
// Model.cs
public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }
public enum ProblemTheme { Director = 0, Gardener = 1 }
public enum StepId { Step1, Step2, Step3, Step4, Reward, Extra1, Extra2 }
```

**개선 방향:**
```
Data/
├── Model.cs           (데이터 클래스만)
└── Enums/
    ├── UserRole.cs
    ├── ProblemTheme.cs
    └── StepId.cs
```

---

### 3.3 Service Interface 분리
**문제:** Service 인터페이스와 구현이 같은 파일에 존재

**현재:**
```csharp
// AuthService.cs
public interface IAuthService { ... }
public class AuthService : IAuthService { ... }
```

**개선 방향:**
```
Service/
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IProgressService.cs
│   └── IRewardService.cs
└── Implementations/
    ├── AuthService.cs
    ├── LocalProgressService.cs
    └── LocalRewardService.cs
```

---

### 3.4 EffectController 공통화
**문제:** Problem별로 EffectController가 완전히 별개로 구현됨

**개선 방향:**
공통 애니메이션 타입 정의:

```csharp
public enum CommonAnimationType
{
    SlideUpFade,
    SlideLeftFade,
    ScalePopFade,
    Pulse,
    Glow
}

// EffectControllerBase에 공통 메서드 추가
public void PlayCommonAnimation(CommonAnimationType type, RectTransform target,
    CanvasGroup cg, AnimationSettings settings);
```

---

## 4. 코드 품질 개선

### 4.1 Null 체크 패턴 통일
**현재:**
```csharp
// 패턴 1
if (rect != null && canvasGroup != null) { ... }

// 패턴 2
if (rect == null) return;
if (canvasGroup == null) return;

// 패턴 3
rect?.DOScale(1f, 0.3f);
```

**권장:**
- 필수 참조: 패턴 2 (early return)
- 선택적 처리: 패턴 1 (조건부 실행)
- 단일 호출: 패턴 3 (null 조건부 연산자)

---

### 4.2 로그 메시지 일관성
**현재:**
```csharp
Debug.Log($"[STT] {message}");
Debug.LogWarning("[ProblemStepBase] context 없음");
Debug.LogError($"[AuthService] Login error: {ex}");
```

**개선:**
로그 유틸리티 클래스 도입:
```csharp
public static class Logger
{
    public static void Info(string tag, string message)
        => Debug.Log($"[{tag}] {message}");

    public static void Warn(string tag, string message)
        => Debug.LogWarning($"[{tag}] {message}");

    public static void Error(string tag, string message, Exception ex = null)
        => Debug.LogError($"[{tag}] {message}" + (ex != null ? $"\n{ex}" : ""));
}

// 사용
Logger.Info("STT", "녹음 시작");
Logger.Error("AuthService", "Login error", ex);
```

---

## 5. 추천 작업 순서

### Phase 1: 즉시 (1-2일)
1. ~~인코딩 문제 수정~~ (UTF-8 저장)
2. Director 폴더 구조 통일
3. HoverGuide.md 규칙 전체 적용

### Phase 2: 단기 (1주)
1. EffectController 공통 메서드 추출
2. AppearAnimation → IntroElement 통합
3. Magic Number 상수화

### Phase 3: 중기 (2-3주)
1. Repository Base 클래스 도입
2. UI 이벤트 패턴 통일
3. Service Interface 분리

### Phase 4: 장기 (1개월+)
1. STTManager 책임 분리
2. Enum 파일 분리
3. 로그 유틸리티 도입

---

## 6. 참고: 잘 된 부분

### 6.1 일관된 패턴
- DataService 싱글톤 허브 패턴
- Result<T> 에러 처리 패턴
- DOTween Sequence 관리 (CreateSequence 패턴)

### 6.2 좋은 네이밍
- Repository/Service 명명 규칙
- EffectController 명명 규칙
- Header 그룹 사용

### 6.3 확장성
- StepBase 상속 체계
- Director Logic/Binder 분리
- 인터페이스 기반 설계

---

## 7. 주의사항

1. **리팩토링 시 테스트 필수** - Unity Play Mode에서 동작 확인
2. **점진적 변경** - 한 번에 많이 바꾸지 않기
3. **커밋 단위 분리** - 리팩토링 커밋과 기능 커밋 분리
4. **기존 동작 유지** - 외부 인터페이스 변경 최소화
