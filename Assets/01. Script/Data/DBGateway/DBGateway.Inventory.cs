// DBGateway.Inventory.cs
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DBGateway
{
    private const string CInventory = "inventory";

    /// <summary>
    /// 인벤토리 아이템 추가.
    /// 기존 inventory 컬렉션에 옛날 스키마 데이터가 섞여 있으면
    /// InvalidCastException 이 날 수 있으므로, 한 번 컬렉션을 Drop 후 재시도한다.
    /// </summary>
    public void AddInventoryItem(InventoryItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        WithDb(db =>
        {
            var col = db.GetCollection<InventoryItem>(CInventory);

            try
            {
                col.Insert(item);
            }
            catch (InvalidCastException)
            {
                // 개발 중에 스키마가 바뀌어서 꼬인 경우: 컬렉션 리셋
                db.DropCollection(CInventory);

                col = db.GetCollection<InventoryItem>(CInventory);
                col.Insert(item);
            }
        });
    }

    /// <summary>
    /// 해당 유저가 특정 itemId를 이미 가지고 있는지 여부.
    /// 스키마 충돌 시 컬렉션을 드랍하고 false 리턴.
    /// </summary>
    public bool HasInventoryItem(string userEmail, string itemId)
    {
        if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(itemId))
            return false;

        return WithDb(db =>
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
    /// 특정 유저의 인벤토리 전체 조회.
    /// 스키마 충돌 시 컬렉션을 드랍하고 빈 리스트 리턴.
    /// </summary>
    public List<InventoryItem> GetInventoryByUser(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return new List<InventoryItem>();

        return WithDb(db =>
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
