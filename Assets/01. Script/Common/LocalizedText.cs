using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Text 컴포넌트에 textId 기반 로컬라이즈 텍스트를 자동 세팅
/// - 버튼 라벨, 캐릭터 네이밍 박스 등 고정 텍스트용
/// - OnEnable 시 ProblemRuntime.L(textId) 로 텍스트 설정
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private int textId;

    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        if (_text != null && textId != 0)
            _text.text = ProblemRuntime.L(textId);
    }
}
