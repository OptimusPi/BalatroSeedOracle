using System;
using System.Collections.ObjectModel;
using System.Linq;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.Widgets
{
    /// <summary>
    /// ViewModel for managing docking zones and visual feedback
    /// </summary>
    public partial class DockZoneViewModel : ObservableObject
    {
        private readonly IDockingService _dockingService;

        /// <summary>
        /// Collection of available dock zones
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DockZone> _zones = new();

        /// <summary>
        /// Currently active dock zone
        /// </summary>
        [ObservableProperty]
        private DockZone? _activeZone = null;

        /// <summary>
        /// Whether dock zones should be visible
        /// </summary>
        [ObservableProperty]
        private bool _showZones = false;

        public DockZoneViewModel(IDockingService dockingService)
        {
            _dockingService = dockingService ?? throw new ArgumentNullException(nameof(dockingService));

            // Subscribe to docking service events
            _dockingService.DockZonesRequested += OnDockZonesRequested;
            _dockingService.DockZonesHidden += OnDockZonesHidden;
        }

        /// <summary>
        /// Show dock zones
        /// </summary>
        [RelayCommand]
        public void ShowDockZones()
        {
            ShowZones = true;
        }

        /// <summary>
        /// Hide dock zones
        /// </summary>
        [RelayCommand]
        public void HideDockZones()
        {
            ShowZones = false;
            ActiveZone = null;
            
            // Clear all zone highlights
            foreach (var zone in Zones)
            {
                zone.ClearHighlight();
            }
        }

        /// <summary>
        /// Activate a specific dock zone
        /// </summary>
        [RelayCommand]
        public void ActivateZone(DockPosition position)
        {
            var zone = Zones.FirstOrDefault(z => z.Position == position);
            if (zone != null)
            {
                // Clear previous active zone
                if (ActiveZone != null)
                    ActiveZone.ClearHighlight();

                ActiveZone = zone;
                zone.Highlight();
            }
        }

        private void OnDockZonesRequested(object? sender, DockZonesEventArgs e)
        {
            Zones.Clear();
            foreach (var zone in e.DockZones)
            {
                Zones.Add(zone);
            }
            ShowZones = true;
        }

        private void OnDockZonesHidden(object? sender, EventArgs e)
        {
            ShowZones = false;
            ActiveZone = null;
            Zones.Clear();
        }
    }
}