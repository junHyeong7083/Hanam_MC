public readonly struct Result
{
    // 작업이 성공했는지 여부
    public bool Ok { get; }

    public AuthError Error { get; }

    public string Message { get; }

    public Result(bool ok, AuthError error = AuthError.None, string message = null)
    {
        Ok = ok; Error = error; Message = message;
    }

    public static Result Success() => new Result(true);

    public static Result Fail(AuthError e, string msg = null) => new Result(false, e, msg);
}

public readonly struct Result<T>
{
    
    public bool Ok { get; }

    public AuthError Error { get; }

    public string Message { get; }

    public T Value { get; }

    public Result(T value)
    {
        Ok = true; Error = AuthError.None; Message = null; Value = value;
    }

    public Result(AuthError e, string msg = null)
    {
        Ok = false; Error = e; Message = msg; Value = default;
    }

    public static Result<T> Success(T v) => new Result<T>(v);

    public static Result<T> Fail(AuthError e, string msg = null) => new Result<T>(e, msg);
}

// 인증/가입/로그인에서 공통으로 쓰는 에러 코드.
public enum AuthError
{
    //오류 없음(성공)
    None = 0,
    // 이름이 비어 있음
    NameEmpty,
    // 이메일 형식이 잘못됨
    EmailInvalid,
    // 이메일이 이미 존재함
    EmailDuplicate,
    // 비밀번호 정책 미달
    PasswordWeak,
    // 사용자를 찾을 수 없거나 비활성화됨
    NotFoundOrInactive,
    // 비밀번호 불일치
    PasswordMismatch,
    // 내부 오류(네트워크/DB/예상치 못한 예외 등)
    Internal,
    InventoryError
}
