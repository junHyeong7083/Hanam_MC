# Hanam_MC 코드 스타일 가이드

## 1. 프로젝트 아키텍처 개요

### 1.1 폴더 구조
```
Assets/01. Script/
├── Data/                    # 데이터 레이어 (Repository, Service, Model)
│   ├── Repository/          # Repository 구현체들
│   └── Model.cs            # 데이터 모델 클래스들
├── Effect/                  # 이펙트/애니메이션 컨트롤러
│   ├── Common/             # 공통 이펙트 컴포넌트
│   └── Problem{N}/         # 문제별 이펙트 컨트롤러
├── ProblemScene/           # 문제 씬 관련
│   ├── Director/           # Director 패턴 (Logic + Binder)
│   ├── StepBases/          # Step 베이스 클래스들
│   └── StepComponent/      # Step 공통 컴포넌트
├── HomeScene/              # 홈 씬 UI
├── RegisterScene/          # 회원가입/로그인 UI
├── ResultScene/            # 결과 씬 UI
├── Service/                # 서비스 레이어
├── STT/                    # 음성인식 관련
└── md/                     # 문서 파일들
```

---

## 2. 핵심 디자인 패턴

### 2.1 Data Layer 패턴

#### Repository Pattern
Interface와 Implementation을 분리하여 데이터 접근을 추상화한다.

```csharp
// Interface 정의
public interface IUserRepository
{
    bool ExistsEmail(string email);
    User FindActiveUserByEmail(string email);
    void InsertUser(User user);
    void UpdateUser(User user);
}

// Implementation
public class UserRepository : IUserRepository
{
    private readonly IDBGateway _db;

    public UserRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public bool ExistsEmail(string email)
    {
        return _db.WithDb(db => {
            var col = db.GetCollection<User>("users");
            return col.Exists(u => u.Email == email);
        });
    }
}
```

**규칙:**
- Interface는 `I` 접두사 사용 (예: `IUserRepository`)
- Repository는 데이터 접근만 담당, 비즈니스 로직 X
- 생성자에서 null 체크 필수

#### DataService 싱글톤 허브
모든 Repository와 Service를 조립하는 중앙 허브.

```csharp
public class DataService : MonoBehaviour
{
    public static DataService Instance { get; private set; }

    // Repositories
    public IUserRepository UserRepository { get; private set; }
    public IProgressRepository ProgressRepository { get; private set; }

    // Services
    public IAuthService Auth { get; private set; }
    public IProgressService Progress { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Repository 조립
        var db = new DBGateway();
        UserRepository = new UserRepository(db);

        // Service 조립 (Repository 주입)
        Auth = new AuthService(UserRepository);
    }
}
```

**규칙:**
- Singleton은 MonoBehaviour 기반
- DontDestroyOnLoad 사용
- Repository → Service 순서로 조립

#### Model 클래스
순수 데이터 클래스, 동작 없음.

```csharp
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.USER;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**규칙:**
- Id는 Guid 문자열 사용
- CreatedAt 기본값은 DateTime.UtcNow
- Enum은 Model.cs에 함께 정의

---

### 2.2 Director 패턴 (Logic + Binder)

Step의 로직과 UI 참조를 분리하는 패턴.

#### Logic 클래스 (추상 클래스)
비즈니스 로직과 abstract property들을 정의.

```csharp
// Logic: 비즈니스 로직 담당
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    // Binder에서 구현할 abstract property
    protected abstract RectTransform MegaphoneDropTargetRect { get; }
    protected abstract GameObject MegaphoneDropIndicatorRoot { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    // 베이스 클래스 연결
    protected override RectTransform DropTargetRect => MegaphoneDropTargetRect;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    // 설정값
    protected override float DropRadius => 250f;
    protected override float ActivateScale => 1.05f;
}
```

#### Binder 클래스 (구체 클래스)
SerializeField로 UI 참조를 가지고, Logic의 abstract property 구현.

```csharp
// Binder: UI 참조 담당
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    [Header("드롭 대상 타깃 영역")]
    [SerializeField] private RectTransform megaphoneDropTargetRect;

    [Header("드롭 인디케이터")]
    [SerializeField] private GameObject megaphoneDropIndicatorRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // abstract property 구현
    protected override RectTransform MegaphoneDropTargetRect => megaphoneDropTargetRect;
    protected override GameObject MegaphoneDropIndicatorRoot => megaphoneDropIndicatorRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
