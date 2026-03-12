/// <summary>
/// Result - 인증/데이터 작업의 결과를 담는 값 타입 (성공 여부 + 에러 정보)
///
/// 【역할】 반환값이 없는 작업(가입, 업데이트 등)의 성공/실패를 표현한다.
///         AuthError 열거형으로 실패 원인을 구분하고, Message로 상세 내용을 전달한다.
/// 【씬】 모든 씬에서 사용 (인증, 데이터 계층의 공용 타입)
/// 【참조하는 곳】 IAuthService, IAdminDataService 등 서비스 계층의 반환 타입
/// 【참조되는 곳】 LoginController, SignupController 등에서 결과 처리
/// 【흐름】 서비스 호출 → Result/Result{T} 반환 → Ok 확인 → 성공/실패 분기
/// </summary>
public readonly struct Result
{
    /// <summary>작업이 성공했는지 여부</summary>
    public bool Ok { get; }

    /// <summary>실패 시 에러 코드 (성공 시 None)</summary>
    public AuthError Error { get; }

    /// <summary>추가 메시지 (에러 상세 설명 등)</summary>
    public string Message { get; }

    public Result(bool ok, AuthError error = AuthError.None, string message = null)
    {
        Ok = ok; Error = error; Message = message;
    }

    /// <summary>성공 결과 생성</summary>
    public static Result Success() => new Result(true);

    /// <summary>실패 결과 생성 (에러 코드 + 선택적 메시지)</summary>
    public static Result Fail(AuthError e, string msg = null) => new Result(false, e, msg);
}

/// <summary>
/// Result{T} - 반환값이 있는 작업의 결과를 담는 제네릭 값 타입
///
/// 【역할】 로그인(User 반환), 검색(배열 반환) 등 값을 반환하는 작업의 성공/실패를 표현한다.
///         성공 시 Value에 결과값이 담기고, 실패 시 Error/Message에 원인이 담긴다.
/// </summary>
public readonly struct Result<T>
{
    /// <summary>작업이 성공했는지 여부</summary>
    public bool Ok { get; }

    /// <summary>실패 시 에러 코드 (성공 시 None)</summary>
    public AuthError Error { get; }

    /// <summary>추가 메시지 (에러 상세 설명 등)</summary>
    public string Message { get; }

    /// <summary>성공 시 반환값 (실패 시 default)</summary>
    public T Value { get; }

    /// <summary>성공 결과 생성자 (값 포함)</summary>
    public Result(T value)
    {
        Ok = true; Error = AuthError.None; Message = null; Value = value;
    }

    /// <summary>실패 결과 생성자 (에러 코드 + 선택적 메시지)</summary>
    public Result(AuthError e, string msg = null)
    {
        Ok = false; Error = e; Message = msg; Value = default;
    }

    /// <summary>성공 결과 팩토리 메서드</summary>
    public static Result<T> Success(T v) => new Result<T>(v);

    /// <summary>실패 결과 팩토리 메서드</summary>
    public static Result<T> Fail(AuthError e, string msg = null) => new Result<T>(e, msg);
}

/// <summary>
/// AuthError - 인증 및 데이터 작업에서 공통으로 사용하는 에러 코드 열거형
///
/// 【역할】 Result/Result&lt;T&gt;의 Error 필드에 사용되는 에러 유형을 정의한다.
///          이름은 Auth 계열이지만, 전체 Service 계층에서 범용으로 사용된다.
/// </summary>
public enum AuthError
{
    /// <summary>오류 없음 (성공)</summary>
    None = 0,
    /// <summary>이름이 비어 있음 (회원가입 시)</summary>
    NameEmpty,
    /// <summary>이메일 형식이 잘못됨</summary>
    EmailInvalid,
    /// <summary>이메일이 이미 존재함 (회원가입 시 중복)</summary>
    EmailDuplicate,
    /// <summary>비밀번호 정책 미달 (6자 미만 또는 영문/숫자 미포함)</summary>
    PasswordWeak,
    /// <summary>사용자를 찾을 수 없거나 비활성화됨 (로그인/조회 시)</summary>
    NotFoundOrInactive,
    /// <summary>비밀번호 불일치 (로그인 시)</summary>
    PasswordMismatch,
    /// <summary>내부 오류 (네트워크/DB/예상치 못한 예외 등)</summary>
    Internal,
    /// <summary>인벤토리 관련 오류 (아이템 지급/조회 실패)</summary>
    InventoryError
}
