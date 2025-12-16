using UnityEngine;

/// <summary>
/// Problem5 Step2: Effect Controller
/// - 팝업 애니메이션은 PopupImageDisplay가 담당
/// - 추가 이펙트 필요 시 여기에 구현
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