```

**파일 구조:**
```
Director/Problem{N}/
├── Logic/
│   └── Director_Problem{N}_Step{M}_Logic.cs
└── Binder/
    └── Director_Problem{N}_Step{M}.cs
```

**규칙:**
- Logic은 `_Logic` 접미사
- Binder는 Logic을 상속
- UI 참조는 Binder에만 존재
- Logic에서 protected virtual로 설정값 오버라이드 가능

---

### 2.3 EffectController 패턴

DOTween 기반 애니메이션을 관리하는 패턴.

#### EffectControllerBase
```csharp
public abstract class EffectControllerBase : MonoBehaviour
{
    [Header("===== 인트로 연출 =====")]
    [SerializeField] protected IntroElement[] introElements;
    [SerializeField] protected bool playOnEnable = true;

    protected Sequence _currentSequence;

    public bool IsAnimating => _currentSequence != null && _currentSequence.IsPlaying();

    protected void KillCurrentSequence()
    {
        _currentSequence?.Kill();
        _currentSequence = null;
    }

    protected Sequence CreateSequence()
    {
        KillCurrentSequence();
        _currentSequence = DOTween.Sequence();
        return _currentSequence;
    }

    protected virtual void OnDisable()
    {
        KillCurrentSequence();
    }
}
```

#### Problem별 EffectController
```csharp
public class Problem8_Step1_EffectController : EffectControllerBase
{
    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform speechCardRect;
    [SerializeField] private CanvasGroup speechCardCanvasGroup;
    [SerializeField] private float speechSlideDistance = 30f;

