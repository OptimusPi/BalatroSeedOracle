using System;
using BalatroSeedOracle.Controls;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a unique instance of an item with its own configuration
/// </summary>
public class ItemInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ItemKey { get; set; } = "";
    public ItemConfig? Config { get; set; }
    
    public ItemInstance(string itemKey, ItemConfig? config = null)
    {
        ItemKey = itemKey;
        Config = config;
    }
}