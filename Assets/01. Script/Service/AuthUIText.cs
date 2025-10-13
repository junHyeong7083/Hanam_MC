using UnityEngine;

[CreateAssetMenu(fileName = "AuthUIText", menuName = "Auth/UI Text", order = 0)]
public class AuthUIText : ScriptableObject
{
    [Header("Common")]
    public string required = "필수 항목을 입력하세요.";

    [Header("Signup")]
    public string emailFormatError = "이메일 형식 오류";
    public string emailDuplicate = "이미 사용 중인 이메일입니다.";
    public string emailAvailable = "사용 가능한 이메일입니다.";
    public string nameEmpty = "이름을 입력하세요.";
    public string pwWeak = "최소 8자, 문자+숫자 포함";
    public string pwStrong = "안전한 비밀번호";
    public string pwConfirmMismatch = "비밀번호 확인이 일치하지 않습니다.";
    public string signupFail = "가입 실패. 다시 시도하세요.";
    public string signupDone = "가입 완료";

    [Header("Login")]
    public string loginInProgress = "로그인 중...";
    public string loginFail = "이메일 또는 비밀번호가 올바르지 않습니다.";
    public string loginDone = "로그인 성공";
}