    private Tween _promptPulseTween;

    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        if (speechCardRect != null && speechCardCanvasGroup != null)
        {
            Vector2 basePos = speechCardRect.anchoredPosition;
            speechCardRect.anchoredPosition = basePos + Vector2.down * speechSlideDistance;
            speechCardCanvasGroup.alpha = 0f;

            seq.Append(speechCardRect.DOAnchorPos(basePos, 0.5f).SetEase(Ease.OutQuad));
            seq.Join(speechCardCanvasGroup.DOFade(1f, 0.5f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void ResetAll()
    {
        KillCurrentSequence();
        _promptPulseTween?.Kill();
        // ... 초기화 로직
    }
}
```

**규칙:**
- CreateSequence() 사용하여 시퀀스 생성 (이전 시퀀스 자동 Kill)
- 루프 트윈은 별도 변수로 관리 (예: `_promptPulseTween`)
- OnDisable/OnDestroy에서 트윈 정리 필수
- ResetAll() 메서드로 초기 상태 복원

---

### 2.4 StepBase 패턴

문제 Step의 공통 기능을 제공하는 베이스 클래스들.

#### ProblemStepBase
```csharp
public abstract class ProblemStepBase : MonoBehaviour
{
    [Header("DB 저장 사용 여부")]
    [SerializeField] private bool useDBSave = true;

    [Header("공용 Problem 컨텍스트")]
    [SerializeField] protected ProblemContext context;

    [Header("이 스텝의 고유 키")]
    [SerializeField] protected StepKeyConfig stepKeyConfig;

    protected virtual void OnEnable() => OnStepEnter();
    protected virtual void OnDisable() => OnStepExit();

    protected abstract void OnStepEnter();
    protected virtual void OnStepExit() { }

    protected void SaveAttempt(object body) { /* DB 저장 */ }
}
```

#### 파생 StepBase들
```csharp
// 객관식 문제
public abstract class MultipleChoiceStepBase<TQuestion> : ProblemStepBase
{
    [SerializeField] protected Button[] optionButtons;
    protected abstract int QuestionCount { get; }
    protected abstract TQuestion GetQuestion(int index);
    protected abstract int GetCorrectOptionIndex(TQuestion q);
}

// 드래그앤드롭 문제
public abstract class InventoryDropTargetStepBase : ProblemStepBase, IStepInventoryDragHandler
{
    protected abstract RectTransform DropTargetRect { get; }
    protected virtual float DropRadius => 200f;
}
```

**규칙:**
- OnStepEnter/Exit로 Step 생명주기 관리
- SaveAttempt()로 진행 상황 DB 저장
- 제네릭 타입으로 문제 데이터 타입 추상화

---

## 3. 공통 컴포넌트 사용법

### 3.1 ButtonHover (호버 효과)
```csharp
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.08f;

    // 호버 비활성화 시
    public void SetInteractable(bool interactable);
}
```

**사용법:**
```csharp
// 호버 비활성화 (클릭 후 등)
var buttonHover = button.GetComponent<ButtonHover>();
buttonHover?.SetInteractable(false);
```

**주의:** EffectController에서 호버 관련 코드 작성 금지. 항상 ButtonHover.SetInteractable(false) 사용.

### 3.2 IntroElement / AppearAnimation (등장 애니메이션)
```csharp
// IntroElement: 외부 호출 또는 OnEnable 자동 재생
public class IntroElement : MonoBehaviour
{
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Down;

    public void Play(Action onComplete = null);
    public void SetDelay(float newDelay);
}

// AppearAnimation: OnEnable에서 자동 재생
public class AppearAnimation : MonoBehaviour
{
    [SerializeField] private float delay = 0f;
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Bottom;
}
```

### 3.3 PopupImageDisplay (팝업 표시)
```csharp
public class PopupImageDisplay : MonoBehaviour
{
    public void Show(Action onComplete = null);
    public void Hide(Action onComplete = null);
    public void HideImmediate();
}
```

---

## 4. 네이밍 규칙

### 4.1 클래스/파일
| 타입 | 패턴 | 예시 |
|------|------|------|
| Repository Interface | `I{Name}Repository` | `IUserRepository` |
| Repository Impl | `{Name}Repository` | `UserRepository` |
| Service Interface | `I{Name}Service` | `IAuthService` |
| Service Impl | `{Name}Service` | `AuthService` |
| Director Logic | `Director_Problem{N}_Step{M}_Logic` | `Director_Problem7_Step1_Logic` |
| Director Binder | `Director_Problem{N}_Step{M}` | `Director_Problem7_Step1` |
| EffectController | `Problem{N}_Step{M}_EffectController` | `Problem8_Step1_EffectController` |
| StepBase | `{기능}StepBase` | `MultipleChoiceStepBase` |
| UI 컴포넌트 | `{Scene/기능}UI` | `HomeSceneUI`, `ThemePanelUI` |

### 4.2 변수/필드
```csharp
// SerializeField: camelCase
[SerializeField] private RectTransform speechCardRect;
[SerializeField] private float slideDistance = 30f;

// private 필드: _접두사 + camelCase
private Tween _promptPulseTween;
private bool _isRecording;
private Vector2 _basePosition;

// const: PascalCase
private const string CUsers = "users";
private const int BcryptWorkFactor = 10;
```

### 4.3 Header 그룹
```csharp
[Header("===== 인트로 카드 =====")]
[SerializeField] private RectTransform cardRect;

[Header("===== 액션 선택 =====")]
[SerializeField] private float hoverScale = 1.02f;
```

---

## 5. DOTween 사용 규칙

### 5.1 Sequence 관리
```csharp
// 올바른 방법: CreateSequence() 사용
public void PlayAnimation()
{
    var seq = CreateSequence();  // 이전 시퀀스 자동 Kill
    seq.Append(...);
    seq.OnComplete(() => ...);
}

// 잘못된 방법: 직접 생성
public void PlayAnimation()
{
    var seq = DOTween.Sequence();  // 이전 시퀀스 누수!
    seq.Append(...);
}
```

### 5.2 루프 트윈 관리
```csharp
private Tween _pulseTween;

public void StartPulse()
{
    StopPulse();  // 기존 트윈 정리
    _pulseTween = targetCanvasGroup
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
    base.OnDisable();
    StopPulse();
    StopGlow();
}

protected override void OnDestroy()
{
    base.OnDestroy();
    StopPulse();
    StopGlow();
}
```

---

## 6. 이벤트 패턴

### 6.1 UI 이벤트
```csharp
public class HomeSceneUI : MonoBehaviour
{
    [SerializeField] Button startButton;

    // Action 이벤트 노출
    public event Action OnStartProblemRequested;

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(ClickStart);
    }

    void ClickStart() => OnStartProblemRequested?.Invoke();
}
```

### 6.2 STT 이벤트
```csharp
public class STTButton : MonoBehaviour
{
    public event Action<string> OnRecognitionComplete;
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

