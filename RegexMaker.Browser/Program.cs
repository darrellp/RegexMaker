using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using RegexMaker;

[assembly: SupportedOSPlatform("browser")]

internal sealed class Program
{
    private static Task Main(string[] args)
    {
        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .LogToTrace();
    }
}