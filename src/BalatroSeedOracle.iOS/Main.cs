using System.Runtime.Versioning;
using UIKit;

namespace BalatroSeedOracle.iOS
{
    [SupportedOSPlatform("ios")]
    public class Application
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
