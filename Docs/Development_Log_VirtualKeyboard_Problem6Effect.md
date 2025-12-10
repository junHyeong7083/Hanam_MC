# 개발 로그: 가상 키보드 & Problem6 이펙트

## 1. 가상 키보드 시스템

### 1.1 개요
- **목적**: 키오스크 환경(물리 키보드 없음)에서 사용할 가상 키보드
- **기능**: 한글/영문 입력, Shift 3단계 상태, InputField 자동 감지

### 1.2 주요 파일
| 파일 | 설명 |
|------|------|
| `Assets/01. Script/Keyboard/VirtualKeyboard.cs` | 키보드 입력 처리 (한글 조합, Shift 상태) |
| `Assets/01. Script/Keyboard/VirtualKeyboardController.cs` | 키보드 표시/숨김 관리 |
| `Assets/01. Script/Keyboard/VirtualKeyboardGenerator.cs` | Editor에서 키보드 UI 생성 |
| `Assets/01. Script/Keyboard/VirtualKeyboardKey.cs` | 개별 키 데이터 |

### 1.3 Shift 3단계 상태
```
상태 0: 소문자/기본 특수문자 (기본)
상태 1: 대문자/Shift 특수문자 (1회 입력 후 상태 0으로 복귀)
상태 2: 대문자/Shift 특수문자 고정 (빨간색 표시, 다시 누르면 상태 0)
```

### 1.4 해결한 이슈들

#### Shift 특수문자 미작동
- **문제**: Shift 누르면 특수문자가 변경되지 않음
- **원인**: Generator에서 Symbol 타입 키에 `englishShiftChar` 미설정
- **해결**: `keyType != KeyType.Letter` 조건으로 변경

#### 특수 키 NULL 오류
- **문제**: Shift, Backspace 등 특수 키를 찾지 못함
- **해결**: 이름 기반 자동 탐색 (`Key_shiftButton` 등)

#### Shift 색상 즉시 반영 안됨
- **문제**: 상태 2 진입 시 빨간색이 바로 안 보임
- **해결**: `image.color = targetColor` 직접 할당 추가

#### InputField 캐럿 깜빡임 사라짐
- **문제**: 가상 키보드 사용 시 캐럿이 안 보임
- **해결**: `RefocusNextFrame` 코루틴으로 다음 프레임에 재포커스
```csharp
private System.Collections.IEnumerator RefocusNextFrame()
{
    yield return null;
    if (targetInputField != null)
    {
        targetInputField.ActivateInputField();
        targetInputField.caretPosition = targetInputField.text.Length;
        targetInputField.selectionAnchorPosition = targetInputField.text.Length;
        targetInputField.selectionFocusPosition = targetInputField.text.Length;
    }
}
```

#### 텍스트 전체 선택됨
- **문제**: 캐럿 대신 텍스트가 선택됨
- **해결**: `selectionAnchorPosition`, `selectionFocusPosition`도 함께 설정

#### 키보드 밖 클릭 시 숨김
- **구현**: `CheckOutsideClick()` 메서드
- InputField 클릭 시에는 숨기지 않음
- Enter 키 또는 외부 클릭 시 숨김

---

## 2. Problem6 Step1 이펙트 컨트롤러

### 2.1 개요
- **목적**: Part 6 - Step 1 인트로 이펙트 (빈 공간 → 의자 배치 전환)
- **패턴**: Update 기반 상태 머신 (기존 프로젝트 패턴 준수)

### 2.2 파일 위치
```
Assets/01. Script/Effect/Problem6/Problem6_Step1_EffectController.cs
```

### 2.3 애니메이션 단계 (AnimPhase)
```
Idle → EmptyFadeOut → ChairAppear → LightingFlicker → Idle
```

| 단계 | 설명 |
|------|------|
| EmptyFadeOut | 빈 공간 아이콘 페이드 아웃 (0.2초) |
| ChairAppear | 의자 활성화 + AppearAnimation 재생 대기 (0.6초) |
| LightingFlicker | 조명 깜빡임 효과 (3초) |

### 2.4 Inspector 연결
```
Director 필드              →  EffectController 필드
─────────────────────────────────────────────────
emptyIconRoot             →  emptyIconRoot
chairPlacedIconRoot       →  chairPlacedIconRoot (AppearAnimation 부착)
(조명 이미지)              →  lightingOverlay
```

### 2.5 Common 스크립트 활용
- `AppearAnimation`: chairPlacedIconRoot에 부착 → SetActive(true) 시 자동 재생
- `SparkleEffect`: 스파클 자식 오브젝트에 부착

### 2.6 사용법
```csharp
// Director_Part6_Step1에서 드롭 성공 시:
effectController.PlayActivateEffect(() => {
    // 완료 후 콜백
});

// 초기 상태로 리셋:
effectController.ResetToInitial();
```

---

## 3. 프로젝트 패턴 참고

### 3.1 이펙트 컨트롤러 패턴
- Update 기반 상태 머신 사용
- `AnimPhase` enum으로 단계 관리
- `_elapsed`, `_isAnimating` 상태 변수
- Common 스크립트 재사용 (SparkleEffect, AppearAnimation, DropZoneIndicator)

### 3.2 참고 파일
- `Assets/01. Script/Effect/Problem5/Problem5_Step1_EffectController.cs`
- `Assets/01. Script/Effect/Problem5/Problem5_Step2_EffectController.cs`
- `Assets/01. Script/Effect/Common/` (공통 이펙트 스크립트)

---

## 4. 작업 일자
- **날짜**: 2025-12-09
- **환경**: Unity, C#
