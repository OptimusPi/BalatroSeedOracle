using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the EditionSelector control.
    /// Manages card edition selection with visual effects and descriptions.
    /// </summary>
    public class EditionSelectorViewModel : BaseViewModel
    {
        private EditionOptionViewModel? _selectedEdition;
        private string _description = string.Empty;
        private bool _isPreviewVisible;
        private string _previewEmoji = string.Empty;
        private string _previewName = string.Empty;
        private string _previewEffect = string.Empty;
        private string _previewColor = string.Empty;
        private string _borderColor = string.Empty;
        private double _borderThickness;
        private string _backgroundColor = string.Empty;

        public EditionSelectorViewModel()
        {
            InitializeEditions();

            // Set default selection (Any Edition)
            if (Editions.Count > 0)
            {
                SelectedEdition = Editions[0];
            }
        }

        #region Properties

        public ObservableCollection<EditionOptionViewModel> Editions { get; } = new();

        public EditionOptionViewModel? SelectedEdition
        {
            get => _selectedEdition;
            set
            {
                if (SetProperty(ref _selectedEdition, value))
                {
                    UpdateDisplay();
                    RaiseEditionChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            private set => SetProperty(ref _description, value);
        }

        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            private set => SetProperty(ref _isPreviewVisible, value);
        }

        public string PreviewEmoji
        {
            get => _previewEmoji;
            private set => SetProperty(ref _previewEmoji, value);
        }

        public string PreviewName
        {
            get => _previewName;
            private set => SetProperty(ref _previewName, value);
        }

        public string PreviewEffect
        {
            get => _previewEffect;
            private set => SetProperty(ref _previewEffect, value);
        }

        public string PreviewColor
        {
            get => _previewColor;
            private set => SetProperty(ref _previewColor, value);
        }

        public string BorderColor
        {
            get => _borderColor;
            private set => SetProperty(ref _borderColor, value);
        }

        public double BorderThickness
        {
            get => _borderThickness;
            private set => SetProperty(ref _borderThickness, value);
        }

        public string BackgroundColor
        {
            get => _backgroundColor;
            private set => SetProperty(ref _backgroundColor, value);
        }

        #endregion

        #region Events

        public event EventHandler<string>? EditionChanged;

        #endregion

        #region Initialization

        private void InitializeEditions()
        {
            Editions.Add(new EditionOptionViewModel("", "Any Edition", "⚪", "Card can have any edition type", "#CCCCCC"));
            Editions.Add(new EditionOptionViewModel("Normal", "⚪ Normal", "⚪", "No special effect - base card", "#FFFFFF"));
            Editions.Add(new EditionOptionViewModel("Foil", "✨ Foil (+50 chips)", "✨", "+50 chips when scored", "#C0C0C0"));
            Editions.Add(new EditionOptionViewModel("Holographic", "🌈 Holographic (+10 mult)", "🌈", "+10 mult when scored", "#FF69B4"));
            Editions.Add(new EditionOptionViewModel("Polychrome", "🎭 Polychrome (x1.5 mult)", "🎭", "x1.5 mult when scored", "#FF4500"));
            Editions.Add(new EditionOptionViewModel("Negative", "🖤 Negative (+1 joker slot)", "🖤", "+1 joker slot (permanent)", "#8B008B"));
        }

        #endregion

        #region Private Methods

        private void UpdateDisplay()
        {
            if (_selectedEdition == null)
            {
                Description = "Unknown edition";
                IsPreviewVisible = false;
                return;
            }

            Description = _selectedEdition.Description;

            if (string.IsNullOrEmpty(_selectedEdition.Tag))
            {
                IsPreviewVisible = false;
                ResetBorderEffects();
            }
            else
            {
                IsPreviewVisible = true;
                PreviewEmoji = _selectedEdition.Emoji;
                PreviewName = _selectedEdition.Name;
                PreviewEffect = _selectedEdition.Effect;
                PreviewColor = _selectedEdition.Color;

                ApplyEditionEffects(_selectedEdition.Tag);
            }
        }

        private void ApplyEditionEffects(string edition)
        {
            switch (edition)
            {
                case "Foil":
                    BorderColor = "#C0C0C0";
                    BorderThickness = 2;
                    BackgroundColor = "Transparent";
                    break;

                case "Holographic":
                    BorderColor = "#FF69B4";
                    BorderThickness = 3;
                    BackgroundColor = "Transparent";
                    break;

                case "Polychrome":
                    BorderColor = "#FF4500";
                    BorderThickness = 3;
                    BackgroundColor = "Transparent";
                    break;

                case "Negative":
                    BorderColor = "#8B008B";
                    BorderThickness = 2;
                    BackgroundColor = "#22000022";
                    break;

                default:
                    ResetBorderEffects();
                    break;
            }
        }

        private void ResetBorderEffects()
        {
            BorderColor = "#444444";
            BorderThickness = 1;
            BackgroundColor = "Transparent";
        }

        private void RaiseEditionChanged()
        {
            EditionChanged?.Invoke(this, _selectedEdition?.Tag ?? "");
        }

        #endregion

        #region Public Methods

        public string GetSelectedEdition()
        {
            return _selectedEdition?.Tag ?? "";
        }

        public void SetSelectedEdition(string edition)
        {
            var editionOption = Editions.FirstOrDefault(e => e.Tag == edition);
            if (editionOption != null)
            {
                SelectedEdition = editionOption;
            }
        }

        public static string GetEditionDisplayName(string edition)
        {
            return edition switch
            {
                "" => "Any Edition",
                "Normal" => "Normal",
                "Foil" => "Foil (+50 chips)",
                "Holographic" => "Holographic (+10 mult)",
                "Polychrome" => "Polychrome (x1.5 mult)",
                "Negative" => "Negative (+1 joker slot)",
                _ => "Unknown"
            };
        }

        public static string[] GetAllEditions()
        {
            return new[]
            {
                "",
                "Normal",
                "Foil",
                "Holographic",
                "Polychrome",
                "Negative"
            };
        }

        public static int GetEditionPowerLevel(string edition)
        {
            return edition switch
            {
                "Normal" => 1,
                "Foil" => 2,
                "Holographic" => 3,
                "Polychrome" => 4,
                "Negative" => 5,
                _ => 0
            };
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for an edition option
    /// </summary>
    public class EditionOptionViewModel
    {
        public EditionOptionViewModel(string tag, string displayName, string emoji, string description, string color)
        {
            Tag = tag;
            DisplayName = displayName;
            Emoji = emoji;
            Description = description;
            Effect = description;
            Color = color;
            Name = tag == "" ? "Any Edition" : tag;
        }

        public string Tag { get; }
        public string DisplayName { get; }
        public string Emoji { get; }
        public string Description { get; }
        public string Effect { get; }
        public string Color { get; }
        public string Name { get; }
    }
}
