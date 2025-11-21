using System;

public class Model
{ }

public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }

public enum ProblemTheme
{
    Director = 0,
    Gardener = 1
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
    public string UserId { get; set; }     // User(class).Id 참조
    public string Theme { get; set; }
    public int Stage { get; set; }         // 1..10 (문제 단계 번호)
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
    public string UserEmail { get; set; }   // 소유자
    public string ItemId { get; set; }      // "mind_lens" 같은 내부 ID
    public string ItemName { get; set; }    // "마음 렌즈" (관리자/리포트용)

    public ProblemTheme Theme { get; set; } // 어디 문제에서 주는지 메
    public DateTime AcquiredAt { get; set; }
}

public class SessionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserEmail { get; set; }
    public ProblemTheme Theme { get; set; }
    public string CurrentStep { get; set; }     // enum 직렬화 or string
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Attempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; }
    public string UserEmail { get; set; }

    // 어떤 문제에 대한 시도인지
    public string Content { get; set; }         // 텍스트/녹취 요약 등
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
