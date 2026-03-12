using UnityEditor;
using UnityEngine;

/// <summary>
/// IntroElementEditor - IntroElement 컴포넌트의 커스텀 에디터
///
/// 【역할】 IntroElement의 FlyIn 애니메이션 경로를 Scene 뷰에서 시각적으로 편집할 수 있게 한다.
///         - enableFlyIn이 활성화되면 Scene 뷰에 Catmull-Rom 스플라인 경로를 노란색 곡선으로 그린다
///         - 빨간 핸들(시작점), 파란 핸들(경유점)을 드래그하여 경로를 조정할 수 있다
///         - 초록 점(도착지)은 현재 오브젝트 위치로 읽기 전용
///         - 인스펙터에 사용 안내 HelpBox를 표시한다
/// 【씬】 에디터 전용 (런타임 미포함)
/// 【참조하는 곳】 IntroElement 컴포넌트를 선택했을 때 자동 활성화
/// 【참조되는 곳】 IntroElement (대상 컴포넌트)
/// 【흐름】 IntroElement 선택 → OnInspectorGUI (HelpBox) + OnSceneGUI (경로 시각화 + 핸들 드래그)
/// </summary>
[CustomEditor(typeof(IntroElement))]
public class IntroElementEditor : Editor
{
    private SerializedProperty _enableFlyIn;       // enableFlyIn 직렬화 프로퍼티
    private SerializedProperty _flyStartOffset;    // flyStartOffset 직렬화 프로퍼티 (시작점 오프셋)
    private SerializedProperty _flyWaypoints;      // flyWaypoints 직렬화 프로퍼티 (경유점 배열)

    private void OnEnable()
    {
        _enableFlyIn    = serializedObject.FindProperty("enableFlyIn");
        _flyStartOffset = serializedObject.FindProperty("flyStartOffset");
        _flyWaypoints   = serializedObject.FindProperty("flyWaypoints");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (_enableFlyIn != null && _enableFlyIn.boolValue)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Scene 뷰에서\n" +
                "  ● 빨강(시작점) / 파랑(경유점) 핸들을 드래그해 경로 조정\n" +
                "  ● 초록점 = 도착지 (현재 오브젝트 위치)\n\n" +
                "※ Screen Space Overlay Canvas에서는 Scene 뷰 좌표가 게임 화면과\n" +
                "  다르게 보일 수 있습니다. Play 모드로 최종 확인하세요.",
                MessageType.Info);
        }
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();

        if (_enableFlyIn == null || !_enableFlyIn.boolValue) return;
        if (_flyStartOffset == null || _flyWaypoints == null) return;

        var element = (IntroElement)target;
        var rt = element.GetComponent<RectTransform>();
        if (rt == null || rt.parent == null) return;

        float z = rt.localPosition.z;

        Vector3 ToWorld(Vector2 anchored)
            => rt.parent.TransformPoint(new Vector3(anchored.x, anchored.y, z));

        Vector2 FromWorld(Vector3 world)
        {
            Vector3 local = rt.parent.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        Vector2 basePos  = rt.anchoredPosition;
        Vector2 startOff = _flyStartOffset.vector2Value;

        // 경로 점 수집: start → waypoints → end
        int wpCount = _flyWaypoints.arraySize;
        int pCount  = 2 + wpCount;
        Vector2[] pts = new Vector2[pCount];
        pts[0] = basePos + startOff;
        for (int i = 0; i < wpCount; i++)
            pts[1 + i] = basePos + _flyWaypoints.GetArrayElementAtIndex(i).vector2Value;
        pts[pCount - 1] = basePos;

        Vector3 camFwd = Camera.current != null
            ? Camera.current.transform.forward
            : Vector3.forward;

        float discSize(Vector3 pos) => HandleUtility.GetHandleSize(pos) * 0.08f;

        // ── Catmull-Rom 곡선 (노랑) ─────────────────────────────────
        Handles.color = Color.yellow;
        const int SegsPerSegment = 20;
        int segCount = pCount - 1;
        if (segCount > 0)
        {
            Vector3 prev = ToWorld(pts[0]);
            int totalSegs = segCount * SegsPerSegment;
            for (int i = 1; i <= totalSegs; i++)
            {
                float t = (float)i / totalSegs;
                Vector2 p = EvalCatmullRom(pts, t);
                Vector3 curr = ToWorld(p);
                Handles.DrawLine(prev, curr);
                prev = curr;
            }
        }

        // ── 점선 (인접 점 연결) ─────────────────────────────────────
        Handles.color = new Color(1f, 1f, 0f, 0.35f);
        for (int i = 0; i < pCount - 1; i++)
            Handles.DrawDottedLine(ToWorld(pts[i]), ToWorld(pts[i + 1]), 5f);

        // ── 끝점 (초록, 읽기 전용) ──────────────────────────────────
        Vector3 endW = ToWorld(basePos);
        Handles.color = Color.green;
        Handles.DrawSolidDisc(endW, camFwd, discSize(endW));
        Handles.Label(endW + Vector3.up * HandleUtility.GetHandleSize(endW) * 0.15f,
            "도착", EditorStyles.boldLabel);

        // ── 시작점 핸들 (빨강) ──────────────────────────────────────
        Vector3 startW = ToWorld(pts[0]);
        Handles.color = Color.red;
        Handles.DrawSolidDisc(startW, camFwd, discSize(startW));

        EditorGUI.BeginChangeCheck();
        Vector3 newStartW = Handles.PositionHandle(startW, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(element, "IntroElement: Move FlyIn Start");
            newStartW.z = startW.z;
            _flyStartOffset.vector2Value = FromWorld(newStartW) - basePos;
            serializedObject.ApplyModifiedProperties();
        }

        Handles.Label(startW + Vector3.up * HandleUtility.GetHandleSize(startW) * 0.15f,
            "시작", EditorStyles.boldLabel);

        // ── 경유점 핸들들 (파랑) ────────────────────────────────────
        for (int i = 0; i < wpCount; i++)
        {
            var wpProp = _flyWaypoints.GetArrayElementAtIndex(i);
            Vector2 wpAbs = basePos + wpProp.vector2Value;
            Vector3 wpW = ToWorld(wpAbs);

            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(wpW, camFwd, discSize(wpW));

            EditorGUI.BeginChangeCheck();
            Vector3 newWpW = Handles.PositionHandle(wpW, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(element, $"IntroElement: Move Waypoint {i}");
                newWpW.z = wpW.z;
                wpProp.vector2Value = FromWorld(newWpW) - basePos;
                serializedObject.ApplyModifiedProperties();
            }

            Handles.Label(wpW + Vector3.up * HandleUtility.GetHandleSize(wpW) * 0.15f,
                $"경유점 {i}", EditorStyles.boldLabel);
        }
    }

    /// <summary>
    /// Catmull-Rom 스플라인 보간 (IntroElement 런타임과 동일)
    /// </summary>
    private static Vector2 EvalCatmullRom(Vector2[] pts, float t)
    {
        int segCount = pts.Length - 1;
        if (segCount <= 0) return pts[0];

        float scaled = t * segCount;
        int seg = Mathf.Min((int)scaled, segCount - 1);
        float lt = scaled - seg;

        Vector2 p0 = pts[Mathf.Max(seg - 1, 0)];
        Vector2 p1 = pts[seg];
        Vector2 p2 = pts[Mathf.Min(seg + 1, pts.Length - 1)];
        Vector2 p3 = pts[Mathf.Min(seg + 2, pts.Length - 1)];

        float lt2 = lt * lt;
        float lt3 = lt2 * lt;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * lt +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * lt2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * lt3
        );
    }
}
