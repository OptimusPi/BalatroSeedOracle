using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the AnteSelector control.
    /// Manages ante selection state with quick selection commands.
    /// </summary>
    public class AnteSelectorViewModel : BaseViewModel
    {
        private string _selectionSummary = string.Empty;

        public AnteSelectorViewModel()
        {
            // Initialize ante checkboxes
            for (int i = 1; i <= 8; i++)
            {
                var ante = new AnteItemViewModel(i);
                ante.IsSelectedChanged += OnAnteSelectionChanged;

                // Default: select first 4 antes
                ante.IsSelected = i <= 4;

                Antes.Add(ante);
            }

            InitializeCommands();
            UpdateSelectionSummary();
        }

        #region Properties

        public ObservableCollection<AnteItemViewModel> Antes { get; } = new();

        public string SelectionSummary
        {
            get => _selectionSummary;
            private set => SetProperty(ref _selectionSummary, value);
        }

        #endregion

        #region Commands

        public ICommand SelectAllCommand { get; private set; } = null!;
        public ICommand SelectNoneCommand { get; private set; } = null!;
        public ICommand SelectEarlyCommand { get; private set; } = null!;
        public ICommand SelectLateCommand { get; private set; } = null!;

        #endregion

        #region Events

        public event EventHandler<int[]>? SelectedAntesChanged;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand(SelectAll);
            SelectNoneCommand = new RelayCommand(SelectNone);
            SelectEarlyCommand = new RelayCommand(SelectEarly);
            SelectLateCommand = new RelayCommand(SelectLate);
        }

        #endregion

        #region Command Implementations

        private void SelectAll()
        {
            foreach (var ante in Antes)
            {
                ante.IsSelected = true;
            }
        }

        private void SelectNone()
        {
            foreach (var ante in Antes)
            {
                ante.IsSelected = false;
            }
        }

        private void SelectEarly()
        {
            // Clear all first
            foreach (var ante in Antes)
            {
                ante.IsSelected = false;
            }

            // Select antes 1-3
            for (int i = 0; i < 3 && i < Antes.Count; i++)
            {
                Antes[i].IsSelected = true;
            }
        }

        private void SelectLate()
        {
            // Clear all first
            foreach (var ante in Antes)
            {
                ante.IsSelected = false;
            }

            // Select antes 4-8
            for (int i = 3; i < Antes.Count; i++)
            {
                Antes[i].IsSelected = true;
            }
        }

        #endregion

        #region Event Handlers

        private void OnAnteSelectionChanged(object? sender, EventArgs e)
        {
            UpdateSelectionSummary();
            RaiseSelectionChanged();
        }

        #endregion

        #region Private Methods

        private void UpdateSelectionSummary()
        {
            var selectedAntes = GetSelectedAntes();

            if (selectedAntes.Length == 0)
            {
                SelectionSummary = "Selected: None";
            }
            else if (selectedAntes.Length == 8)
            {
                SelectionSummary = "Selected: All Antes";
            }
            else
            {
                var anteList = string.Join(", ", selectedAntes);
                SelectionSummary = $"Selected: Antes {anteList}";
            }
        }

        private void RaiseSelectionChanged()
        {
            SelectedAntesChanged?.Invoke(this, GetSelectedAntes());
        }

        #endregion

        #region Public Methods

        public int[] GetSelectedAntes()
        {
            return Antes
                .Where(a => a.IsSelected)
                .Select(a => a.AnteNumber)
                .ToArray();
        }

        public void SetSelectedAntes(int[] antes)
        {
            // Clear all first
            foreach (var ante in Antes)
            {
                ante.IsSelected = false;
            }

            // Set selected ones
            foreach (var anteNum in antes)
            {
                var ante = Antes.FirstOrDefault(a => a.AnteNumber == anteNum);
                if (ante != null)
                {
                    ante.IsSelected = true;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for an individual ante checkbox
    /// </summary>
    public class AnteItemViewModel : BaseViewModel
    {
        private bool _isSelected;

        public AnteItemViewModel(int anteNumber)
        {
            AnteNumber = anteNumber;
        }

        public int AnteNumber { get; }

        public string DisplayText => $"Ante {AnteNumber}";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    IsSelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? IsSelectedChanged;
    }
}
