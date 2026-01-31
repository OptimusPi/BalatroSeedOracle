using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;

namespace BalatroSeedOracle.Services
{
    public class FavoritesService
    {
        private static FavoritesService? _instance;
        public static FavoritesService Instance => _instance ??= new FavoritesService();

        private readonly string _favoritesPath;
        private FavoritesData _data = new FavoritesData();

        private FavoritesService()
        {
            _favoritesPath = Path.Combine(Helpers.AppPaths.DataRootDir, "favorites.json");

            LoadFavorites();
            InitializeDefaultSets();
        }

        private void LoadFavorites()
        {
            var platformServices = ServiceHelper.GetService<IPlatformServices>();
            if (platformServices is null || !platformServices.SupportsFileSystem)
            {
                // Browser: Skip file loading, use empty data
                _data = new FavoritesData();
                return;
            }
            try
            {
                if (File.Exists(_favoritesPath))
                {
                    var json = File.ReadAllText(_favoritesPath);
                    // AOT-compatible: Use source-generated serializer context
                    _data = JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.FavoritesData) ?? new FavoritesData();
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
                        Items = new List<string> { "photograph", "hangingchad" },
                        Tags = new List<string> { "#FaceCards", "#Mult", "#Synergy" },
                    },
                    new JokerSet
                    {
                        Name = "BaronMime",
                        Description = "Kings multiply and copy",
                        Items = new List<string> { "baron", "mime" },
                        Tags = new List<string> { "#Kings", "#Copy", "#XMult" },
                    },
                    new JokerSet
                    {
                        Name = "Double Vision",
                        Description = "Copy your best joker twice",
                        Items = new List<string> { "brainstorm", "blueprint" },
                        Tags = new List<string> { "#Copy", "#Legendary", "#Synergy" },
                    },
                    new JokerSet
                    {
                        Name = "Lucky",
                        Description = "Maximum luck manipulation",
                        Items = new List<string> { "oopsall6s", "luckycat", "themagician" },
                        Tags = new List<string> { "#Luck", "#Cat", "#LuckyCat", "#Tarot" },
                    },
                    new JokerSet
                    {
                        Name = "Legendary Lineup",
                        Description = "The legendary joker collection",
                        Items = new List<string>
                        {
                            "chicot",
                            "perkeo",
                            "triboulet",
                            "yorick",
                            "canio",
                        },
                        Tags = new List<string> { "#Legendary", "#LateGame", "#XMult" },
                    },
                    new JokerSet
                    {
                        Name = "ReTrigger",
                        Description = "Maximum retrigger synergy",
                        Items = new List<string>
                        {
                            "sockandbuskin",
                            "hangingchad",
                            "dusk",
                            "dejavu",
                        },
                        Tags = new List<string> { "#Retrigger", "#FaceCards", "#Spectral" },
                    },
                    new JokerSet
                    {
                        Name = "Money Makers",
                        Description = "Economic powerhouse combo",
                        Items = new List<string>
                        {
                            "businesscard",
                            "bull",
                            "tothemoon",
                            "bootstraps",
                            "egg",
                        },
                        Tags = new List<string> { "#Money", "#Gold", "#Economy" },
                    },
                    new JokerSet
                    {
                        Name = "HandSize",
                        Description = "Maximum hand size expansion",
                        Items = new List<string> { "turtlebean", "juggler", "troubadour" },
                        Tags = new List<string> { "#HandSize", "#Voucher", "#Utility" },
                    },
                    new JokerSet
                    {
                        Name = "Blueprint Bros",
                        Description = "Copy and enhance strategy",
                        Items = new List<string> { "blueprint", "brainstorm", "showman", "dna" },
                        Tags = new List<string> { "#Copy", "#Synergy", "#LateGame" },
                    },
                    new JokerSet
                    {
                        Name = "Economy",
                        Description = "Build your gold empire",
                        Items = new List<string>
                        {
                            "goldenticket",
                            "thedevil",
                            "businesscard",
                            "tradingcard",
                            "reservedparking",
                            "facelessjoker",
                        },
                        Tags = new List<string> { "#Gold", "#Economy", "#Scaling", "#Tarot" },
                    },
                };
                SaveFavorites();
            }
        }

        /// <summary>
        /// Save favorites to disk. Fire-and-forget - handles errors internally.
        /// </summary>
        public void SaveFavorites()
        {
            Task.Run(async () =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(_data, BsoJsonSerializerContext.Default.FavoritesData);
                    await File.WriteAllTextAsync(_favoritesPath, json);
                    DebugLogger.Log("FavoritesService", "Favorites saved successfully");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FavoritesService", $"Failed to save favorites: {ex.Message}");
                }
            });
        }

        public List<string> GetFavoriteItems()
        {
            return new List<string>(_data.FavoriteItems);
        }

        public void SetFavoriteItems(List<string> items)
        {
            _data.FavoriteItems = new List<string>(items);
            SaveFavorites();
        }

        public void AddFavoriteItem(string item)
        {
            if (!_data.FavoriteItems.Contains(item))
            {
                _data.FavoriteItems.Add(item);
                SaveFavorites();
            }
        }

        public void RemoveFavoriteItem(string item)
        {
            if (_data.FavoriteItems.Remove(item))
            {
                SaveFavorites();
            }
        }

        public List<JokerSet> GetCommonSets()
        {
            return new List<JokerSet>(_data.CommonSets);
        }

        public void AddCustomSet(JokerSet set)
        {
            _data.CommonSets.Add(set);
            SaveFavorites();
        }

        public void RemoveSet(string setName)
        {
            _data.CommonSets.RemoveAll(s => s.Name == setName);
            SaveFavorites();
        }
    }
}
