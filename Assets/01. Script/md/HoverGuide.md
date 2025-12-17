# Hover 비활성화 가이드

## 방향성

호버 기능을 비활성화할 때는 **무조건 ButtonHover 스크립트의 `SetInteractable(false)`를 사용**합니다.

EffectController에서 자체적으로 hover 플래그를 관리하지 않습니다.

---

## ButtonHover.cs

위치: `Assets/01. Script/Effect/Common/ButtonHover.cs`

### 주요 메서드

```csharp
// 호버 비활성화
buttonHover.SetInteractable(false);

// 호버 활성화
buttonHover.SetInteractable(true);

// 스케일 리셋
buttonHover.ResetScale();
```

### SetInteractable(false) 동작

1. `_isInteractable = false` 설정
2. `_isHovering = false` 설정
3. 스케일을 원래대로 리셋
4. X 이동 리셋 (enableMoveX가 true인 경우)
5. Outline 비활성화 (있는 경우)

---

## 사용 예시

### Binder에서 클릭 시 호버 비활성화

```csharp
protected override void OnButtonClickedVisual()
{
    base.OnButtonClickedVisual();

    // ButtonHover 비활성화
    if (targetButton != null)
    {
        var buttonHover = targetButton.GetComponent<ButtonHover>();
        if (buttonHover != null)
            buttonHover.SetInteractable(false);
    }

    // 다른 애니메이션 실행
    if (effectController != null)
    {
        effectController.PlayClickAnimation();
    }
}
```

---

## 금지 사항

- EffectController에서 `_hoverDisabled` 같은 플래그를 만들지 않습니다
- EffectController에서 `PlayHover()` / `PlayUnhover()` 메서드를 만들지 않습니다
- 호버 관련 로직은 ButtonHover 스크립트에서만 관리합니다

---

## 관련 스크립트

| 스크립트 | 역할 |
|---------|------|
| ButtonHover.cs | 호버 애니메이션 + 상태 관리 |
| IntroElement.cs | 인트로 등장 애니메이션 (개별 오브젝트) |
| AppearAnimation.cs | 등장 애니메이션 (OnEnable 자동 재생) |
