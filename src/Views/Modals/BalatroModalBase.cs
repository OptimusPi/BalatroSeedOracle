using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public class BalatroModalBase : UserControl
    {
        public event EventHandler? BackClicked;

        protected virtual void OnBackClicked()
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        }

        public BalatroModalBase()
        {
            this.AttachedToVisualTree += (s, e) =>
            {
                var backBtn = this.FindControl<Button>("BackButton");
                if (backBtn != null)
                {
                    backBtn.Click -= BackButton_Click;
                    backBtn.Click += BackButton_Click;
                }
            };
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
