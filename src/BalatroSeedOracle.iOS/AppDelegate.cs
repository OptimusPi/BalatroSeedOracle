using Avalonia;
using Avalonia.iOS;
using BalatroSeedOracle;
using Foundation;
using UIKit;

namespace BalatroSeedOracle.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }
}
