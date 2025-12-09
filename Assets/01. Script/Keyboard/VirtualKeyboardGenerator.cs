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

    // 텐키리스 QWERTY 레이아웃 (12키씩)
    private static readonly string[][] ROWS_EN = {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
        new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "[", "]" },
        new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", ";", "'" },
        new[] { "z", "x", "c", "v", "b", "n", "m", ",", ".", "/" }
    };

    private static readonly string[][] ROWS_EN_SHIFT = {
        new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+" },
        new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "{", "}" },
        new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L", ":", "\"" },
        new[] { "Z", "X", "C", "V", "B", "N", "M", "<", ">", "?" }
    };

    // 두벌식 한글 (텐키리스)
    private static readonly string[][] ROWS_KO = {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
        new[] { "ㅂ", "ㅈ", "ㄷ", "ㄱ", "ㅅ", "ㅛ", "ㅕ", "ㅑ", "ㅐ", "ㅔ", "[", "]" },
        new[] { "ㅁ", "ㄴ", "ㅇ", "ㄹ", "ㅎ", "ㅗ", "ㅓ", "ㅏ", "ㅣ", ";", "'" },
        new[] { "ㅋ", "ㅌ", "ㅊ", "ㅍ", "ㅠ", "ㅜ", "ㅡ", ",", ".", "/" }
    };

    private static readonly string[][] ROWS_KO_SHIFT = {
        new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+" },
        new[] { "ㅃ", "ㅉ", "ㄸ", "ㄲ", "ㅆ", "ㅛ", "ㅕ", "ㅑ", "ㅒ", "ㅖ", "{", "}" },
        new[] { "ㅁ", "ㄴ", "ㅇ", "ㄹ", "ㅎ", "ㅗ", "ㅓ", "ㅏ", "ㅣ", ":", "\"" },
        new[] { "ㅋ", "ㅌ", "ㅊ", "ㅍ", "ㅠ", "ㅜ", "ㅡ", "<", ">", "?" }
    };

    private static readonly VirtualKeyboardKey.KeyType[][] ROW_TYPES = {
        new[] { VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Number, VirtualKeyboardKey.KeyType.Symbol, VirtualKeyboardKey.KeyType.Symbol },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Symbol, VirtualKeyboardKey.KeyType.Symbol },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Symbol, VirtualKeyboardKey.KeyType.Symbol },
        new[] { VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Letter, VirtualKeyboardKey.KeyType.Symbol, VirtualKeyboardKey.KeyType.Symbol, VirtualKeyboardKey.KeyType.Symbol }
    };

    // 각 행의 키 개수
    private static readonly int[] ROW_KEY_COUNTS = { 12, 12, 11, 10 };

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

        // 일반 키 생성 (텐키리스 스타일)
        float maxRowWidth = 12 * (keyWidth + keySpacing) - keySpacing; // 최대 행 기준

        for (int row = 0; row < ROWS_EN.Length; row++)
        {
            int keyCount = ROWS_EN[row].Length;
            float rowWidth = keyCount * (keyWidth + keySpacing) - keySpacing;
            float startX = -rowWidth / 2f + keyWidth / 2f;

            for (int col = 0; col < keyCount; col++)
            {
                CreateKey(
                    ROWS_EN[row][col],
                    ROWS_EN_SHIFT[row][col],
                    ROWS_KO[row][col],
                    ROWS_KO_SHIFT[row][col],
                    ROW_TYPES[row][col],
                    new Vector2(startX + col * (keyWidth + keySpacing), startY - row * (keyHeight + rowSpacing)),
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

        // 라벨 찾기
        Text label = keyObj.GetComponentInChildren<Text>();
        if (label != null)
            label.text = enChar;

        // Reflection으로 private 필드 설정 (에디터에서만)
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(keyScript);
        so.FindProperty("keyType").enumValueIndex = (int)keyType;
        so.FindProperty("englishChar").stringValue = enChar;
        so.FindProperty("englishShiftChar").stringValue = keyType != VirtualKeyboardKey.KeyType.Letter ? enShift : "";
        so.FindProperty("koreanChar").stringValue = koChar;
        so.FindProperty("koreanShiftChar").stringValue = koShift != koChar ? koShift : "";
        so.FindProperty("label").objectReferenceValue = label;  // label 참조 설정
        so.ApplyModifiedProperties();
#endif
    }

    private void CreateSpecialKeys(float y)
    {
        // 12키 기준 전체 너비에 맞춤
        float totalWidth = 12 * (keyWidth + keySpacing) - keySpacing;
        float leftX = -totalWidth / 2f;
        float rightX = totalWidth / 2f;

        // 레이아웃: [Shift 1.5] [한/영 1] [Space 5] [← 1.5] [Enter 2] = 11키 + spacing
        float shiftWidth = keyWidth * 1.5f;
        float langWidth = keyWidth;
        float spaceWidth = keyWidth * 5.5f;  // 더 넓은 스페이스바
        float backWidth = keyWidth * 1.5f;
        float enterWidth = keyWidth * 2f;

        float cursor = leftX;

        // Shift
        CreateSpecialKey("Shift", cursor + shiftWidth / 2f, y, shiftWidth, "shiftButton");
        cursor += shiftWidth + keySpacing;

        // 한/영
        CreateSpecialKey("한/영", cursor + langWidth / 2f, y, langWidth, "languageButton");
        cursor += langWidth + keySpacing;

        // Space (중앙에 맞춤)
        CreateSpecialKey("", 0, y, spaceWidth, "spaceButton");

        // 오른쪽에서 시작
        cursor = rightX;

        // Enter (가장 오른쪽)
        CreateSpecialKey("Enter", cursor - enterWidth / 2f, y, enterWidth, "enterButton");
        cursor -= enterWidth + keySpacing;

        // Backspace
        CreateSpecialKey("←", cursor - backWidth / 2f, y, backWidth, "backspaceButton");
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
