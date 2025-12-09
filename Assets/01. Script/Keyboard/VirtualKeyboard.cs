using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 가상 키보드 입력 처리
/// - 한글/영문 전환 지원
/// - Shift (대소문자/특수문자) 지원
/// - VirtualKeyboardController와 함께 사용
/// </summary>
public class VirtualKeyboard : MonoBehaviour
{
    [Header("===== 연결된 InputField =====")]
    [SerializeField] private TMP_InputField targetInputField;

    [Header("===== 키 배열 (자동 연결) =====")]
    [SerializeField] private VirtualKeyboardKey[] keys;

    [Header("===== 특수 키 =====")]
    [SerializeField] private Button shiftButton;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button spaceButton;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button languageButton;

    [Header("===== Shift 버튼 색상 =====")]
    [SerializeField] private Color shiftNormalColor = Color.white;
    [SerializeField] private Color shiftActiveColor = new Color(1f, 0.54f, 0.24f);

    [Header("===== 언어 버튼 라벨 =====")]
    [SerializeField] private Text languageButtonLabel;

    // 상태
    private bool _isShiftActive;
    private bool _isKorean;

    // 한글 조합용
    private int _choIndex = -1;   // 초성 인덱스
    private int _jungIndex = -1;  // 중성 인덱스
    private int _jongIndex = -1;  // 종성 인덱스
    private bool _isComposing;

