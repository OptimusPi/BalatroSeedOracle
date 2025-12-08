using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BalatroSeedOracle;

namespace BalatroSeedOracle.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
