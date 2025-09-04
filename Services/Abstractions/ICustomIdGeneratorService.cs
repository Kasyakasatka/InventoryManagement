using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface ICustomIdGeneratorService
{
    Task<string> GenerateIdAsync(Guid inventoryId);
}