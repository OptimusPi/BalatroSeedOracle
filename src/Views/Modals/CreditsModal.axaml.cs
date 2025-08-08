using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Oracle.Views.Modals
{
    public partial class CreditsModal : UserControl
    {
        public CreditsModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}