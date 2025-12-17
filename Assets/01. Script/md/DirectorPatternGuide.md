# Director 패턴 가이드 (Logic + Binder)

## 개요
Director 패턴은 Step의 **비즈니스 로직**과 **UI 참조**를 분리하여 코드의 재사용성과 유지보수성을 높입니다.

---

## 1. 패턴 구조

```
┌──────────────────────────────────────────────────────────┐
│                    ProblemStepBase                        │
│              (OnStepEnter/Exit, SaveAttempt)              │
└──────────────────────────────────────────────────────────┘
                            ▲
                            │ 상속
┌──────────────────────────────────────────────────────────┐
│                  StepBase 파생 클래스                      │
│    (MultipleChoiceStepBase, InventoryDropTargetStepBase)  │
└──────────────────────────────────────────────────────────┘
                            ▲
                            │ 상속
┌──────────────────────────────────────────────────────────┐
│                  Director_Logic                           │
│     - abstract property로 UI 참조 정의                     │
│     - 비즈니스 로직 구현                                    │
│     - 설정값 override 가능                                 │
└──────────────────────────────────────────────────────────┘
                            ▲
                            │ 상속
┌──────────────────────────────────────────────────────────┐
│                  Director_Binder                          │
│     - SerializeField로 실제 UI 참조                        │
│     - abstract property 구현                               │
│     - Scene에 붙이는 컴포넌트                               │
└──────────────────────────────────────────────────────────┘
```

---

## 2. 폴더 구조

```
Assets/01. Script/ProblemScene/Director/
├── Problem1/
│   ├── Logic/
│   │   ├── Director_Problem1_Step1_Logic.cs
│   │   ├── Director_Problem1_Step2_Logic.cs
│   │   └── Director_Problem1_Step3_Logic.cs
│   └── Binder/
│       ├── Director_Problem1_Step1.cs
│       ├── Director_Problem1_Step2.cs
│       └── Director_Problem1_Step3.cs
├── Problem7/
│   ├── Logic/
│   │   ├── Director_Problem7_Step1_Logic.cs
│   │   └── ...
│   └── Binder/
│       ├── Director_Problem7_Step1.cs
│       └── ...
└── ...
```

---

## 3. Logic 클래스 작성법

### 3.1 기본 구조
```csharp
/// <summary>
/// Director / Problem7 / Step1 로직 베이스
/// - 인벤토리에서 '격려의 메가폰'을 NG 모니터로 드래그
/// - 드롭 성공 시 CompletionGate 활성화
/// </summary>
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    // ============================================
    // 1. Binder에서 구현할 abstract property들
    // ============================================
    protected abstract RectTransform MegaphoneDropTargetRect { get; }
    protected abstract GameObject MegaphoneDropIndicatorRoot { get; }
    protected abstract RectTransform MegaphoneTargetVisualRoot { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    // ============================================
    // 2. 베이스 클래스 연결
    // ============================================
    protected override RectTransform DropTargetRect => MegaphoneDropTargetRect;
    protected override GameObject DropIndicatorRoot => MegaphoneDropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => MegaphoneTargetVisualRoot;
    protected override GameObject InstructionRoot => null;  // 사용 안함
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    // ============================================
    // 3. 설정값 override
    // ============================================
    protected override float DropRadius => 250f;
    protected override float ActivateScale => 1.05f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.0f;

    // ============================================
    // 4. 추가 로직 (필요시)
    // ============================================
    protected override void OnDropComplete()
    {
        base.OnDropComplete();
        // 커스텀 로직...
    }
}
```

### 3.2 abstract property 정의 규칙
```csharp
// 필수 UI 요소: abstract
protected abstract RectTransform DropTargetRect { get; }
protected abstract StepCompletionGate CompletionGateRef { get; }

// 선택적 UI 요소: virtual + null 반환
protected virtual GameObject OptionalElement => null;
protected virtual RectTransform OptionalRect => null;

// 설정값: virtual + 기본값
protected virtual float Duration => 0.5f;
protected virtual int MaxCount => 3;
```

