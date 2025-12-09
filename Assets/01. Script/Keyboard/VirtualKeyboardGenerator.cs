using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가상 키보드 자동 생성기
/// - Play 모드 또는 에디터에서 키보드 UI 자동 생성
/// - QWERTY 레이아웃 + 한글 두벌식
/// </summary>
public class VirtualKeyboardGenerator : MonoBehaviour
{
    [Header("===== 생성 설정 =====")]
    [SerializeField] private RectTransform keyboardContainer;
    [SerializeField] private VirtualKeyboard virtualKeyboard;

    [Header("===== 키 프리팹 =====")]
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private GameObject wideKeyPrefab;  // Shift, Space, Enter 등

    [Header("===== 레이아웃 설정 =====")]
    [SerializeField] private float keyWidth = 80f;
    [SerializeField] private float keyHeight = 80f;
    [SerializeField] private float keySpacing = 8f;
    [SerializeField] private float rowSpacing = 8f;

    [Header("===== 색상 =====")]
    [SerializeField] private Color keyNormalColor = Color.white;
    [SerializeField] private Color keySpecialColor = new Color(0.8f, 0.8f, 0.8f);

    // QWERTY 레이아웃 데이터
    private static readonly string[][] ROWS_EN = {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
        new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l" },
        new[] { "z", "x", "c", "v", "b", "n", "m" }
    };

    private static readonly string[][] ROWS_EN_SHIFT = {
        new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" },
        new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
        new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" },
        new[] { "Z", "X", "C", "V", "B", "N", "M" }
    };

