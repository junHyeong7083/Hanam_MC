public readonly struct Result
{
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

    public Result(T value) { Ok = true; Error = AuthError.None; Message = null; Value = value; }
    public Result(AuthError e, string msg = null) { Ok = false; Error = e; Message = msg; Value = default; }

    public static Result<T> Success(T v) => new Result<T>(v);
    public static Result<T> Fail(AuthError e, string msg = null) => new Result<T>(e, msg);
}

public enum AuthError
{
    None = 0,
    NameEmpty,
    EmailInvalid,
    EmailDuplicate,
    PasswordWeak,
    NotFoundOrInactive,
    PasswordMismatch,
    Internal
}
