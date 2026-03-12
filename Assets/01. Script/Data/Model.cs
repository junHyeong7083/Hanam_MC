using System;

/// <summary>
/// Model.cs - 앱 전체에서 사용하는 데이터 모델(엔티티) 정의 파일
///
/// 【역할】 LiteDB에 저장되는 모든 도큐먼트 모델과 공용 enum, DTO를 한 곳에 정의한다.
///          이 파일의 클래스들은 Repository 계층에서 LiteDB 컬렉션의 문서 타입으로 사용된다.
/// 【포함 모델】
///   - User: 사용자 계정 (인증, 권한)
///   - ResultDoc: 문제 풀이 결과 기록
///   - Problem: 문제 정의
///   - InventoryItem: 보상 아이템 (사용자 인벤토리)
///   - SessionRecord: 세션 기록
///   - Attempt: 문제 풀이 시도 기록
///   - Feedback: 관리자 피드백
///   - UserProgress: 사용자 진행도 요약 (DTO)
///   - UserSummary: 사용자 요약 정보 (DTO, 관리자 검색용)
///   - ProblemFlowSummary: 문제 풀이 흐름 요약 (DTO)
/// </summary>
public class Model
{ }

/// <summary>사용자 권한 등급. USER(일반) < ADMIN(관리자) < SUPERADMIN(최고관리자)</summary>
public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }

/// <summary>
/// 문제 테마. 현재 Director(마음 필름 감독)와 Gardener(마음 정원사) 두 가지가 있다.
/// HomeScene에서 테마를 선택하면 ProblemSession.CurrentTheme에 저장된다.
/// </summary>
public enum ProblemTheme
{
    Director = 0,
    Gardener = 1
}

/// <summary>
/// 문제 내 스텝 식별자. 각 문제는 Step1~Step4 + Reward + Extra1/Extra2로 구성될 수 있다.
/// StepFlowController에서 스텝 진행 순서를 관리할 때 사용한다.
/// </summary>
public enum StepId
{
    Step1,
    Step2,
    Step3,
    Step4,
    Reward,
    Extra1,
    Extra2,
}

/// <summary>
/// User - 사용자 계정 모델 (LiteDB "users" 컬렉션에 저장)
///
/// 【용도】 회원가입, 로그인, 권한 관리에 사용되는 핵심 엔티티.
///          BCrypt로 해시된 비밀번호를 저장하며, Email이 로그인 ID 역할을 한다.
/// 【관련 Repository】 UserRepository
/// 【관련 Service】 AuthService (가입/로그인), SessionManager (세션 관리)
/// </summary>
public class User
{
    /// <summary>고유 식별자 (GUID 문자열). LiteDB _id로 사용됨</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>사용자 표시 이름</summary>
    public string Name { get; set; }

    /// <summary>로그인 시 사용되는 이메일 (유니크). 소문자로 정규화하여 저장</summary>
    public string Email { get; set; }
    /// <summary>BCrypt로 해시된 비밀번호. 평문은 저장하지 않음</summary>
    public string PasswordHash { get; set; }
    /// <summary>사용자 권한 (USER=일반, ADMIN=관리자, SUPERADMIN=최고관리자)</summary>
    public UserRole Role { get; set; } = UserRole.USER;
    /// <summary>계정 활성화 상태. false이면 로그인 불가</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>계정 생성 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>관리자 검색용 보조필드: 이름 소문자 버전</summary>
    public string LowerName { get; set; }
    /// <summary>관리자 검색용 보조필드: 이름의 초성 (한글 검색 지원)</summary>
    public string NameChosung { get; set; }
}

/// <summary>
/// ResultDoc - 문제 풀이 결과 기록 (LiteDB "results" 컬렉션에 저장)
///
/// 【용도】 사용자가 특정 문제를 클리어했을 때 생성되는 결과 문서.
///          점수, 정답률, 풀이 시간 등을 기록한다.
///          MarkProblemSolvedForCurrentUser()에서 이미 같은 문제의 결과가 있으면 중복 생성하지 않는다.
/// 【관련 Repository】 ResultRepository
/// 【관련 Service】 LocalProgressService, LocalResultQueryService
/// </summary>
public class ResultDoc
{
    /// <summary>고유 식별자 (GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>외래 키: 이 결과를 생성한 사용자의 User.Id</summary>
    public string UserId { get; set; }

    /// <summary>테마명 문자열 (예: "Director", "Gardener"). ProblemTheme.ToString()으로 저장</summary>
    public string Theme { get; set; }

    /// <summary>문제 번호 (1~10). 테마 내에서의 순서</summary>
    public int ProblemIndex { get; set; }

