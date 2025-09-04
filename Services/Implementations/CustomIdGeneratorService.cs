using InventoryManagement.Web.Data;
using InventoryManagement.Web.Models.Configurations;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Globalization;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Data.Models.Enums;

namespace InventoryManagement.Web.Services.Implementations;

public class CustomIdGeneratorService : ICustomIdGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomIdGeneratorService> _logger;
    private static readonly Random _random = new Random();

    public CustomIdGeneratorService(ApplicationDbContext context, ILogger<CustomIdGeneratorService> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<string> GenerateIdAsync(Guid inventoryId)
    {
        _logger.LogInformation("Generating custom ID for inventory {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogError("Inventory {InventoryId} not found.", inventoryId);
            throw new InvalidOperationException("Inventory not found.");
        }
        var formatModel = JsonSerializer.Deserialize<CustomIdFormatModel>(inventory.CustomIdFormat);
        var idBuilder = new StringBuilder();
        foreach (var element in formatModel.Elements)
        {
            idBuilder.Append(await GenerateElementValueAsync(element, inventoryId));
        }
        return idBuilder.ToString();
    }
    private async Task<string> GenerateElementValueAsync(IdElement element, Guid inventoryId)
    {
        switch (element.Type)
        {
            case IdElementType.FixedText:
                return element.Value ?? string.Empty;
            case IdElementType.Random:
                return GenerateRandomValue(element.Format);
            case IdElementType.Sequence:
                return await GenerateSequenceValueAsync(element.Format, inventoryId);
            case IdElementType.DateTime:
                return DateTime.UtcNow.ToString(element.Format, CultureInfo.InvariantCulture);
            default:
                throw new InvalidOperationException($"Unknown ID element type: {element.Type}");
        }
    }
    private static string GenerateRandomValue(string? format)
    {
        if (format == null) return string.Empty;
        if (format.StartsWith("D"))
        {
            int length = int.Parse(format.Substring(1));
            int max = (int)Math.Pow(10, length) - 1;
            return _random.Next(0, max).ToString($"D{length}");
        }
        else if (format.StartsWith("X"))
        {
            int length = int.Parse(format.Substring(1));
            byte[] bytes = new byte[length];
            _random.NextBytes(bytes);
            return Convert.ToHexString(bytes).Substring(0, length);
        }
        return string.Empty;
    }
    private async Task<string> GenerateSequenceValueAsync(string? format, Guid inventoryId)
    {
        var count = await _context.Items.CountAsync(i => i.InventoryId == inventoryId);
        long nextValue = count + 1;
        if (format == null || format == "D") return nextValue.ToString();
        if (format.StartsWith("D") && format.Length > 1)
        {
            if (int.TryParse(format.Substring(1, 1), out int length))
            {
                return nextValue.ToString($"D{length}");
            }
        }
        return nextValue.ToString();
    }
}