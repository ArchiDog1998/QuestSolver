using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestSolver.Helpers;
internal static class InventoryHelper
{
    public unsafe static int ItemCount(uint itemId, bool isHq)
    {
        return ItemCount(itemId, InventoryType.Inventory1, isHq)
             + ItemCount(itemId, InventoryType.Inventory2, isHq)
             + ItemCount(itemId, InventoryType.Inventory3, isHq)
             + ItemCount(itemId, InventoryType.Inventory4, isHq);
    }

    public unsafe static int ItemCount(uint itemId, InventoryType inventoryType, bool isHq)
    {
        return InventoryManager.Instance()->GetItemCountInContainer(itemId, inventoryType, isHq);
    }
}
