using UnityEngine;

/// <summary>
/// Problem6_Step2_EffectController - 문제6 스텝2의 이펙트 관리자 (인트로 전용)
///
/// 【역할】 EffectControllerBase의 인트로 연출만 사용. 카드 호버/선택은 별도 ButtonHover 등에서 처리.
/// 【사용 위치】 ProblemScene - Problem6 Step2
/// 【트리거】 OnEnable에서 인트로 자동 재생
/// 【의존성】 EffectControllerBase(상속)
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
