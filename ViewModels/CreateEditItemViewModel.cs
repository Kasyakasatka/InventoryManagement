using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{
    public class CreateEditItemViewModel
    {
        public required Guid InventoryId { get; set; }
        public required ItemDTO ItemDTO { get; set; }
        public required IEnumerable<FieldDefinition> FieldDefinitions { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
