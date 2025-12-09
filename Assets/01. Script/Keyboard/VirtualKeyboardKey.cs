using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가상 키보드의 개별 키 버튼
/// - 영문/한글 문자 설정
/// - Shift 상태에 따른 대문자/특수문자 지원
/// </summary>
[RequireComponent(typeof(Button))]
public class VirtualKeyboardKey : MonoBehaviour
{
    public enum KeyType
    {
        Letter,     // 알파벳 (a → A)
        Number,     // 숫자+특수문자 (1 → !)
        Symbol      // 고정 특수문자 (@, ., - 등)
    }

    [Header("===== 키 타입 =====")]
    [SerializeField] private KeyType keyType = KeyType.Letter;

    [Header("===== 영문 설정 =====")]
    [SerializeField] private string englishChar = "a";
    [Tooltip("Shift 시 표시할 문자 (Letter: 자동 대문자, Number/Symbol: 여기 입력)")]
    [SerializeField] private string englishShiftChar = "";

    [Header("===== 한글 설정 =====")]
    [SerializeField] private string koreanChar = "ㅁ";
    [Tooltip("Shift 시 표시할 한글 (쌍자음 등)")]
    [SerializeField] private string koreanShiftChar = "";

    [Header("===== UI =====")]
    [SerializeField] private Text label;
    [Tooltip("Shift 문자를 작게 표시할 보조 라벨 (선택)")]
    [SerializeField] private Text subLabel;

    private Button _button;

    public string EnglishChar => englishChar;
    public string KoreanChar => koreanChar;
    public string EnglishShiftChar => GetEnglishShiftChar();
    public string KoreanShiftChar => string.IsNullOrEmpty(koreanShiftChar) ? koreanChar : koreanShiftChar;

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
