using UnityEngine;

/// <summary>
/// AuthUIText - 인증 UI에서 사용하는 텍스트 문자열을 모아둔 ScriptableObject
///
/// 【역할】 회원가입/로그인 화면에서 표시하는 안내 메시지, 에러 메시지, 성공 메시지를
///          Inspector에서 편집할 수 있도록 ScriptableObject로 분리한 것.
///          하드코딩된 문자열 대신 이 에셋을 참조하여 UI 텍스트를 관리한다.
/// 【참조하는 곳】 LoginController, RegisterController (UI 메시지 표시)
/// 【생성 방법】 Unity 메뉴: Create > Auth > UI Text
/// 【참고】 문제 풀이 화면의 텍스트는 CSV DataTable(ProblemRuntime.L)을 사용하고,
///          이 에셋은 인증 화면 전용이다.
/// </summary>
[CreateAssetMenu(fileName = "AuthUIText", menuName = "Auth/UI Text")]
public class AuthUIText : ScriptableObject
{
    [Header("Common")]
    /// <summary>필수 입력 항목이 비어있을 때 표시할 메시지</summary>
    public string required = "필수 항목을 입력하세요.";

    [Header("Signup")]
    /// <summary>이메일 형식이 올바르지 않을 때</summary>
    public string emailFormatError = "이메일 형식 오류";
    /// <summary>이미 가입된 이메일일 때</summary>
    public string emailDuplicate = "이미 등록된 이메일입니다.";
    /// <summary>이메일 중복 확인 통과 시</summary>
    public string emailAvailable = "사용 가능한 이메일입니다.";
    /// <summary>이름이 비어있을 때</summary>
    public string nameEmpty = "이름을 입력하세요.";
    /// <summary>비밀번호 정책 미달 시 안내</summary>
    public string pwWeak = "최소 6자, 문자+숫자 포함";
    /// <summary>비밀번호 정책 충족 시 표시</summary>
    public string pwStrong = "강력한 비밀번호";
    /// <summary>비밀번호 확인이 일치하지 않을 때</summary>
    public string pwConfirmMismatch = "비밀번호 확인이 일치하지 않습니다.";
    /// <summary>회원가입 실패 시 표시</summary>
    public string signupFail = "가입 실패. 다시 시도하세요.";
    /// <summary>회원가입 성공 시 표시</summary>
    public string signupDone = "가입 완료";

    [Header("Login")]
    /// <summary>로그인 처리 중 표시</summary>
    public string loginInProgress = "로그인 중...";
    /// <summary>로그인 실패 시 표시</summary>
    public string loginFail = "이메일 또는 비밀번호가 올바르지 않습니다.";
    /// <summary>로그인 성공 시 표시</summary>
    public string loginDone = "로그인 성공";
}
