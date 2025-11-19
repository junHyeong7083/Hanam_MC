using System;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

public interface IUserRepository
{
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveByEmail(string email);
    User FindById(string id);
    void Insert(User u);
    void Update(User u);

    UserSummary[] SearchUsersFriendly(string query);
    UserSummary[] ListAllUsers(int limit = 0);
}


/// <summary>
/// User 컬렉션에 대해서 DbGateway를 래핑하는 얇은 레포지토리.
/// 실제 LiteDB 쿼리는 모두 DbGateway 안에 있고,
/// 이 클래스는 인터페이스(IUserRepository)를 통해 AuthService, Admin 쪽에 주입하기 위한 용도.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DBGateway _db;

    public UserRepository(DBGateway db = null)
    {
        _db = db ?? new DBGateway();
    }

    public bool ExistsEmail(string email) => _db.ExistsEmail(email);

    public bool HasSuperAdmin() => _db.HasSuperAdmin();

    public User FindActiveByEmail(string email) => _db.FindActiveUserByEmail(email);

    public User FindById(string id) => _db.FindUserById(id);

    public void Insert(User u) => _db.InsertUser(u);

    public void Update(User u) => _db.UpdateUser(u);

    public UserSummary[] SearchUsersFriendly(string query) =>
        _db.SearchUsersFriendly(query);

    public UserSummary[] ListAllUsers(int limit = 0) =>
        _db.ListAllUsers(limit);
}

