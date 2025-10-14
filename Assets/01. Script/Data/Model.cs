using System;

public class Model
{ }

public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }
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
    public int Stage { get; set; }         // 1..10 (문제 단계 번호)
    public int Score { get; set; }          // 점수
    public decimal? CorrectRate { get; set; } // 정답비율
    public int? DurationSec { get; set; } // 문제풀이에 걸린 시간
    public string MetaJson { get; set; }   // 문항별 로그 JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Problem
{
    public string Id { get; set; }
    public string OwnerEmail { get; set; }
    public string Theme { get; set; }     // Gardener/Director 등
    public string Title { get; set; }
    public string Content { get; set; }
    public System.DateTime CreatedAt { get; set; }
}
public class SessionRecord
{
    public string Id { get; set; }
    public string UserEmail { get; set; }
    public string Theme { get; set; }
    public string CurrentStep { get; set; }     // enum 직렬화 or string
    public System.DateTime CreatedAt { get; set; }
}
public class Attempt
{
    public string Id { get; set; }
    public string SessionId { get; set; }
    public string UserEmail { get; set; }
    public string Content { get; set; }         // 텍스트/녹취 요약 등
    public System.DateTime CreatedAt { get; set; }
}
public class Feedback
{
    public string Id { get; set; }
    public string ResultId { get; set; }
    public string AdminEmail { get; set; }
    public string Comment { get; set; }
    public float? Score { get; set; }
    public System.DateTime CreatedAt { get; set; }
}
public class UserProgress
{
    public string UserEmail { get; set; }
    public int TotalSessions { get; set; }
    public int TotalSolved { get; set; }
    public System.DateTime? LastSessionAt { get; set; }
}

public class UserSummary
{
    public string Email { get; set; }
    public string Name { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}