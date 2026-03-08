using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SweepHighlightTrigger 커스텀 에디터
/// - 에디터 모드에서 "▶ 스윕 테스트" 버튼으로 Play 없이 효과 미리보기
/// </summary>
[CustomEditor(typeof(SweepHighlightTrigger))]
public class SweepHighlightTriggerEditor : Editor
{
    private static readonly int PropSweepT     = Shader.PropertyToID("_SweepT");
    private static readonly int PropSweepColor = Shader.PropertyToID("_SweepColor");
    private static readonly int PropSweepAngle = Shader.PropertyToID("_SweepAngle");

    private static bool     _isPreviewing;
    private static double   _startTime;
    private static float    _previewDuration;
    private static Material _previewMat;   // 직접 수정할 머티리얼 (원본)

    // 프리뷰 종료 시 복원용
    private static Color _origSweepColor;
    private static float _origSweepAngle;

    private SerializedProperty _enableSweep;
    private SerializedProperty _sweepDuration;
    private SerializedProperty _sweepDelay;

    private void OnEnable()
    {
        _enableSweep   = serializedObject.FindProperty("enableSweep");
        _sweepDuration = serializedObject.FindProperty("sweepDuration");
        _sweepDelay    = serializedObject.FindProperty("sweepDelay");
    }

    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("── 에디터 미리보기 ──", EditorStyles.boldLabel);

        var trigger = (SweepHighlightTrigger)target;
        var image   = trigger.GetComponent<Image>();

        if (image == null)
        {
            EditorGUILayout.HelpBox("Image 컴포넌트가 없습니다.", MessageType.Warning);
            return;
        }
        if (image.material == null || !image.material.HasProperty(PropSweepT))
        {
            EditorGUILayout.HelpBox(
                "UI/SweepHighlight 쉐이더 Material 이 Image 에 할당되지 않았습니다.",
                MessageType.Warning);
            return;
        }

        if (_isPreviewing)
        {
            float elapsed = (float)(EditorApplication.timeSinceStartup - _startTime);
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _previewDuration));

            // 진행 바
            EditorGUILayout.Space(2);
            Rect r = EditorGUILayout.GetControlRect(false, 6);
            EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * t, r.height), new Color(0.3f, 0.8f, 1f));
            EditorGUILayout.Space(2);

            if (GUILayout.Button("■ 중지", GUILayout.Height(28)))
                StopPreview();
        }
        else
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.6f);
            if (GUILayout.Button("▶  스윕 테스트 (Play 없이 미리보기)", GUILayout.Height(32)))
                StartPreview(image.material, _sweepDuration.floatValue, trigger);
            GUI.backgroundColor = Color.white;
        }
    }

    // ── 미리보기 시작 ─────────────────────────────────────────────────
    private static void StartPreview(Material mat, float duration, SweepHighlightTrigger trigger)
    {
        StopPreview();

        _previewMat      = mat;
        _previewDuration = Mathf.Max(0.01f, duration);
        _startTime       = EditorApplication.timeSinceStartup;
        _isPreviewing    = true;

        // 트리거의 sweepColor / sweepAngle 을 원본 머티리얼에 임시 적용
        if (_previewMat != null && trigger != null)
        {
            var so    = new SerializedObject(trigger);
            var color = so.FindProperty("sweepColor").colorValue;
            var angle = so.FindProperty("sweepAngle").floatValue;

            // 원래 값 백업
            _origSweepColor = _previewMat.GetColor(PropSweepColor);
            _origSweepAngle = _previewMat.GetFloat(PropSweepAngle);

            _previewMat.SetColor(PropSweepColor, color);
            _previewMat.SetFloat(PropSweepAngle, angle);
            EditorUtility.SetDirty(_previewMat);
        }

        EditorApplication.update += OnEditorUpdate;
    }

    // ── 미리보기 종료 ─────────────────────────────────────────────────
    private static void StopPreview()
    {
        EditorApplication.update -= OnEditorUpdate;

        if (_previewMat != null)
        {
            _previewMat.SetFloat(PropSweepT, 0f);
            // 임시로 덮어쓴 color / angle 복원
            _previewMat.SetColor(PropSweepColor, _origSweepColor);
            _previewMat.SetFloat(PropSweepAngle, _origSweepAngle);
            EditorUtility.SetDirty(_previewMat);
        }

        _previewMat   = null;
        _isPreviewing = false;

        RepaintAll();
    }

    // ── 에디터 업데이트 ───────────────────────────────────────────────
    private static void OnEditorUpdate()
    {
        if (!_isPreviewing || _previewMat == null)
        {
            StopPreview();
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - _startTime;
        float t      = Mathf.Clamp01((float)(elapsed / _previewDuration));
        float eased  = Mathf.SmoothStep(0f, 1f, t);
        float sweepT = Mathf.Lerp(0.25f, 1f, eased);

        _previewMat.SetFloat(PropSweepT, sweepT);
        EditorUtility.SetDirty(_previewMat);  // 머티리얼 변경 즉시 반영

        RepaintAll();

        if (t >= 1f)
            StopPreview();
    }

    private static void RepaintAll()
    {
        // Canvas 메시 강제 재구성 (UI 반영)
        Canvas.ForceUpdateCanvases();

        // Game View 강제 리페인트 (Edit Mode에서 자동 갱신 안 됨)
        var assembly     = typeof(EditorWindow).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
        {
            var gv = EditorWindow.GetWindow(gameViewType, false, null, false);
            gv?.Repaint();
        }

        // Scene View + 플레이어 루프 업데이트
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }
}
