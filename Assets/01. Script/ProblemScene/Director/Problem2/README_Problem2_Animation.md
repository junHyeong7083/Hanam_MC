# Problem 2 애니메이션 설정 가이드

## 개요
Problem 2 (NG 장면 분석)의 각 Step별 애니메이션 스크립트 부착 위치 안내

---

## Step 1: 마음 렌즈 드롭

### 기존 스크립트 (이미 구현됨)
- `Director_Problem2_Step1.cs` - 좌/우 슬라이드 인트로 애니메이션 포함
  - `leftEnterRoot`: 왼쪽에서 등장
  - `rightEnterRoot`: 오른쪽에서 등장

### 추가할 애니메이션 스크립트

#### 1. 마음 렌즈 (드래그 아이템)
**오브젝트:** `dragItems[0]` (마음 렌즈)

| 스크립트 | 부착 위치 | 설정값 |
|---------|----------|--------|
| `DragScaleFeedback.cs` | 마음 렌즈 루트 | draggingScale=0.9, hoverScale=1.05 |

**연동 방법:**
```csharp
// Director_Problem2_DragItem.cs의 OnBeginDrag()에 추가
var feedback = GetComponent<DragScaleFeedback>();
if (feedback != null) feedback.OnDragBegin();

// OnEndDrag()에 추가
if (feedback != null) feedback.OnDragEnd();
```

#### 2. 스파클 효과 (마음 렌즈 위)
**오브젝트:** 마음 렌즈의 자식으로 3개 생성

```
마음 렌즈 (dragItems[0])
├── itemImage
│   ├── Sparkle1 (Image + SparkleEffect)
│   ├── Sparkle2 (Image + SparkleEffect)
│   └── Sparkle3 (Image + SparkleEffect)
└── ghostImage
```

| 오브젝트 | delay 설정 |
|---------|-----------|
| Sparkle1 | 0.0 |
| Sparkle2 | 0.3 |
| Sparkle3 | 0.6 |

**공통 설정:**
- duration: 2
- minScale: 0, maxScale: 1
- startRotation: 0, endRotation: 360
- Sprite: ✨ 또는 Sparkles 아이콘

#### 3. 드롭 영역 표시
**오브젝트:** `dropBoxArea` 하위

```
dropBoxArea (UIDropBoxArea)
├── [기존 UI들]
└── DropIndicator (새로 추가)
    └── DropZoneIndicator.cs
        ├── borderRoot → DashedBorder (Image)
        └── glowRoot → Glow (Image, optional)
```

**설정:**
- style: Border (또는 Both)
- borderColor: #FF8A3D
- animateBorder: true

**연동 방법:**
```csharp
// UIDropBoxArea.cs 또는 Director_Problem2_Step1_Logic.cs에서
var indicator = GetComponentInChildren<DropZoneIndicator>();

// NotifyDragBegin()에서
if (indicator != null) indicator.Show();

// NotifyDragEnd()에서
if (indicator != null) indicator.Hide();
```

#### 4. 안내 텍스트 펄스
**오브젝트:** "마음 렌즈를 필름 위로 드래그하세요" 텍스트

| 스크립트 | 설정 |
|---------|------|
| `TextOpacityPulse.cs` | minOpacity=0.5, maxOpacity=1, duration=2 |

#### 5. NG 뱃지 펄스 (드롭 성공 시)
**오브젝트:** `resultPanelRoot` 내부 NG 뱃지

| 스크립트 | 설정 |
|---------|------|
| `PulseAnimation.cs` | pulseType=Scale, minScale=1, maxScale=1.2, playOnEnable=false |

**연동:**
```csharp
// 드롭 성공 시
var pulse = ngBadge.GetComponent<PulseAnimation>();
if (pulse != null) pulse.PlayTimes(3); // 3번만 펄스
```

#### 6. CTA 버튼 등장
**오브젝트:** `completionGate.completeRoot` 내부 버튼

| 스크립트 | 설정 |
|---------|------|
| `SlideUpFadeIn.cs` | startOffsetY=50, duration=0.5, delay=0.2 |

---

## Step 2: 감정 조명 연결

### 기존 애니메이션 (이미 구현됨)
- 라인 그리기 애니메이션: `UILineConnector`
- 조명 등장 애니메이션: `PlayLightAppear()` 내장

### 추가할 애니메이션

#### 1. 조명 글로우 펄스
**오브젝트:** 각 `EmotionLightSlot.lightGlowImage`

| 스크립트 | 설정 |
|---------|------|
| `PulseAnimation.cs` | pulseType=Both, minScale=1, maxScale=1.3, minAlpha=0.3, maxAlpha=0.6, loop=true |

---

## Step 3: 재촬영

### 기존 애니메이션 (이미 구현됨)
- 카드 플립: `UICardFlip`
- 마이크 인디케이터: `MicRecordingIndicator`

### 추가할 애니메이션

#### 1. 관점 버튼 선택 피드백
**오브젝트:** 각 `perspectiveButtons[]`

| 스크립트 | 설정 |
|---------|------|
| `PulseAnimation.cs` | 선택 시 1회 펄스, PlayTimes(1) |

---

## 폴더 구조

```
Assets/01. Script/
├── Effect/
│   ├── Common/                    ← 공용 애니메이션
│   │   ├── PulseAnimation.cs
│   │   ├── SlideUpFadeIn.cs
│   │   ├── TextOpacityPulse.cs
│   │   ├── SparkleEffect.cs
│   │   ├── DragScaleFeedback.cs
│   │   └── DropZoneIndicator.cs
│   └── HomeScene/
│       └── LevelSelectPanelAnimator.cs
│
└── ProblemScene/
    └── Director/
        └── Problem2/
            ├── README_Problem2_Animation.md  ← 이 문서
            ├── Director_Problem2_Step1.cs
            ├── Director_Problem2_DragItem.cs
            └── ...
```

---

## 체크리스트

### Step 1
- [ ] 마음 렌즈에 `DragScaleFeedback.cs` 부착
- [ ] 마음 렌즈 하위에 Sparkle 3개 생성 + `SparkleEffect.cs`
- [ ] dropBoxArea 하위에 DropIndicator 생성 + `DropZoneIndicator.cs`
- [ ] 안내 텍스트에 `TextOpacityPulse.cs` 부착
- [ ] NG 뱃지에 `PulseAnimation.cs` 부착
- [ ] CTA 버튼에 `SlideUpFadeIn.cs` 부착
- [ ] `Director_Problem2_DragItem.cs`에 DragScaleFeedback 연동 코드 추가

### Step 2
- [ ] 각 lightGlowImage에 `PulseAnimation.cs` 부착

### Step 3
- [ ] (선택) perspectiveButtons에 선택 피드백 추가
