# Problem8 Step3 - 첫 장면 결정 (음성 녹음)

## 개요
사용자가 첫 장면을 선택하고 음성을 녹음하는 단계입니다.

## 파일 구조

```
Problem8/
├── Logic/
│   └── Director_Problem8_Step3_Logic.cs   # 로직 베이스 클래스
└── Binder/
    └── Director_Problem8_Step3.cs         # UI 참조 바인더
```

## 흐름 (Flow)

```
SelectAction → Recording → Result → 3초 후 자동 완료
```

1. **SelectActionRoot** 활성화 (3개 버튼, 텍스트 동적 바인딩)
2. **액션 버튼 클릭** → RecordingRoot 표시 (SelectActionRoot 유지)
3. **녹음 버튼 첫 클릭** → 녹음 시작
4. **녹음 버튼 재클릭** → 녹음 종료 → DB 저장 → SelectActionRoot + RecordingRoot 숨김 → ResultRoot 표시
5. **3초 후** → MarkOneDone() 호출 → 다음 Step으로 전환

## 데이터 구조

### ActionItem (선택지)
```csharp
[Serializable]
public class ActionItem
{
    public string id;       // DB 저장용 ID (예: "action1", "action2", "action3")
    public string text;     // 대사 텍스트 (예: "워크넷 사이트 둘러보는 장면")
    public Button button;   // 버튼 참조
    public Text label;      // 텍스트 표시용 (text 값이 동적으로 매핑됨)
}
```

### DB 저장 DTO
```csharp
{
    "selectedAction": {
        "id": "action1",
        "text": "워크넷 사이트 둘러보는 장면"
    },
    "recordingDuration": 5.2
}
```

## UI 구성

### 필수 요소

| 구분 | 프로퍼티 | 타입 | 설명 |
|------|----------|------|------|
| 액션 선택 | SelectActionRoot | GameObject | 선택 화면 루트 |
| | ActionChoices | ActionItem[] | 선택지 배열 (3개) |
| 녹음 | RecordingRoot | GameObject | 녹음 화면 루트 |
| | RecordButton | Button | 녹음 시작/종료 버튼 |
| 결과 | ResultRoot | GameObject | 결과 화면 루트 |
| | ResultText | Text | 결과 메시지 표시 |
| 완료 | CompletionGateRef | StepCompletionGate | Step 완료 게이트 |

### 결과 텍스트 형식
```
좋아요! '{선택한 텍스트}'는 정말 훌륭한 첫 장면이에요.
```

## Inspector 설정 예시

```
Director_Problem8_Step3 (Component)
├── ===== 액션 선택 화면 =====
│   ├── Select Action Root: [GameObject]
│   └── Action Choices:
│       ├── [0] id: "action1"
│       │       text: "워크넷 사이트 둘러보는 장면"
│       │       button: [Button]
│       │       label: [Text]
│       ├── [1] id: "action2"
│       │       text: "친구와 이야기하는 장면"
│       │       button: [Button]
│       │       label: [Text]
│       └── [2] id: "action3"
│       │       text: "면접 연습하는 장면"
│       │       button: [Button]
│       │       label: [Text]
├── ===== 녹음 화면 =====
│   ├── Recording Root: [GameObject]
│   └── Record Button: [Button]
├── ===== 결과 화면 =====
│   ├── Result Root: [GameObject]
│   └── Result Text: [Text]
└── ===== 완료 게이트 =====
    └── Completion Gate: [StepCompletionGate]
```

## 확장 포인트 (Virtual Methods)

파생 클래스에서 override하여 시각 효과를 추가할 수 있습니다:

```csharp
// 액션 선택 시 효과
protected virtual void OnActionSelectedVisual(ActionItem selected) { }

// 녹음 시작 시 효과
protected virtual void OnRecordingStarted() { }

// 녹음 종료 시 효과
protected virtual void OnRecordingEnded() { }
```

## 설정 값 (Virtual Config)

```csharp
// 결과 화면 표시 후 자동 완료까지 시간 (기본 3초)
protected virtual float ResultDisplayDuration => 3.0f;
```

## 사용 예시

1. Hierarchy에 Step3 오브젝트 생성
2. `Director_Problem8_Step3` 컴포넌트 추가
3. Inspector에서 UI 요소 연결
4. `ActionChoices` 배열에 선택지 데이터 입력
5. `StepCompletionGate`의 다음 Step 설정