    // 두벌식 한글
    private static readonly string[][] ROWS_KO = {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "ㅂ", "ㅈ", "ㄷ", "ㄱ", "ㅅ", "ㅛ", "ㅕ", "ㅑ", "ㅐ", "ㅔ" },
        new[] { "ㅁ", "ㄴ", "ㅇ", "ㄹ", "ㅎ", "ㅗ", "ㅓ", "ㅏ", "ㅣ" },
        new[] { "ㅋ", "ㅌ", "ㅊ", "ㅍ", "ㅠ", "ㅜ", "ㅡ" }
    };

    private static readonly string[][] ROWS_KO_SHIFT = {
        new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" },
        new[] { "ㅃ", "ㅉ", "ㄸ", "ㄲ", "ㅆ", "ㅛ", "ㅕ", "ㅑ", "ㅒ", "ㅖ" },
        new[] { "ㅁ", "ㄴ", "ㅇ", "ㄹ", "ㅎ", "ㅗ", "ㅓ", "ㅏ", "ㅣ" },
        new[] { "ㅋ", "ㅌ", "ㅊ", "ㅍ", "ㅠ", "ㅜ", "ㅡ" }
    };

    private static readonly VirtualKeyboardKey.KeyType[][] ROW_TYPES = {
        new[] { VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter }
    };

    [ContextMenu("Generate Keyboard")]
    public void GenerateKeyboard()
    {
        if (keyboardContainer == null)
        {
            Debug.LogError("keyboardContainer가 설정되지 않았습니다.");
            return;
        }

        // 기존 키 삭제
        for (int i = keyboardContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(keyboardContainer.GetChild(i).gameObject);
        }

        float totalHeight = (keyHeight + rowSpacing) * 5; // 4행 + 하단 행
        float startY = totalHeight / 2f - keyHeight / 2f;

        // 일반 키 생성
        for (int row = 0; row < ROWS_EN.Length; row++)
        {
            float rowWidth = ROWS_EN[row].Length * (keyWidth + keySpacing) - keySpacing;
            float startX = -rowWidth / 2f + keyWidth / 2f;

            // 행별 오프셋 (QWERTY 스타일)
            float rowOffset = 0f;
            if (row == 2) rowOffset = keyWidth * 0.25f;
            if (row == 3) rowOffset = keyWidth * 0.5f;

            for (int col = 0; col < ROWS_EN[row].Length; col++)
            {
                CreateKey(
                    ROWS_EN[row][col],
                    ROWS_EN_SHIFT[row][col],
                    ROWS_KO[row][col],
                    ROWS_KO_SHIFT[row][col],
                    ROW_TYPES[row][col],
                    new Vector2(startX + col * (keyWidth + keySpacing) + rowOffset, startY - row * (keyHeight + rowSpacing)),
                    new Vector2(keyWidth, keyHeight)
                );
            }
        }

        // 하단 행 (Shift, Space, Backspace, Enter, 한/영)
        float bottomY = startY - 4 * (keyHeight + rowSpacing);
        CreateSpecialKeys(bottomY);

        Debug.Log("키보드 생성 완료!");
    }

    private void CreateKey(string enChar, string enShift, string koChar, string koShift,
        VirtualKeyboardKey.KeyType keyType, Vector2 position, Vector2 size)
    {
        GameObject prefab = keyPrefab;
        if (prefab == null)
        {
            prefab = CreateDefaultKeyPrefab();
        }

        GameObject keyObj = Instantiate(prefab, keyboardContainer);
        keyObj.name = $"Key_{enChar}";

        RectTransform rect = keyObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // VirtualKeyboardKey 설정
        VirtualKeyboardKey keyScript = keyObj.GetComponent<VirtualKeyboardKey>();
        if (keyScript == null)
            keyScript = keyObj.AddComponent<VirtualKeyboardKey>();

        // Reflection으로 private 필드 설정 (에디터에서만)
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(keyScript);
        so.FindProperty("keyType").enumValueIndex = (int)keyType;
        so.FindProperty("englishChar").stringValue = enChar;
        so.FindProperty("englishShiftChar").stringValue = keyType == VirtualKeyboardKey.KeyType.Number ? enShift : "";
        so.FindProperty("koreanChar").stringValue = koChar;
        so.FindProperty("koreanShiftChar").stringValue = koShift != koChar ? koShift : "";
        so.ApplyModifiedProperties();
#endif

        // 라벨 설정
        Text label = keyObj.GetComponentInChildren<Text>();
        if (label != null)
            label.text = enChar;
    }

    private void CreateSpecialKeys(float y)
    {
        float totalWidth = 10 * (keyWidth + keySpacing) - keySpacing;
        float leftX = -totalWidth / 2f;

        // Shift (왼쪽)
        CreateSpecialKey("Shift", leftX + keyWidth * 0.75f, y, keyWidth * 1.5f, "shiftButton");

        // 한/영
        CreateSpecialKey("KO", leftX + keyWidth * 2f + keySpacing, y, keyWidth, "languageButton");

        // Space (중앙)
        CreateSpecialKey("", 0, y, keyWidth * 4f, "spaceButton");

        // Backspace
        CreateSpecialKey("←", totalWidth / 2f - keyWidth * 1.5f, y, keyWidth * 1.2f, "backspaceButton");

        // Enter
        CreateSpecialKey("Enter", totalWidth / 2f - keyWidth * 0.4f, y, keyWidth * 1.5f, "enterButton");
    }

    private void CreateSpecialKey(string label, float x, float y, float width, string fieldName)
    {
        GameObject prefab = wideKeyPrefab ?? keyPrefab;
        if (prefab == null)
            prefab = CreateDefaultKeyPrefab();

        GameObject keyObj = Instantiate(prefab, keyboardContainer);
        keyObj.name = $"Key_{fieldName}";

        RectTransform rect = keyObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, keyHeight);

        // 라벨 설정
        Text textLabel = keyObj.GetComponentInChildren<Text>();
        if (textLabel != null)
            textLabel.text = label;

        // 색상
        Image img = keyObj.GetComponent<Image>();
        if (img != null)
            img.color = keySpecialColor;

        // VirtualKeyboardKey 제거 (특수키는 VirtualKeyboard에서 직접 참조)
        VirtualKeyboardKey keyScript = keyObj.GetComponent<VirtualKeyboardKey>();
        if (keyScript != null)
            DestroyImmediate(keyScript);

#if UNITY_EDITOR
        // VirtualKeyboard에 버튼 연결
        if (virtualKeyboard != null)
        {
            Button btn = keyObj.GetComponent<Button>();
            if (btn != null)
            {
                var so = new UnityEditor.SerializedObject(virtualKeyboard);
                var prop = so.FindProperty(fieldName);
                if (prop != null)
                {
                    prop.objectReferenceValue = btn;
                    so.ApplyModifiedProperties();
                }
            }
        }
#endif
    }

    private GameObject CreateDefaultKeyPrefab()
    {
        GameObject keyObj = new GameObject("Key");

        // Image
        Image img = keyObj.AddComponent<Image>();
        img.color = keyNormalColor;

        // Button
        keyObj.AddComponent<Button>();

        // VirtualKeyboardKey
        keyObj.AddComponent<VirtualKeyboardKey>();

        // Text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(keyObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return keyObj;
    }
}
