using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VirtualKeyboardKey - 가상 키보드의 개별 키 버튼 컴포넌트
///
/// 【역할】 하나의 키 버튼을 나타내며, 영문/한글 및 Shift 상태에 따라
///         적절한 문자를 반환하고 라벨을 갱신한다.
///         - KeyType에 따라 Shift 동작이 다름:
///           Letter → 자동 대문자, Number → 특수문자, Symbol → 변경 없음
///         - 한글 Shift: 쌍자음 등 (ㄱ→ㄲ, ㅂ→ㅃ 등)
/// 【씬】 VirtualKeyboard가 사용되는 모든 씬
/// 【참조하는 곳】 VirtualKeyboard (키 클릭 이벤트 수신, 라벨 업데이트 호출)
/// 【참조되는 곳】 VirtualKeyboardGenerator (키 생성 시 설정)
/// 【흐름】 클릭 → OnKeyPressed 이벤트 발행 → VirtualKeyboard.OnKeyPressed() 에서 처리
/// </summary>
[RequireComponent(typeof(Button))]
public class VirtualKeyboardKey : MonoBehaviour
{
    /// <summary>키 타입 열거형 - Shift 동작 방식을 결정</summary>
    public enum KeyType
    {
        Letter,     // 알파벳 (Shift 시 자동 대문자: a → A)
        Number,     // 숫자 (Shift 시 특수문자: 1 → !)
        Symbol      // 고정 특수문자 (Shift 무관: @, ., - 등)
    }

    [Header("===== 키 타입 =====")]
    [SerializeField] private KeyType keyType = KeyType.Letter;   // 이 키의 타입

    [Header("===== 영문 설정 =====")]
    [SerializeField] private string englishChar = "a";            // 영문 기본 문자
    [Tooltip("Shift 시 표시할 문자 (Letter: 자동 대문자, Number/Symbol: 여기 입력)")]
    [SerializeField] private string englishShiftChar = "";        // 영문 Shift 문자 (Letter는 비워두면 자동 대문자)

    [Header("===== 한글 설정 =====")]
    [SerializeField] private string koreanChar = "ㅁ";            // 한글 기본 문자 (자모)
    [Tooltip("Shift 시 표시할 한글 (쌍자음 등)")]
    [SerializeField] private string koreanShiftChar = "";         // 한글 Shift 문자 (비워두면 기본 문자 유지)

    [Header("===== UI =====")]
    [SerializeField] private Text label;                          // 키 메인 라벨 텍스트
    [Tooltip("Shift 문자를 작게 표시할 보조 라벨 (선택)")]
    [SerializeField] private Text subLabel;                       // 키 보조 라벨 텍스트 (Shift 미리보기)

    private Button _button;  // 이 키의 Button 컴포넌트

    /// <summary>영문 기본 문자</summary>
    public string EnglishChar => englishChar;
    /// <summary>한글 기본 문자</summary>
    public string KoreanChar => koreanChar;
    /// <summary>영문 Shift 문자 (Letter 타입이면 자동 대문자)</summary>
    public string EnglishShiftChar => GetEnglishShiftChar();
    /// <summary>한글 Shift 문자 (비어있으면 기본 문자 반환)</summary>
    public string KoreanShiftChar => string.IsNullOrEmpty(koreanShiftChar) ? koreanChar : koreanShiftChar;

    /// <summary>키 클릭 시 발행되는 이벤트. 매개변수: 클릭된 키 자신</summary>
    public event Action<VirtualKeyboardKey> OnKeyPressed;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }

        if (label == null)
            label = GetComponentInChildren<Text>();
    }

    private void OnClick()
    {
        OnKeyPressed?.Invoke(this);
    }

    /// <summary>
    /// 현재 상태에 맞는 문자 반환
    /// </summary>
    public string GetCurrentChar(bool isKorean, bool isShift)
    {
        if (isKorean)
        {
            return isShift ? KoreanShiftChar : koreanChar;
        }
        else
        {
            return isShift ? EnglishShiftChar : englishChar;
        }
    }

    /// <summary>
    /// 라벨 업데이트 (언어/Shift 상태에 따라)
    /// </summary>
    public void UpdateLabel(bool isKorean, bool isShift)
    {
        if (label == null) return;

        string mainChar;
        string shiftChar;

        if (isKorean)
        {
            mainChar = koreanChar;
            shiftChar = koreanShiftChar;
        }
        else
        {
            mainChar = englishChar;
            shiftChar = GetEnglishShiftChar();
        }

        // 메인 라벨
        label.text = isShift ? shiftChar : mainChar;

        // 보조 라벨 (Shift 문자 미리보기)
        if (subLabel != null)
        {
            if (!string.IsNullOrEmpty(shiftChar) && shiftChar != mainChar.ToUpper())
            {
                subLabel.text = isShift ? mainChar : shiftChar;
                subLabel.gameObject.SetActive(true);
            }
            else
            {
                subLabel.gameObject.SetActive(false);
            }
        }
    }

    private string GetEnglishShiftChar()
    {
        if (!string.IsNullOrEmpty(englishShiftChar))
            return englishShiftChar;

        // Letter 타입은 자동 대문자
        if (keyType == KeyType.Letter)
            return englishChar.ToUpper();

        return englishChar;
    }
}
