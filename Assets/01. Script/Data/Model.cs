using System;

public class Model
{ }

public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }

public enum ProblemTheme
{
    Director = 0,
    Gardener = 1
}

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

public class User
{
    // 고유 식별자
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }

    // 로그인시 사용되는 아이디
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.USER;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // admin 검색용 보조필드
    public string LowerName { get; set; }
    public string NameChosung { get; set; }
}

public class ResultDoc
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // 메인 키: UserId
    public string UserId { get; set; }     // User.Id 참조

    // Theme은 string으로 저장 (예: "Director")
    public string Theme { get; set; }

    // 기존 Stage → ProblemIndex 로 네이밍 통일
    public int ProblemIndex { get; set; }  // 1..10 (문제 번호)

    // 요약 정보 (점수, 정답 비율, 걸린 시간 등)
    public int Score { get; set; }                 // 점수
    public decimal? CorrectRate { get; set; }      // 정답 비율 (0~1)
    public int? DurationSec { get; set; }          // 전체 풀이 시간(초)

    // 상세 메타 정보 (문항별 로그, 요약 JSON 등)
    public string MetaJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Problem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OwnerEmail { get; set; }

    // Director / Gardener 테마
    public ProblemTheme Theme { get; set; }

    // 테마 안에서의 문제 번호 (1..10)
    public int Index { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();          // LiteDB ObjectId 문자열 or Guid
    public string UserId { get; set; }   // 소유자
    public string UserEmail { get; set; }
    public string ItemId { get; set; }      // "mind_lens" 같은 내부 ID
    public string ItemName { get; set; }    // "마음 렌즈" (관리자/리포트용)
    public int ProblemIndex { get; set; }  // 어느 문제(1..N)에서 얻었는지 (필요 없으면 null)
    public ProblemTheme Theme { get; set; } // 어디 문제에서 주는지 메
    public DateTime AcquiredAt { get; set; }
}

public class SessionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // 메인 키: UserId (UserEmail은 보조)
    public string UserId { get; set; }
    public string UserEmail { get; set; }

    public ProblemTheme Theme { get; set; }

    // 현재 진행 중인 Step 키 (예: "Director_Problem1_Step3")
    public string CurrentStep { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Attempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SessionId { get; set; }

    // 메인 키: UserId, UserEmail은 보조
    public string UserId { get; set; }
    public string UserEmail { get; set; }

    // 어떤 문제에 대한 시도인지
    public string Content { get; set; }         // 텍스트/요약 JSON 등
    public string ProblemId { get; set; }       // Problem.Id
    public ProblemTheme Theme { get; set; }     // Director / Gardener
    public int? ProblemIndex { get; set; }      // 1..10

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Feedback
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResultId { get; set; }
    public string AdminEmail { get; set; }
    public string Comment { get; set; }
    public float? Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class ProblemFlowSummary
{
    // 이 문제를 푸는 동안 시도(Attempt)가 몇 번 있었는지
    public int AttemptCount { get; set; }

    // 문제 풀이에 걸린 시간(초). 알 수 없으면 null
    public int? DurationSec { get; set; }

    // 최종적으로 성공했는지 여부 (여기서는 항상 true로 쓰게 될 가능성 높음)
    public bool Succeeded { get; set; } = true;
}

public class UserProgress
{
    public string UserEmail { get; set; }
    public int TotalSessions { get; set; }
    public int TotalSolved { get; set; }
    public DateTime? LastSessionAt { get; set; }

    public void MarkSolved(string themeKey, int problemIndex)
    {
        // 여기서는 요약 정보만 간단히 갱신해도 됨.
        // (필요하면 나중에 테마별 통계, 최근 푼 문제 정보 등 더 넣을 수 있음)

        TotalSolved++;
        LastSessionAt = DateTime.UtcNow;
    }
}

public class UserSummary
{
    public string Email { get; set; }
    public string Name { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
