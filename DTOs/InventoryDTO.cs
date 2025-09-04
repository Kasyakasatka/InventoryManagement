namespace InventoryManagement.Web.DTOs;

public class InventoryDTO
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public required Guid CategoryId { get; set; }
    public required bool IsPublic { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? CustomIdFormat { get; set; }
    public List<FieldDefinitionDTO>? FieldDefinitions { get; set; }
    public Guid? Id { get; set; } 
    public uint? Version { get; set; }
    public string? TagsInput { get; set; }
    public IFormFile? ImageFile { get; set; }
    
}