### 3.3 로직 구현 예시 (복잡한 경우)
```csharp
public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    // UI 참조
    protected abstract Button[] OptionButtons { get; }
    protected abstract Text QuestionText { get; }
    protected abstract StepCompletionGate CompletionGateRef { get; }

    // 설정
    protected virtual int QuestionCount => 5;

    // 상태
    private int _currentQuestion;
    private bool _stepCompleted;

    protected override void OnStepEnter()
    {
        _currentQuestion = 0;
        _stepCompleted = false;
        CompletionGateRef?.ResetGate(QuestionCount);
        ShowQuestion(_currentQuestion);
        SetupButtons();
    }

    private void SetupButtons()
    {
        for (int i = 0; i < OptionButtons.Length; i++)
        {
            int idx = i;  // 클로저 캡처 방지
            OptionButtons[i].onClick.RemoveAllListeners();
            OptionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
        }
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (_stepCompleted) return;

        bool correct = CheckAnswer(optionIndex);
        if (correct)
        {
            CompletionGateRef?.MarkOneDone();
            _currentQuestion++;

            if (_currentQuestion >= QuestionCount)
            {
                _stepCompleted = true;
                OnAllQuestionsCompleted();
            }
            else
            {
                ShowQuestion(_currentQuestion);
            }
        }
        else
        {
            OnWrongAnswer(optionIndex);
        }
    }

    // Binder나 파생 클래스에서 override 가능
    protected virtual void ShowQuestion(int index) { }
    protected virtual bool CheckAnswer(int optionIndex) => false;
    protected virtual void OnWrongAnswer(int optionIndex) { }
    protected virtual void OnAllQuestionsCompleted() { }
}
```

---

## 4. Binder 클래스 작성법

### 4.1 기본 구조
```csharp
/// <summary>
/// Director / Problem7 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder
/// - 실제 로직은 Director_Problem7_Step1_Logic(부모)에 있음
/// </summary>
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    // ============================================
    // 1. SerializeField UI 참조
    // ============================================
    [Header("드롭 대상 타깃 영역 (NG 모니터 박스)")]
    [SerializeField] private RectTransform megaphoneDropTargetRect;

    [Header("드롭 인디케이터 (드래그 중 테두리 박스)")]
    [SerializeField] private GameObject megaphoneDropIndicatorRoot;

    [Header("활성화 연출용 비주얼 루트 (스케일 튕김 대상)")]
    [SerializeField] private RectTransform megaphoneTargetVisualRoot;

    [Header("완료 게이트 (CompleteRoot에 활성화 패널 연결)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ============================================
    // 2. abstract property 구현
    // ============================================
    protected override RectTransform MegaphoneDropTargetRect => megaphoneDropTargetRect;
    protected override GameObject MegaphoneDropIndicatorRoot => megaphoneDropIndicatorRoot;
    protected override RectTransform MegaphoneTargetVisualRoot => megaphoneTargetVisualRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
```

### 4.2 Binder 확장 (추가 기능 필요시)
```csharp
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    // 기본 UI 참조
    [Header("===== 기본 요소 =====")]
    [SerializeField] private RectTransform dropTargetRect;
    [SerializeField] private StepCompletionGate completionGate;

    // 추가 UI 참조 (이 Step에서만 필요)
    [Header("===== 추가 요소 =====")]
    [SerializeField] private Problem7_Step1_EffectController effectController;
    [SerializeField] private AudioClip successSound;

    // abstract property 구현
    protected override RectTransform MegaphoneDropTargetRect => dropTargetRect;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;

    // 추가 기능 override
    protected override void OnDropComplete()
    {
        base.OnDropComplete();

        // 이펙트 재생
        effectController?.PlaySuccessAnimation();

        // 사운드 재생
        if (successSound != null)
            AudioSource.PlayClipAtPoint(successSound, Vector3.zero);
    }
}
```

---

## 5. StepBase 선택 가이드

### 5.1 사용 가능한 StepBase들

| StepBase | 용도 | 주요 기능 |
|----------|------|----------|
| `ProblemStepBase` | 기본 Step | OnStepEnter/Exit, SaveAttempt |
| `MultipleChoiceStepBase<T>` | 객관식 문제 | 문제/옵션 관리, 정답 체크 |
| `InventoryDropTargetStepBase` | 드래그앤드롭 | 드롭 영역, 아이템 소비 |
| `RandomCardSequenceStepBase` | 카드 순서 맞추기 | 랜덤 카드, 순서 검증 |

### 5.2 선택 기준
```
문제 유형이 무엇인가?
├── 객관식/선택형 → MultipleChoiceStepBase<T>
├── 드래그앤드롭 → InventoryDropTargetStepBase
├── 카드 순서 맞추기 → RandomCardSequenceStepBase
└── 그 외 / 커스텀 → ProblemStepBase
```

---

## 6. 명명 규칙

### 6.1 파일명
```
Director_Problem{N}_Step{M}_Logic.cs   (Logic)
Director_Problem{N}_Step{M}.cs         (Binder)
```

### 6.2 클래스명
```csharp
// Logic
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase

// Binder
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
```

### 6.3 Property명
```csharp
// Logic의 abstract property: {기능명}Ref 또는 {기능명}Root
protected abstract RectTransform DropTargetRect { get; }
protected abstract StepCompletionGate CompletionGateRef { get; }
protected abstract GameObject InstructionRoot { get; }

// Binder의 SerializeField: camelCase
[SerializeField] private RectTransform dropTargetRect;
[SerializeField] private StepCompletionGate completionGate;
```

