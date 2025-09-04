using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Web.Services.Implementations;

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemService> _logger;
    private readonly ICustomIdGeneratorService _idGenerator;

    public ItemService(ApplicationDbContext context, IMapper mapper, ILogger<ItemService> logger, ICustomIdGeneratorService idGenerator)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _idGenerator = idGenerator;
    }
    public async Task<IEnumerable<Item>> GetItemsByInventoryIdAsync(Guid inventoryId)
    {
        _logger.LogInformation("Retrieving items for inventory {InventoryId}.", inventoryId);
        return await _context.Items
            .Where(i => i.InventoryId == inventoryId)
            .ToListAsync();
    }
    public async Task<Item?> GetItemByIdAsync(Guid itemId)
    {
        _logger.LogInformation("Retrieving item with ID {ItemId}.", itemId);
        return await _context.Items
            .Include(i => i.CreatedBy) 
            .Include(i => i.CustomFields)
            .ThenInclude(cf => cf.FieldDefinition)
            .FirstOrDefaultAsync(i => i.Id == itemId);
    }

    public async Task<Guid> CreateItemAsync(Guid inventoryId, ItemDTO itemDto, Guid createdByUserId)
    {
        _logger.LogInformation("Creating new item for inventory {InventoryId}.", inventoryId);
        if (itemDto.CustomFields == null)
        {
            itemDto.CustomFields = new List<CustomFieldValueDTO>();
        }
        var requiredFieldDefinitions = await _context.FieldDefinitions
            .AsNoTracking()
            .Where(fd => fd.InventoryId == inventoryId && fd.IsRequired)
            .ToListAsync();
        var providedFieldDefinitionIds = itemDto.CustomFields
            .Select(cf => cf.FieldDefinitionId)
            .ToHashSet();
        foreach (var requiredField in requiredFieldDefinitions)
        {
            if (!providedFieldDefinitionIds.Contains(requiredField.Id))
            {
                throw new InvalidOperationException($"Required field '{requiredField.Title}' is missing.");
            }
        }
        if (string.IsNullOrEmpty(itemDto.CustomId))
        {
            itemDto.CustomId = await _idGenerator.GenerateIdAsync(inventoryId);
        }
        var item = _mapper.Map<Item>(itemDto);
        item.InventoryId = inventoryId;
        item.CreatedById = createdByUserId;
        item.CreatedAt = DateTime.UtcNow;
        item.Id = Guid.NewGuid();
        _context.Items.Add(item);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("New item with ID {ItemId} created successfully in inventory {InventoryId}.", item.Id, inventoryId);
            return item.Id;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create item in inventory {InventoryId}. Possible duplicate CustomId.", inventoryId);
            throw new InvalidOperationException("Failed to create item. A duplicate custom ID may exist.", ex);
        }
    }

    public async Task UpdateItemAsync(Guid itemId, ItemDTO itemDto)
    {
        _logger.LogInformation("Updating item with ID {ItemId}.", itemId);
        var item = await _context.Items
           .Include(i => i.CustomFields)
           .FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null)
        {
            _logger.LogWarning("Update failed: Item {ItemId} not found.", itemId);
            throw new NotFoundException($"Item with ID {itemId} not found.");
        }
        item.CustomId = itemDto.CustomId;
        var fieldIdsInDto = itemDto.CustomFields
            .Select(cf => cf.FieldDefinitionId)
            .ToHashSet();
        var fieldsToRemove = item.CustomFields
            .Where(cf => !fieldIdsInDto
            .Contains(cf.FieldDefinitionId))
            .ToList();
        _context.CustomFields.RemoveRange(fieldsToRemove);
        foreach (var fieldDto in itemDto.CustomFields)
        {
            var existingField = item.CustomFields.FirstOrDefault(cf => cf.FieldDefinitionId == fieldDto.FieldDefinitionId);
            if (existingField != null)
            {
                _mapper.Map(fieldDto, existingField);
            }
            else
            {
                var customField = _mapper.Map<CustomFieldValue>(fieldDto);
                customField.Id = Guid.NewGuid();
                customField.Item = item;
                item.CustomFields.Add(customField);
            }
        }
        _context.Items.Update(item);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Item with ID {ItemId} updated successfully.", itemId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during update of item {ItemId}.", itemId);
            throw new ConcurrencyException("The item has been modified by another user. Please refresh and try again.");
        }
    }
    public async Task DeleteItemAsync(Guid itemId)
    {
        _logger.LogInformation("Deleting item with ID {ItemId}.", itemId);
        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
        {
            _logger.LogWarning("Deletion attempt for item {ItemId} successful, as it was not found.", itemId);
            return;
        }
        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Item with ID {ItemId} deleted successfully.", itemId);
    }
    public async Task<IEnumerable<Item>> SearchItemsAsync(string query)
    {
        _logger.LogInformation("Performing full-text search for items with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Item>();
        }
        var formattedQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => $"{w}:*"));
        var items = await _context.Items
            .Include(i => i.Inventory)
            .Where(i => i.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        _logger.LogInformation("Found {Count} items for query: {Query}.", items.Count, query);
        return items;
    }
}