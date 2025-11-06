using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace BalatroSeedOracle.Models
{
    public class SelectableItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _type = "";
        private string _category = "";
        private bool _isSelected;
        private IImage? _itemImage;
        private string _displayName = "";
        private string _itemKey = "";
        private bool _isBeingDragged;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public IImage? ItemImage
        {
            get => _itemImage;
            set
            {
                if (_itemImage != value)
                {
                    _itemImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public IImage? SoulFaceImage
        {
            get
            {
                if (_type == "SoulJoker")
                {
                    return Services.SpriteService.Instance.GetJokerSoulImage(_name);
                }
                return null;
            }
        }

        public bool IsFavorite { get; set; }

        public string DisplayName
        {
            get => string.IsNullOrEmpty(_displayName) ? _name : _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemKey
        {
            get => string.IsNullOrEmpty(_itemKey) ? $"{_type}_{_name}" : _itemKey;
            set
            {
                if (_itemKey != value)
                {
                    _itemKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemType => _type;

        public bool IsBeingDragged
        {
            get => _isBeingDragged;
            set
            {
                if (_isBeingDragged != value)
                {
                    _isBeingDragged = value;
                    OnPropertyChanged();
                }
            }
        }

        // Additional properties for filter configuration
        public string? Value { get; set; }
        public string? Label { get; set; }
        public int[]? Antes { get; set; }
        public string? Edition { get; set; }
        public bool IncludeBoosterPacks { get; set; }
        public bool IncludeShopStream { get; set; }
        public bool IncludeSkipTags { get; set; }

        // Playing card properties
        public string? Rank { get; set; }
        public string? Suit { get; set; }
        public string? Enhancement { get; set; }
        public string? Seal { get; set; }

        // Animation properties
        // Used for staggered flip animations - each card gets a unique delay
        public int StaggerDelay { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