---

## 7. 패턴 적용 시 주의사항

### 7.1 Logic에서 하면 안 되는 것
```csharp
// ❌ 잘못된 예: Logic에서 직접 SerializeField 사용
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    [SerializeField] private RectTransform dropTarget;  // ❌ 금지!
}

// ✅ 올바른 예: abstract property 사용
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    protected abstract RectTransform DropTargetRect { get; }  // ✅
}
```

### 7.2 Binder에서 하면 안 되는 것
```csharp
// ❌ 잘못된 예: Binder에서 복잡한 로직 구현
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    protected override void OnStepEnter()
    {
        base.OnStepEnter();
        // 여기에 복잡한 비즈니스 로직... ❌
        for (int i = 0; i < 10; i++)
        {
            // 긴 처리 로직...
        }
    }
}

// ✅ 올바른 예: 로직은 Logic에, Binder는 UI 바인딩만
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    // UI 참조와 property 구현만
    protected override RectTransform DropTargetRect => dropTargetRect;
}
```

### 7.3 상태 변수 위치
```csharp
// Logic에 상태 변수 배치
public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    // 상태 변수
    private int _currentQuestion;      // ✅ Logic에 위치
    private bool _stepCompleted;       // ✅ Logic에 위치
    private float _elapsedTime;        // ✅ Logic에 위치

    // UI 참조는 abstract
    protected abstract Button[] OptionButtons { get; }
}
```

---

## 8. 새 Director 생성 가이드

### Step 1: Logic 클래스 생성
```csharp
// Director/Problem{N}/Logic/Director_Problem{N}_Step{M}_Logic.cs

/// <summary>
/// Problem{N} Step{M} 로직
/// - {기능 설명}
/// </summary>
public abstract class Director_Problem{N}_Step{M}_Logic : {적절한StepBase}
{
    // 1. UI 참조 abstract property
    protected abstract RectTransform MainTargetRect { get; }
    protected abstract StepCompletionGate CompletionGateRef { get; }

    // 2. 설정값 (필요시 override)
    protected virtual float Duration => 0.5f;

    // 3. 로직 구현
    protected override void OnStepEnter()
    {
        // 초기화 로직
    }
}
```

### Step 2: Binder 클래스 생성
```csharp
// Director/Problem{N}/Binder/Director_Problem{N}_Step{M}.cs

/// <summary>
/// Problem{N} Step{M} Binder
/// - UI 참조 바인딩
/// </summary>
public class Director_Problem{N}_Step{M} : Director_Problem{N}_Step{M}_Logic
{
    [Header("===== 주요 UI =====")]
    [SerializeField] private RectTransform mainTargetRect;
    [SerializeField] private StepCompletionGate completionGate;

    // abstract property 구현
    protected override RectTransform MainTargetRect => mainTargetRect;
    protected override StepCompletionGate CompletionGateRef => completionGate;
}
```

### Step 3: Scene에 추가
1. Step GameObject에 Binder 컴포넌트 추가
2. Inspector에서 UI 참조 연결
3. ProblemContext, StepKeyConfig 설정

---

## 9. 마이그레이션 가이드 (기존 코드 → Logic/Binder 분리)

### Before (분리 전)
```csharp
public class Director_Problem3_Step1 : ProblemStepBase
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Button[] buttons;
    [SerializeField] private StepCompletionGate gate;

    private int _currentIndex;

    protected override void OnStepEnter()
    {
        _currentIndex = 0;
        gate.ResetGate(3);
        SetupButtons();
    }

    private void SetupButtons() { ... }
    private void OnButtonClicked(int idx) { ... }
}
```

### After (분리 후)

**Logic:**
```csharp
public abstract class Director_Problem3_Step1_Logic : ProblemStepBase
{
    protected abstract RectTransform TargetRect { get; }
    protected abstract Button[] Buttons { get; }
    protected abstract StepCompletionGate GateRef { get; }

    private int _currentIndex;

    protected override void OnStepEnter()
    {
        _currentIndex = 0;
        GateRef?.ResetGate(3);
        SetupButtons();
    }

    private void SetupButtons() { ... }
    private void OnButtonClicked(int idx) { ... }
}
```

**Binder:**
```csharp
public class Director_Problem3_Step1 : Director_Problem3_Step1_Logic
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Button[] buttons;
    [SerializeField] private StepCompletionGate gate;

    protected override RectTransform TargetRect => targetRect;
    protected override Button[] Buttons => buttons;
    protected override StepCompletionGate GateRef => gate;
}
```