    // 한글 자모 테이블
    private static readonly char[] CHO = { 'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };
    private static readonly char[] JUNG = { 'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ' };
    private static readonly char[] JONG = { '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };

    public event Action<string> OnTextChanged;
    public event Action OnEnterPressed;

    private void Start()
    {
        InitializeKeys();
        SetupSpecialKeys();
        UpdateKeyLabels();
    }

    #region Public API

    /// <summary>
    /// 대상 InputField 설정
    /// </summary>
    public void SetTargetInputField(TMP_InputField inputField)
    {
        CommitComposition();
        targetInputField = inputField;
        ResetComposition();
    }

    /// <summary>
    /// 현재 텍스트 가져오기
    /// </summary>
    public string GetText()
    {
        return targetInputField != null ? targetInputField.text : string.Empty;
    }

    #endregion

    #region Initialization

    private void InitializeKeys()
    {
        if (keys == null || keys.Length == 0)
            keys = GetComponentsInChildren<VirtualKeyboardKey>(true);

        foreach (var key in keys)
        {
            if (key != null)
                key.OnKeyPressed += OnKeyPressed;
        }
    }

    private void SetupSpecialKeys()
    {
        if (shiftButton != null)
        {
            shiftButton.onClick.RemoveAllListeners();
            shiftButton.onClick.AddListener(OnShiftPressed);
        }

        if (backspaceButton != null)
        {
            backspaceButton.onClick.RemoveAllListeners();
            backspaceButton.onClick.AddListener(OnBackspacePressed);
        }

        if (spaceButton != null)
        {
            spaceButton.onClick.RemoveAllListeners();
            spaceButton.onClick.AddListener(OnSpacePressed);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(OnEnterKeyPressed);
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(OnLanguagePressed);
        }
    }

    #endregion

    #region Key Press Handlers

    private void OnKeyPressed(VirtualKeyboardKey key)
    {
        if (targetInputField == null) return;

        string character = key.GetCurrentChar(_isKorean, _isShiftActive);

        if (_isKorean)
            ProcessKoreanInput(character);
        else
            InsertCharacter(character);

        // Shift는 한 번 누르면 해제 (Caps Lock이 아님)
        if (_isShiftActive)
        {
            _isShiftActive = false;
            UpdateShiftVisual();
            UpdateKeyLabels();
        }
    }

    private void OnShiftPressed()
    {
        _isShiftActive = !_isShiftActive;
        UpdateShiftVisual();
        UpdateKeyLabels();
    }

    private void OnBackspacePressed()
    {
        if (targetInputField == null) return;

        if (_isComposing)
        {
            // 한글 조합 중 백스페이스
            if (_jongIndex >= 0)
            {
                _jongIndex = -1;
                UpdateComposition();
            }
            else if (_jungIndex >= 0)
            {
                _jungIndex = -1;
                UpdateComposition();
            }
            else if (_choIndex >= 0)
            {
                _choIndex = -1;
                _isComposing = false;
                DeleteLastCharacter();
            }
        }
        else
        {
            DeleteLastCharacter();
        }
    }

    private void OnSpacePressed()
    {
        CommitComposition();
        InsertCharacter(" ");
    }

    private void OnEnterKeyPressed()
    {
        CommitComposition();
        OnEnterPressed?.Invoke();
    }

    private void OnLanguagePressed()
    {
        CommitComposition();
        _isKorean = !_isKorean;
        UpdateKeyLabels();
        UpdateLanguageButtonLabel();
    }

    #endregion

    #region Korean Input Processing

    private void ProcessKoreanInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        char c = input[0];

        int choIdx = GetChoIndex(c);
        int jungIdx = GetJungIndex(c);

        if (choIdx >= 0)
        {
            // 초성 입력
            if (!_isComposing)
            {
                // 새 글자 시작
                _choIndex = choIdx;
                _jungIndex = -1;
                _jongIndex = -1;
                _isComposing = true;
                InsertCharacter(CHO[_choIndex].ToString());
            }
            else if (_jungIndex < 0)
            {
                // 초성만 있는 상태에서 또 자음 → 기존 확정, 새 초성
                CommitComposition();
                _choIndex = choIdx;
                _isComposing = true;
                InsertCharacter(CHO[_choIndex].ToString());
            }
            else if (_jongIndex < 0)
            {
                // 초성+중성 → 종성 추가
                int jongIdx = GetJongIndex(c);
                if (jongIdx > 0)
                {
                    _jongIndex = jongIdx;
                    UpdateComposition();
                }
                else
                {
                    CommitComposition();
                    _choIndex = choIdx;
                    _isComposing = true;
                    InsertCharacter(CHO[_choIndex].ToString());
                }
            }
            else
            {
                // 종성까지 있는 상태 → 확정 후 새 글자
                CommitComposition();
                _choIndex = choIdx;
                _isComposing = true;
                InsertCharacter(CHO[_choIndex].ToString());
            }
        }
        else if (jungIdx >= 0)
        {
            // 중성 입력
            if (!_isComposing)
            {
                // 모음만 입력
                InsertCharacter(JUNG[jungIdx].ToString());
            }
            else if (_jungIndex < 0)
            {
                // 초성 + 중성
                _jungIndex = jungIdx;
                UpdateComposition();
            }
            else if (_jongIndex > 0)
            {
                // 종성이 있는 상태에서 모음 → 종성을 다음 글자 초성으로
                char jongChar = JONG[_jongIndex];
                _jongIndex = -1;
                UpdateComposition();
                CommitComposition();

                int newCho = GetChoIndex(jongChar);
                if (newCho >= 0)
                {
                    _choIndex = newCho;
                    _jungIndex = jungIdx;
                    _jongIndex = -1;
                    _isComposing = true;
                    InsertCharacter(ComposeHangul().ToString());
                }
            }
            else
            {
                // 복합 모음 처리 (ㅗ+ㅏ=ㅘ 등)
                int combined = CombineJung(_jungIndex, jungIdx);
                if (combined >= 0)
                {
                    _jungIndex = combined;
                    UpdateComposition();
                }
                else
                {
                    CommitComposition();
                    InsertCharacter(JUNG[jungIdx].ToString());
                }
            }
        }
    }

    private void UpdateComposition()
    {
        if (!_isComposing || _choIndex < 0) return;

        DeleteLastCharacter();

        if (_jungIndex < 0)
        {
            InsertCharacter(CHO[_choIndex].ToString());
        }
        else
        {
            InsertCharacter(ComposeHangul().ToString());
        }
    }

    private void CommitComposition()
    {
        _choIndex = -1;
        _jungIndex = -1;
        _jongIndex = -1;
        _isComposing = false;
    }

    private void ResetComposition()
    {
        _choIndex = -1;
        _jungIndex = -1;
        _jongIndex = -1;
        _isComposing = false;
    }

    private char ComposeHangul()
    {
        if (_choIndex < 0 || _jungIndex < 0)
            return '\0';

        int jong = _jongIndex < 0 ? 0 : _jongIndex;
        int code = 0xAC00 + (_choIndex * 21 * 28) + (_jungIndex * 28) + jong;
        return (char)code;
    }

    #endregion

    #region Helper Methods

    private void InsertCharacter(string c)
    {
        if (targetInputField == null) return;

        targetInputField.text += c;
        OnTextChanged?.Invoke(targetInputField.text);
    }

    private void DeleteLastCharacter()
    {
        if (targetInputField == null) return;

        string text = targetInputField.text;
        if (text.Length > 0)
        {
            targetInputField.text = text.Substring(0, text.Length - 1);
            OnTextChanged?.Invoke(targetInputField.text);
        }
    }

    private void UpdateShiftVisual()
    {
        if (shiftButton == null) return;

        var colors = shiftButton.colors;
        colors.normalColor = _isShiftActive ? shiftActiveColor : shiftNormalColor;
        colors.highlightedColor = colors.normalColor;
        colors.pressedColor = colors.normalColor;
        shiftButton.colors = colors;
    }

    private void UpdateKeyLabels()
    {
        foreach (var key in keys)
        {
            if (key != null)
                key.UpdateLabel(_isKorean, _isShiftActive);
        }
    }

    private void UpdateLanguageButtonLabel()
    {
        if (languageButtonLabel != null)
            languageButtonLabel.text = _isKorean ? "EN" : "KO";
    }

    private int GetChoIndex(char c)
    {
        for (int i = 0; i < CHO.Length; i++)
            if (CHO[i] == c) return i;
        return -1;
    }

    private int GetJungIndex(char c)
    {
        for (int i = 0; i < JUNG.Length; i++)
            if (JUNG[i] == c) return i;
        return -1;
    }

    private int GetJongIndex(char c)
    {
        for (int i = 1; i < JONG.Length; i++)
            if (JONG[i] == c) return i;
        return -1;
    }

    private int CombineJung(int first, int second)
    {
        // ㅗ + ㅏ = ㅘ, ㅗ + ㅐ = ㅙ, ㅗ + ㅣ = ㅚ
        // ㅜ + ㅓ = ㅝ, ㅜ + ㅔ = ㅞ, ㅜ + ㅣ = ㅟ
        // ㅡ + ㅣ = ㅢ
        if (first == 8 && second == 0) return 9;   // ㅗ + ㅏ = ㅘ
        if (first == 8 && second == 1) return 10;  // ㅗ + ㅐ = ㅙ
        if (first == 8 && second == 20) return 11; // ㅗ + ㅣ = ㅚ
        if (first == 13 && second == 4) return 14; // ㅜ + ㅓ = ㅝ
        if (first == 13 && second == 5) return 15; // ㅜ + ㅔ = ㅞ
        if (first == 13 && second == 20) return 16; // ㅜ + ㅣ = ㅟ
        if (first == 18 && second == 20) return 19; // ㅡ + ㅣ = ㅢ
        return -1;
    }

    #endregion
}
