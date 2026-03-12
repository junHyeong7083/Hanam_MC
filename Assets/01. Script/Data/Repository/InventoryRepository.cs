using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// IInventoryRepository - 인벤토리(보상 아이템) 데이터 접근 인터페이스
///
/// 【역할】 InventoryItem 엔티티의 추가, 존재 확인, 조회 기능을 정의한다.
/// 【참조하는 곳】 LocalRewardService (아이템 지급/조회), LocalUserDataService (아이템 지급/조회)
/// </summary>
public interface IInventoryRepository
{
    /// <summary>인벤토리 아이템을 추가한다</summary>
    void Add(InventoryItem item);
    /// <summary>특정 사용자가 특정 아이템을 이미 보유하고 있는지 확인한다</summary>
    bool HasItem(string userEmail, string itemId);
    /// <summary>특정 사용자의 전체 인벤토리를 조회한다</summary>
    List<InventoryItem> GetByUser(string userEmail);
}

/// <summary>
/// InventoryRepository - IInventoryRepository의 LiteDB 구현체
///
/// 【역할】 LiteDB "inventory" 컬렉션에 대한 CRUD 작업을 수행한다.
///          개발 중 스키마 변경으로 InvalidCastException이 발생할 수 있어,
///          예외 발생 시 컬렉션을 Drop하고 재시도하는 안전장치가 포함되어 있다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, LocalRewardService에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【컬렉션명】 "inventory"
/// 【주의】 스키마 변경 시 InvalidCastException → 컬렉션 Drop → 기존 데이터 소실
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>LiteDB 컬렉션명</summary>
    private const string CInventory = "inventory";

    public InventoryRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 인벤토리 아이템을 추가한다.
    /// 기존 inventory 컬렉션에 다른 스키마 데이터가 남아 있으면
    /// InvalidCastException이 날 수 있으므로, 그 때 컬렉션을 Drop 후 재시도한다.
    /// </summary>
    public void Add(InventoryItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<InventoryItem>(CInventory);

            try
            {
                col.Insert(item);
            }
            catch (InvalidCastException)
            {
                // ���� �߿� ��Ű���� �ٲ� ���� ���: �÷��� ����
                db.DropCollection(CInventory);

                col = db.GetCollection<InventoryItem>(CInventory);
                col.Insert(item);
            }
        });
    }

    /// <summary>
    /// 해당 사용자가 특정 itemId를 이미 보유하고 있는지 확인한다.
    /// 스키마 충돌 시 컬렉션을 삭제하고 false를 반환한다.
    /// </summary>
    public bool HasItem(string userEmail, string itemId)
    {
        if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(itemId))
            return false;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<InventoryItem>(CInventory);

            try
            {
                return col.Exists(x => x.UserEmail == userEmail && x.ItemId == itemId);
            }
            catch (InvalidCastException)
            {
                db.DropCollection(CInventory);
                return false;
            }
        });
    }

    /// <summary>
    /// 특정 사용자의 인벤토리 전체를 조회한다.
    /// 스키마 충돌 시 컬렉션을 삭제하고 빈 리스트를 반환한다.
    /// </summary>
    public List<InventoryItem> GetByUser(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return new List<InventoryItem>();

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<InventoryItem>(CInventory);

            try
            {
                return col.Find(x => x.UserEmail == userEmail).ToList();
            }
            catch (InvalidCastException)
            {
                db.DropCollection(CInventory);
                return new List<InventoryItem>();
            }
        });
    }
}
