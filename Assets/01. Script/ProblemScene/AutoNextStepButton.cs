using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AutoNextStepButton - 자동으로 StepFlowController.NextStep()을 호출하는 버튼 컴포넌트
///
/// 【역할】 버튼 클릭 시 부모 계층에서 "Problem_*" 이름의 오브젝트를 찾고,
///          그 하위에서 StepFlowController를 리플렉션으로 검색하여 NextStep()을 호출한다.
///          인스펙터에서 직접 StepFlowController를 참조하지 않아도 동작하도록 설계되었다.
/// 【참조하는 곳】 StartStep 등의 "다음" 버튼에 부착하여 사용
/// 【참조되는 곳】 StepFlowController (리플렉션으로 메서드 호출)
/// 【흐름】 OnEnable() → Bind() → 부모 계층에서 Problem_* 찾기 → 하위에서 StepFlowController 검색
///          → Button.onClick에 리스너 등록 → 클릭 시 NextStep() 호출
///
/// ※ 리플렉션을 사용하는 이유: 여러 Problem_N에 걸쳐 공용으로 배치되는 버튼이므로,
///    런타임에 동적으로 대상 컨트롤러를 찾아 바인딩해야 한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class AutoNextStepButton : MonoBehaviour
{
    [Header("Search Rules")]
    [Tooltip("이 이름으로 시작하는 부모(또는 조상) 오브젝트를 찾는다. 예: Problem_1, Problem_2 ...")]
    [SerializeField] private string problemPrefix = "Problem_";

    [Tooltip("Problem_* 아래에서 찾을 컨트롤러 타입명(클래스명)")]
    [SerializeField] private string controllerTypeName = "StepFlowController";

    [Tooltip("호출할 메서드명 (기본: NextStep)")]
    [SerializeField] private string methodName = "NextStep";

    [Tooltip("true면 Problem_* 오브젝트 자신뿐 아니라 그 자식들까지 검색해서 StepFlowController를 찾음")]
    [SerializeField] private bool searchInChildren = true;

    [Header("Debug")]
    [SerializeField] private bool logOnBind = false; // true면 바인딩 성공/실패 로그 출력

    private Button _btn;                  // 이 오브젝트에 부착된 Button 컴포넌트
    private object _controllerInstance;   // 리플렉션으로 찾은 StepFlowController 인스턴스
    private MethodInfo _method;           // 리플렉션으로 찾은 NextStep() 메서드 정보
    private bool _bound;                  // 바인딩 완료 여부

    /// <summary>활성화 시 Button 컴포넌트를 캐싱하고 바인딩을 시도한다.</summary>
    private void OnEnable()
    {
        if (_btn == null) _btn = GetComponent<Button>();
        Bind();
    }

    /// <summary>비활성화 시 바인딩을 해제한다.</summary>
    private void OnDisable()
    {
        Unbind();
    }

    /// <summary>
    /// 부모 계층에서 Problem_* 오브젝트를 찾고, 그 하위에서 StepFlowController를 검색하여
    /// Button.onClick에 NextStep() 호출 리스너를 등록한다.
    /// </summary>
    private void Bind()
    {
        if (_bound) return;

        // 1) 부모 계층을 거슬러 올라가며 Problem_* 이름의 오브젝트 찾기
        var problemRoot = FindProblemRoot(transform, problemPrefix);
        if (problemRoot == null)
        {
            if (logOnBind) Debug.LogWarning($"[AutoNextStepButton] Problem root not found. prefix={problemPrefix}", this);
            return;
        }

        // 2) StepFlowController 찾기 (리플렉션으로 타입명 매칭)
        if (!TryFindController(problemRoot, controllerTypeName, searchInChildren, out _controllerInstance, out _method))
        {
            if (logOnBind) Debug.LogWarning($"[AutoNextStepButton] {controllerTypeName}.{methodName}() not found under {problemRoot.name}", this);
            return;
        }

        // 3) 버튼 클릭에 연결 (중복 방지를 위해 Remove 후 Add)
        _btn.onClick.RemoveListener(InvokeNextStep);
        _btn.onClick.AddListener(InvokeNextStep);

        _bound = true;

        if (logOnBind)
            Debug.Log($"[AutoNextStepButton] Bound -> {problemRoot.name}/{controllerTypeName}.{methodName}()", this);
    }

    /// <summary>Button.onClick에서 리스너를 제거하고 바인딩 상태를 해제한다.</summary>
    private void Unbind()
    {
        if (_btn == null) return;
        _btn.onClick.RemoveListener(InvokeNextStep);
        _bound = false;
    }

    /// <summary>
    /// 버튼 클릭 시 실행되는 콜백. 리플렉션으로 찾은 NextStep() 메서드를 호출한다.
    /// 컨트롤러가 런타임 중 변경되었을 수 있으므로, null이면 재바인딩을 시도한다.
    /// </summary>
    private void InvokeNextStep()
    {
        if (_controllerInstance == null || _method == null)
        {
            // 런타임에 오브젝트가 바뀌었을 수 있으므로 재바인딩 시도
            _bound = false;
            Bind();
            if (_controllerInstance == null || _method == null) return;
        }

        _method.Invoke(_controllerInstance, null);
    }

    /// <summary>
    /// 지정된 Transform에서 부모 방향으로 올라가며 prefix로 시작하는 이름의 오브젝트를 찾는다.
    /// </summary>
    /// <param name="from">검색 시작 Transform</param>
    /// <param name="prefix">찾을 오브젝트 이름의 접두사 (예: "Problem_")</param>
    /// <returns>찾은 Transform. 없으면 null.</returns>
    private static Transform FindProblemRoot(Transform from, string prefix)
    {
        var t = from;
        while (t != null)
        {
            if (!string.IsNullOrEmpty(t.name) && t.name.StartsWith(prefix, StringComparison.Ordinal))
                return t;
            t = t.parent;
        }
        return null;
    }

    /// <summary>
    /// problemRoot 아래의 MonoBehaviour들 중에서 타입명이 일치하는 컴포넌트를 찾고,
    /// 해당 컴포넌트에서 파라미터 없는 메서드(methodName)를 리플렉션으로 가져온다.
    /// </summary>
    /// <param name="problemRoot">검색 루트 Transform</param>
    /// <param name="typeName">찾을 컴포넌트의 클래스명 (예: "StepFlowController")</param>
    /// <param name="includeChildren">true면 자식까지 검색, false면 루트만 검색</param>
    /// <param name="instance">찾은 컴포넌트 인스턴스 (out)</param>
    /// <param name="method">찾은 메서드 정보 (out)</param>
    /// <returns>성공 여부</returns>
    private bool TryFindController(
        Transform problemRoot,
        string typeName,
        bool includeChildren,
        out object instance,
        out MethodInfo method)
    {
        instance = null;
        method = null;

        // 루트 및 자식들의 모든 MonoBehaviour를 가져와 타입명 매칭
        var monos = includeChildren
            ? problemRoot.GetComponentsInChildren<MonoBehaviour>(true)
            : problemRoot.GetComponents<MonoBehaviour>();

        for (int i = 0; i < monos.Length; i++)
        {
            var mb = monos[i];
            if (mb == null) continue;

            var t = mb.GetType();
            if (!string.Equals(t.Name, typeName, StringComparison.Ordinal))
                continue;

            // 파라미터 없는 메서드 찾기 (NextStep() 등)
            var mi = t.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi == null) continue;
            if (mi.GetParameters().Length != 0) continue;

            instance = mb;
            method = mi;
            return true;
        }

        return false;
    }
}
