using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.Helpers;

namespace Oracle.Services
{
    public class FavoritesService
    {
        private static FavoritesService? _instance;
        public static FavoritesService Instance => _instance ??= new FavoritesService();
        
        private readonly string _favoritesPath;
        private FavoritesData _data = new FavoritesData();
        
        public class FavoritesData
        {
            public List<string> FavoriteItems { get; set; } = new List<string>();
            public List<JokerSet> CommonSets { get; set; } = new List<JokerSet>();
        }
        
        public class JokerSet
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Items { get; set; } = new List<string>(); // Can be jokers, tarots, etc
            public List<string> Tags { get; set; } = new List<string>();
            
            // Backwards compatibility
            public List<string> Jokers 
            { 
                get => Items; 
                set => Items = value; 
            }
        }
        
        private FavoritesService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BalatroSeedOracle"
            );
            
            Directory.CreateDirectory(appDataPath);
            _favoritesPath = Path.Combine(appDataPath, "favorites.json");
            
            LoadFavorites();
            InitializeDefaultSets();
        }
        
        private void LoadFavorites()
        {
            try
            {
                if (File.Exists(_favoritesPath))
                {
                    var json = File.ReadAllText(_favoritesPath);
                    _data = JsonSerializer.Deserialize<FavoritesData>(json) ?? new FavoritesData();
                }
                else
                {
                    _data = new FavoritesData();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FavoritesService", $"Failed to load favorites: {ex.Message}");
                _data = new FavoritesData();
            }
        }
        
        private void InitializeDefaultSets()
        {
            if (_data.CommonSets.Count == 0)
            {
                _data.CommonSets = new List<JokerSet>
                {
                    new JokerSet
                    {
                        Name = "Photo Chad",
                        Description = "Picture perfect combo",
                        Jokers = new List<string> { "Photograph", "HangingChad" },
                        Tags = new List<string> { "#FaceCards", "#Mult", "#Synergy" }
                    },
                    new JokerSet
                    {
                        Name = "BaronMime",
                        Description = "Kings multiply and copy",
                        Jokers = new List<string> { "Baron", "Mime" },
                        Tags = new List<string> { "#Kings", "#Copy", "#XMult" }
                    },
                    new JokerSet
                    {
                        Name = "Double Vision",
                        Description = "Copy your best joker twice",
                        Jokers = new List<string> { "Brainstorm", "Blueprint" },
                        Tags = new List<string> { "#Copy", "#Legendary", "#Synergy" }
                    },
                    new JokerSet
                    {
                        Name = "Lucky",
                        Description = "Maximum luck manipulation",
                        Jokers = new List<string> { "OopsAll6s", "LuckyCat" },
                        Items = new List<string> { "TheMagician" },
                        Tags = new List<string> { "#Luck", "#Cat", "#LuckyCat", "#Tarot" }
                    },
                    new JokerSet
                    {
                        Name = "Legendary Lineup",
                        Description = "The legendary joker collection",
                        Jokers = new List<string> { "Chicot", "Perkeo", "Triboulet", "Yorick", "Canio" },
                        Tags = new List<string> { "#Legendary", "#LateGame", "#XMult" }
                    },
                    new JokerSet
                    {
                        Name = "ReTrigger",
                        Description = "Maximum retrigger synergy",
                        Jokers = new List<string> { "Sock and Buskin", "Hanging Chad", "Dusk", "Deja Vu" },
                        Tags = new List<string> { "#Retrigger", "#FaceCards", "#Spectral" }
                    },
                    new JokerSet
                    {
                        Name = "Money Makers",
                        Description = "Economic powerhouse combo",
                        Jokers = new List<string> { "Business Card", "Bull", "To the Moon", "Bootstraps", "Egg" },
                        Tags = new List<string> { "#Money", "#Gold", "#Economy" }
                    },
                    new JokerSet
                    {
                        Name = "HandSize",
                        Description = "Maximum hand size expansion",
                        Jokers = new List<string> { "Turtle Bean", "Juggler", "Paint Brush", "Palette", "Troubadour" },
                        Tags = new List<string> { "#HandSize", "#Voucher", "#Utility" }
                    },
                    new JokerSet
                    {
                        Name = "Blueprint Bros",
                        Description = "Copy and enhance strategy",
                        Jokers = new List<string> { "Blueprint", "Brainstorm", "Showman", "DNA" },
                        Tags = new List<string> { "#Copy", "#Synergy", "#LateGame" }
                    },
                    new JokerSet
                    {
                        Name = "Economy",
                        Description = "Build your gold empire",
                        Jokers = new List<string> { "Golden Ticket", "The Devil", "Business Card", "Trading Card", "Reserved Parking", "Faceless Joker" },
                        Tags = new List<string> { "#Gold", "#Economy", "#Scaling", "#Tarot" }
                    }
                };
                _ = SaveFavorites();
            }
        }
        
        public async Task SaveFavorites()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_favoritesPath, json);
                DebugLogger.Log("FavoritesService", "Favorites saved successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FavoritesService", $"Failed to save favorites: {ex.Message}");
            }
        }
        
        public List<string> GetFavoriteItems()
        {
            return new List<string>(_data.FavoriteItems);
        }
        
        public void SetFavoriteItems(List<string> items)
        {
            _data.FavoriteItems = new List<string>(items);
            _ = SaveFavorites(); // Fire and forget
        }
        
        public void AddFavoriteItem(string item)
        {
            if (!_data.FavoriteItems.Contains(item))
            {
                _data.FavoriteItems.Add(item);
                _ = SaveFavorites();
            }
        }
        
        public void RemoveFavoriteItem(string item)
        {
            if (_data.FavoriteItems.Remove(item))
            {
                _ = SaveFavorites();
            }
        }
        
        public List<JokerSet> GetCommonSets()
        {
            return new List<JokerSet>(_data.CommonSets);
        }
        
        public void AddCustomSet(JokerSet set)
        {
            _data.CommonSets.Add(set);
            _ = SaveFavorites();
        }
        
        public void RemoveSet(string setName)
        {
            _data.CommonSets.RemoveAll(s => s.Name == setName);
            _ = SaveFavorites();
        }
    }
}