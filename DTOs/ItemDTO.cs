using System.Collections.Generic;

namespace InventoryManagement.Web.DTOs;

public class ItemDTO
{
    public required string CustomId { get; set; }
    public required List<CustomFieldValueDTO> CustomFields { get; set; }
    
}