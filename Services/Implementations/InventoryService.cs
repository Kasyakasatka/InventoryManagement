using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Serilog; 
using System.Data;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Web.Services.Implementations;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ApplicationDbContext context, IMapper mapper, ILogger<InventoryService> logger) 
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<InventoryViewDTO>> GetLatestInventoriesAsync(int count)
    {
        _logger.LogInformation("Retrieving the latest {Count} inventories.", count);
        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToListAsync();
        _logger.LogInformation("Successfully retrieved {Count} inventories.", inventories.Count);
        return _mapper.Map<IEnumerable<InventoryViewDTO>>(inventories);
    }
    public async Task<IEnumerable<InventoryViewDTO>> GetMostPopularInventoriesAsync(int count)
    {
        _logger.LogInformation("Retrieving the top {Count} most popular inventories.", count);

        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .Select(i => new { Inventory = i, ItemCount = i.Items.Count()})
            .OrderByDescending(x => x.ItemCount)
            .Take(count)
            .ToListAsync();

        var inventoryDTOs = inventories.Select(x => {
            var dto = _mapper.Map<InventoryViewDTO>(x.Inventory);
            dto.ItemCount = (uint)x.ItemCount;
            return dto;
        });

        _logger.LogInformation("Successfully retrieved {Count} popular inventories.", inventoryDTOs.Count());
        return inventoryDTOs;
    }
    public async Task<Inventory?> GetInventoryByIdAsync(Guid id)
    {
        _logger.LogInformation("Attempting to get inventory with ID {InventoryId}.", id);
        var inventory = await _context.Inventories
            .Include(i => i.Creator)
            .Include(i => i.Category)
            .Include(i => i.FieldDefinitions)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} not found.", id);
        }
        else
        {
            _logger.LogInformation("Inventory with ID {InventoryId} found successfully.", id);
        }
        return inventory;
    }
    public async Task<Guid> CreateInventoryAsync(InventoryDTO inventoryDto, Guid creatorId)
    {
        _logger.LogInformation("Creating a new inventory for user {CreatorId}.", creatorId);
        var inventory = _mapper.Map<Inventory>(inventoryDto);
        inventory.CreatorId = creatorId;
        inventory.CreatedAt = DateTime.UtcNow;
        inventory.Id = Guid.NewGuid();
        if (string.IsNullOrEmpty(inventory.CustomIdFormat))
        {
            var defaultFormat = new
            {
                Elements = new[]
                {
                new { Type = "FixedText", Value = "INV-" },
                new { Type = "Sequence", Value = "" }
            }
            };
            inventory.CustomIdFormat = System.Text.Json.JsonSerializer.Serialize(defaultFormat);
        }
        if (inventoryDto.FieldDefinitions != null)
        {
            inventory.FieldDefinitions = inventoryDto.FieldDefinitions
                .Select(f => _mapper.Map<FieldDefinition>(f))
                .ToList();
        }
        else
        {
            inventory.FieldDefinitions = new List<FieldDefinition>();
        }
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();
        _logger.LogInformation("New inventory with ID {InventoryId} created successfully.", inventory.Id);
        return inventory.Id;
    }
    public async Task UpdateInventoryAsync(Guid inventoryId, InventoryDTO inventoryDto)
    {
        _logger.LogInformation("Attempting to update inventory with ID {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories
           .Include(i => i.FieldDefinitions) 
           .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Update failed: Inventory with ID {InventoryId} not found.", inventoryId);
            throw new NotFoundException($"Inventory with ID {inventoryId} not found.");
        }
        _mapper.Map(inventoryDto, inventory);
        inventory.LastModifiedAt = DateTime.UtcNow;
        var newFieldDefinitions = inventoryDto.FieldDefinitions
            .Where(f => f.Id == null)
            .ToList();
        var updatedFieldDefinitions = inventoryDto.FieldDefinitions
            .Where(f => f.Id != null)
            .ToList();
        var fieldsToRemove = inventory.FieldDefinitions
           .Where(f => !updatedFieldDefinitions.Any(uf => uf.Id == f.Id))
           .ToList();
        _context.FieldDefinitions.RemoveRange(fieldsToRemove);
        foreach (var newField in newFieldDefinitions)
        {
            var field = _mapper.Map<FieldDefinition>(newField);
            field.Id = Guid.NewGuid();
            inventory.FieldDefinitions.Add(field);
        }
        foreach (var updatedField in updatedFieldDefinitions)
        {
            var field = inventory.FieldDefinitions.FirstOrDefault(f => f.Id == updatedField.Id);
            if (field != null)
            {
                _mapper.Map(updatedField, field);
            }
        }
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory with ID {InventoryId} updated successfully.", inventoryId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict occurred for inventory {InventoryId}. Message: {Message}", inventoryId, ex.Message);
            throw new ConcurrencyException("The inventory has been modified by another user. Please refresh and try again.");
        }
    }
    public async Task DeleteInventoryAsync(Guid inventoryId)
    {
        _logger.LogInformation("Attempting to delete inventory with ID {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories.FindAsync(inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Deletion attempt for inventory {InventoryId} successful, as it was not found.", inventoryId);
            return;
        }
        _context.Inventories.Remove(inventory);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Inventory with ID {InventoryId} deleted successfully.", inventoryId);
    }
    public async Task<Inventory?> GetInventoryWithAccessAsync(Guid id)
    {
        _logger.LogInformation("Retrieving inventory with ID {InventoryId} including access rights.", id);
        var inventory = await _context.Inventories
            .Include(i => i.Creator)
            .Include(i => i.Category)
            .Include(i => i.InventoryAccesses)
            .Include(i => i.FieldDefinitions)
            .AsSplitQuery() 
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} (including access rights) not found.", id);
        }
        else
        {
            _logger.LogInformation("Inventory with ID {InventoryId} (including access rights) retrieved successfully.", id);
        }
        return inventory;
    }
    public async Task<IEnumerable<InventoryViewDTO>> SearchInventoriesAsync(string query)
    {
        _logger.LogInformation("Performing full-text search for inventories with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<InventoryViewDTO>();
        }
        var formattedQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => $"{w}:*"));
        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .Where(i => i.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        _logger.LogInformation("Found {Count} inventories for query: {Query}.", inventories.Count, query);
        return _mapper.Map<IEnumerable<InventoryViewDTO>>(inventories);
    }
    public async Task<IEnumerable<Item>> GetItemsByInventoryIdAsync(Guid inventoryId)
    {
        _logger.LogInformation("Retrieving items for inventory {InventoryId}.", inventoryId);
        return await _context.Items
            .Include(i => i.CustomFields)
            .Where(i => i.InventoryId == inventoryId)
            .ToListAsync();
    }
}