    private void HandleFinalResult(string text)
    {
        OnRecognitionComplete?.Invoke(text);
    }
}
```

---

## 7. 주석 스타일

### 7.1 클래스 주석
```csharp
/// <summary>
/// Part 8 - Step 1 스토리보드 인트로 이펙트 컨트롤러
/// - 어시스턴트 말풍선 슬라이드 업 + 페이드
/// - 스토리보드 테이블 등장
/// - 프롬프트 텍스트 펄스
/// </summary>
public class Problem8_Step1_EffectController : EffectControllerBase
```

### 7.2 메서드 주석
```csharp
/// <summary>
/// 인트로 화면 등장 애니메이션
/// </summary>
public void PlayIntroAnimation(Action onComplete = null)
```

### 7.3 region 사용
```csharp
#region Public API - 인트로
public void PlayIntroAnimation() { }
#endregion

#region Public API - 대기 애니메이션
public void StartIdleAnimations() { }
public void StopIdleAnimations() { }
#endregion

#region Reset
public void ResetAll() { }
#endregion
```

---

## 8. 싱글톤 패턴

```csharp
public class STTManager : MonoBehaviour
{
    public static STTManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
```

**규칙:**
- MonoBehaviour 기반
- Instance null 체크 후 Destroy
- DontDestroyOnLoad 사용
- OnDestroy에서 Instance 정리

---

## 9. 에러 처리

### 9.1 Result 패턴
```csharp
public class Result
{
    public bool Ok { get; }
    public string Error { get; }

    public static Result Success() => new Result(true, null);
    public static Result Fail(string error) => new Result(false, error);
}

public class Result<T>
{
    public bool Ok { get; }
    public T Data { get; }
    public string Error { get; }

    public static Result<T> Success(T data) => new Result<T>(true, data, null);
    public static Result<T> Fail(string error) => new Result<T>(false, default, error);
}
```

### 9.2 사용 예시
```csharp
public Result<User> Login(string email, string password)
{
    try
    {
        if (!AuthValidator.IsValidEmail(email))
            return Result<User>.Fail(AuthError.EmailInvalid);

        var user = _users.FindActiveUserByEmail(email);
        if (user == null)
            return Result<User>.Fail(AuthError.NotFoundOrInactive);

        return Result<User>.Success(user);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[AuthService] Login error: {ex}");
        return Result<User>.Fail(AuthError.Internal);
    }
}
```

---

## 10. 권장 사항

1. **EffectController에서 호버 관련 코드 금지** - ButtonHover.SetInteractable() 사용
2. **Logic/Binder 분리 유지** - UI 참조는 Binder에만
3. **DOTween Kill 필수** - OnDisable/OnDestroy에서 정리
4. **Null 체크 습관화** - SerializeField 사용 시 null 체크
5. **Action 이벤트 사용** - UI 상호작용 시 이벤트 기반 통신
