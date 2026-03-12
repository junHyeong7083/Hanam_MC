using UnityEngine;

/// <summary>
/// Problem5_Step2_EffectController - 문제5 스텝2의 이펙트 관리자 (확장용 껍데기)
///
/// 【역할】 현재 팝업 애니메이션은 PopupImageDisplay 컴포넌트가 전담 처리.
///          추가 시각 효과가 필요할 때 이 클래스에 구현하면 된다.
/// 【사용 위치】 ProblemScene - Problem5 Step2
/// 【트리거】 Logic 클래스에서 ResetAll() 등 호출
/// 【의존성】 EffectControllerBase(상속)
/// </summary>
public class Problem5_Step2_EffectController : EffectControllerBase
{
    // PopupImageDisplay가 모든 팝업 애니메이션을 담당하므로
    // 이 컨트롤러는 추가 이펙트가 필요할 때 사용

    #region Public API

    /// <summary>
    /// 스텝 진입 시 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
    }

    #endregion
}
