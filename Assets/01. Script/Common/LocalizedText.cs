using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LocalizedText - Text 컴포넌트에 textId 기반 로컬라이즈 텍스트를 자동 세팅하는 컴포넌트
///
/// 【역할】 인스펙터에서 설정한 textId를 기반으로 CSV DataTable에서 해당 텍스트를 가져와
///          부착된 Text 컴포넌트에 자동으로 설정한다.
///          버튼 라벨, 캐릭터 네이밍 박스 등 고정 텍스트에 사용된다.
///
/// 【참조하는 곳】 씬 내 고정 텍스트가 필요한 UI 오브젝트에 부착 (버튼, 헤더 등)
/// 【참조되는 곳】 ProblemRuntime.L(textId) — CSV에서 텍스트 로드
///
/// 【흐름】
///   1. Awake()에서 Text 컴포넌트 캐싱
///   2. OnEnable() 시 ProblemRuntime.L(textId)로 텍스트 설정
///   3. textId가 0이면 아무 동작 안 함 (미설정 상태)
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    /// <summary>CSV DataTable에서 가져올 텍스트의 고유 ID (인스펙터에서 설정)</summary>
    [SerializeField] private int textId;

    /// <summary>캐싱된 Text 컴포넌트 참조</summary>
    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    /// <summary>
    /// 활성화 시 textId에 해당하는 로컬라이즈 텍스트를 Text 컴포넌트에 설정한다.
    /// textId가 0이면 미설정 상태이므로 아무 동작 안 함.
    /// </summary>
    private void OnEnable()
    {
        if (_text != null && textId != 0)
            _text.text = ProblemRuntime.L(textId);
    }
}
