using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("true면 Problem_* 오브젝트 자신뿐 아니라 그 자식들까지 포함해서 StepFlowController를 찾음")]
    [SerializeField] private bool searchInChildren = true;

    [Header("Debug")]
    [SerializeField] private bool logOnBind = false;

    private Button _btn;
    private object _controllerInstance;
    private MethodInfo _method;
    private bool _bound;

    private void OnEnable()
    {
        if (_btn == null) _btn = GetComponent<Button>();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (_bound) return;

        // 1) Problem_* 조상 찾기
        var problemRoot = FindProblemRoot(transform, problemPrefix);
        if (problemRoot == null)
        {
            if (logOnBind) Debug.LogWarning($"[AutoNextStepButton] Problem root not found. prefix={problemPrefix}", this);
            return;
        }

        // 2) StepFlowController 찾기 (리플렉션)
        if (!TryFindController(problemRoot, controllerTypeName, searchInChildren, out _controllerInstance, out _method))
        {
            if (logOnBind) Debug.LogWarning($"[AutoNextStepButton] {controllerTypeName}.{methodName}() not found under {problemRoot.name}", this);
            return;
        }

        // 3) 버튼 클릭에 연결 (중복 방지 위해 Remove 후 Add)
        _btn.onClick.RemoveListener(InvokeNextStep);
        _btn.onClick.AddListener(InvokeNextStep);

        _bound = true;

        if (logOnBind)
            Debug.Log($"[AutoNextStepButton] Bound -> {problemRoot.name}/{controllerTypeName}.{methodName}()", this);
    }

    private void Unbind()
    {
        if (_btn == null) return;
        _btn.onClick.RemoveListener(InvokeNextStep);
        _bound = false;
    }

    private void InvokeNextStep()
    {
        if (_controllerInstance == null || _method == null)
        {
            // 런타임에 계층이 바뀌었을 수도 있으니 한 번 재시도
            _bound = false;
            Bind();
            if (_controllerInstance == null || _method == null) return;
        }

        _method.Invoke(_controllerInstance, null);
    }

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

    private bool TryFindController(
        Transform problemRoot,
        string typeName,
        bool includeChildren,
        out object instance,
        out MethodInfo method)
    {
        instance = null;
        method = null;

        // 문제 루트 및 자식들의 모든 MonoBehaviour를 훑어서 타입명 매칭
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

            // 메서드 찾기 (파라미터 없는 NextStep()만 허용)
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