using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Motely;
using BalatroSeedOracle.Models;

// Quick script to generate mega filter with all jokers
var jokers = BalatroData.Jokers
    .Where(j => !j.Key.StartsWith("any")) // Skip wildcards
    .Select(j => new
    {
        score = 1,
        type = "Joker",
        value = j.Key,
        antes = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }
    })
    .ToList();

var filter = new
{
    name = "AllJokers",
    description = $"Mega filter with ALL {jokers.Count} jokers - 1 point each",
    author = "Generated",
    must = new object[] { },
    should = jokers,
    mustNot = new object[] { },
    deck = "Red",
    stake = "White"
};

var json = JsonSerializer.Serialize(filter, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

File.WriteAllText(@"X:\BalatroSeedOracle\JsonItemFilters\AllJokers.json", json);
Console.WriteLine($"Created AllJokers.json with {jokers.Count} jokers!");