    /// <summary>점수 (현재 기본값 0, 향후 채점 로직에서 설정)</summary>
    public int Score { get; set; }
    /// <summary>정답 비율 (0.0~1.0). null이면 미사용</summary>
    public decimal? CorrectRate { get; set; }
    /// <summary>전체 풀이 시간(초). null이면 미측정</summary>
    public int? DurationSec { get; set; }

    /// <summary>기타 메타 정보 (디버그 로그, 응답 JSON 등). null 허용</summary>
    public string MetaJson { get; set; }

    /// <summary>결과 생성 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Problem - 문제 정의 모델 (LiteDB "problems" 컬렉션에 저장)
///
/// 【용도】 각 테마(Director/Gardener)의 개별 문제를 정의한다.
///          Theme + Index 조합으로 유니크하게 식별된다.
/// 【관련 Repository】 ProblemRepository
/// 【관련 Service】 LocalProblemQueryService
/// </summary>
public class Problem
{
    /// <summary>고유 식별자 (GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>문제 출제자 이메일 (현재 미사용)</summary>
    public string OwnerEmail { get; set; }

    /// <summary>Director(마음 필름 감독) 또는 Gardener(마음 정원사) 테마</summary>
    public ProblemTheme Theme { get; set; }

    /// <summary>테마 안에서의 문제 번호 (1~10)</summary>
    public int Index { get; set; }

    /// <summary>문제 제목</summary>
    public string Title { get; set; }
    /// <summary>문제 내용/설명</summary>
    public string Content { get; set; }
    /// <summary>문제 생성 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// InventoryItem - 보상 아이템 모델 (LiteDB "inventory" 컬렉션에 저장)
///
/// 【용도】 사용자가 문제를 풀고 받는 보상 아이템. 각 문제마다 고유한 아이템이 있다.
///          StepInventory UI에서 획득한 아이템 목록을 표시할 때 사용된다.
/// 【관련 Repository】 InventoryRepository
/// 【관련 Service】 LocalRewardService
/// </summary>
public class InventoryItem
{
    /// <summary>고유 식별자 (GUID). LiteDB _id</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>소유자의 User.Id</summary>
    public string UserId { get; set; }
    /// <summary>소유자의 이메일 (조회 편의를 위해 중복 저장)</summary>
    public string UserEmail { get; set; }
    /// <summary>아이템 고유 ID (예: "mind_lens", "emotion_card"). 중복 지급 체크에 사용</summary>
    public string ItemId { get; set; }
    /// <summary>아이템 표시명 (예: "마음 렌즈"). UI에 표시할 때 사용</summary>
    public string ItemName { get; set; }
    /// <summary>이 아이템을 획득한 문제 번호 (1~10)</summary>
    public int ProblemIndex { get; set; }
    /// <summary>이 아이템을 획득한 테마 (Director/Gardener)</summary>
    public ProblemTheme Theme { get; set; }
    /// <summary>아이템 획득 시각 (UTC)</summary>
    public DateTime AcquiredAt { get; set; }
}

/// <summary>
/// SessionRecord - 사용자 세션 기록 (LiteDB "sessions" 컬렉션에 저장)
///
/// 【용도】 사용자의 문제 풀이 세션을 추적한다. 현재 진행 중인 스텝 정보를 저장한다.
///          ProgressRepository에서 TotalSessions 카운트에 사용된다.
/// 【관련 Repository】 ProgressRepository (세션 수 집계)
/// </summary>
public class SessionRecord
{
    /// <summary>고유 식별자 (GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>세션 소유자의 User.Id</summary>
    public string UserId { get; set; }
    /// <summary>세션 소유자의 이메일</summary>
    public string UserEmail { get; set; }

    /// <summary>세션의 테마 (Director/Gardener)</summary>
    public ProblemTheme Theme { get; set; }

    /// <summary>현재 진행 중인 스텝 키 (예: "Director_Problem1_Step3")</summary>
    public string CurrentStep { get; set; }

    /// <summary>세션 생성 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Attempt - 문제 풀이 시도 기록 (LiteDB "attempts" 컬렉션에 저장)
///
/// 【용도】 사용자가 각 스텝에서 수행한 시도를 기록한다.
///          Content에 사용자 응답을 JSON 직렬화하여 저장한다.
///          LocalProgressService.SaveStepAttemptForCurrentUser()에서 자동 생성된다.
/// 【관련 Repository】 ProgressRepository
/// 【관련 Service】 LocalProgressService, LocalRewardService
/// </summary>
public class Attempt
{
    /// <summary>고유 식별자 (GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>이 시도가 속한 세션의 SessionRecord.Id (현재 null로 저장, 향후 활용 예정)</summary>
    public string SessionId { get; set; }

