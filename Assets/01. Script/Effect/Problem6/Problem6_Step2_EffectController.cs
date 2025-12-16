using UnityEngine;

/// <summary>
/// Part 6 - Step 2 이펙트 컨트롤러
/// - 인트로 연출 (EffectControllerBase에서 상속)
/// - 카드 호버/선택은 별도 Hover 스크립트에서 처리
/// </summary>
public class Problem6_Step2_EffectController : EffectControllerBase
{
    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        ResetIntroElements();
    }
}
