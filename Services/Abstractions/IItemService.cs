using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IItemService
{
    Task<IEnumerable<Item>> GetItemsByInventoryIdAsync(Guid inventoryId);
    Task<Item?> GetItemByIdAsync(Guid itemId);
    Task<Guid> CreateItemAsync(Guid inventoryId, ItemDTO itemDto, Guid createdByUserId);
    Task UpdateItemAsync(Guid itemId, ItemDTO itemDto);
    Task DeleteItemAsync(Guid itemId);
    Task<IEnumerable<Item>> SearchItemsAsync(string query);
}