    /// <summary>시도한 사용자의 User.Id</summary>
    public string UserId { get; set; }
    /// <summary>시도한 사용자의 이메일</summary>
    public string UserEmail { get; set; }

    /// <summary>사용자 응답 내용 (JSON 직렬화된 텍스트)</summary>
    public string Content { get; set; }
    /// <summary>이 시도가 속한 문제의 Problem.Id</summary>
    public string ProblemId { get; set; }
    /// <summary>테마 (Director/Gardener)</summary>
    public ProblemTheme Theme { get; set; }
    /// <summary>문제 번호 (1~10)</summary>
    public int? ProblemIndex { get; set; }

    /// <summary>시도 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// <summary>
/// Feedback - 관리자 피드백 모델 (LiteDB "feedback" 컬렉션에 저장)
///
/// 【용도】 관리자가 특정 ResultDoc에 대해 남기는 코멘트와 점수.
///          LocalAdminDataService.SubmitFeedback()으로 저장된다.
/// 【관련 Repository】 FeedbackRepository
/// 【관련 Service】 LocalAdminDataService
/// </summary>
public class Feedback
{
    /// <summary>고유 식별자 (GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>이 피드백이 달린 ResultDoc.Id</summary>
    public string ResultId { get; set; }
    /// <summary>피드백을 작성한 관리자 이메일</summary>
    public string AdminEmail { get; set; }
    /// <summary>피드백 코멘트 텍스트</summary>
    public string Comment { get; set; }
    /// <summary>관리자가 부여한 점수 (null이면 미부여)</summary>
    public float? Score { get; set; }
    /// <summary>피드백 작성 시각 (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ProblemFlowSummary - 문제 풀이 흐름 요약 DTO
///
/// 【용도】 하나의 문제를 풀기까지의 시도 횟수, 소요 시간, 성공 여부를 요약한다.
///          결과 화면(ResultScene)에서 표시하거나 분석에 사용 가능.
/// </summary>
public class ProblemFlowSummary
{
    /// <summary>이 문제를 푸는 동안 시도(Attempt)가 몇 번 있었는지</summary>
    public int AttemptCount { get; set; }

    /// <summary>전체 풀이에 걸린 시간(초). 측정하지 않았으면 null</summary>
    public int? DurationSec { get; set; }

    /// <summary>최종적으로 성공했는지 여부 (현재 항상 true, 추후 확장 예정)</summary>
    public bool Succeeded { get; set; } = true;
}

/// <summary>
/// UserProgress - 사용자 진행도 요약 DTO (DB에 직접 저장하지 않고, 쿼리 결과로 조립됨)
///
/// 【용도】 ProgressRepository.GetUserProgress()에서 sessions/results 컬렉션을 집계하여 생성.
///          홈 화면에서 사용자 진행 상황을 표시할 때 사용된다.
/// </summary>
public class UserProgress
{
    /// <summary>사용자 이메일</summary>
    public string UserEmail { get; set; }
    /// <summary>총 세션 수 (sessions 컬렉션에서 집계)</summary>
    public int TotalSessions { get; set; }
    /// <summary>총 풀이 완료 문제 수 (results 컬렉션에서 집계)</summary>
    public int TotalSolved { get; set; }
    /// <summary>마지막 세션 시각. null이면 세션 기록 없음</summary>
    public DateTime? LastSessionAt { get; set; }

    /// <summary>
    /// 문제 풀이 완료 시 카운터를 증가시킨다. 간단한 인메모리 업데이트용.
    /// (필요시 테마별 푼 수, 최근 푼 문제 목록 등 더 세분화 가능)
    /// </summary>
    public void MarkSolved(string themeKey, int problemIndex)
    {
        TotalSolved++;
        LastSessionAt = DateTime.UtcNow;
    }
}

/// <summary>
/// UserSummary - 사용자 요약 정보 DTO (관리자 검색 결과용)
///
/// 【용도】 UserRepository.SearchUsersFriendly()에서 User를 간략화하여 반환할 때 사용.
///          PasswordHash 등 민감 정보를 제외한 최소 정보만 포함한다.
/// </summary>
public class UserSummary
{
    /// <summary>사용자 이메일</summary>
    public string Email { get; set; }
    /// <summary>사용자 이름</summary>
    public string Name { get; set; }
    /// <summary>권한 등급</summary>
    public UserRole Role { get; set; }
    /// <summary>계정 활성화 상태</summary>
    public bool IsActive { get; set; }
}
