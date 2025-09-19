using System;
using System.Collections.Generic;
using System.Data;

public class Model
{}

public enum UserRole {  USER=0, ADMIN = 1, SUPERADMIN = 2}
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
