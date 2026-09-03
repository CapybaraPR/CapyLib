namespace Capy.Core.Services;

/// <summary>
/// Фирменный загрузочный баннер сервера CapyLib в классическом стиле AspectLib.
/// </summary>
public static class ServerBanner
{
    private static readonly string[] Hints = new[]
    {
        "Capybara is love, Capybara is life",
        "Keep calm and pet the Capybara",
        "When you build a server, build a family in your mind",
        "Powered by Capybara Framework",
        "Made with love for Capybara servers",
    };

    public static void Show()
    {
        var random = new Random();
        string randomHint = Hints[random.Next(Hints.Length)];

        Log.Debug("This server is powered by: \n" +
        @" ╔════╗  ╔════╗ ╔════╗ ╔╗  ╔╗ ╔╗    ╔╗ ╔════╗ " + "\n" +
        @" ║╔═══╝  ║╔══╗║ ║╔══╗║ ║║  ║║ ║║    ╠╣ ║╔══╗║ " + "\n" +
        @" ║║      ║╚══╝║ ║╚══╝║ ║╚══╝║ ║║    ║║ ║╚══╝║ " + "\n" +
        @" ║║      ║╔══╗║ ║╔═══╝ ╚══╗╔╝ ║║    ║║ ║╔══╗║ " + "\n" +
        @" ║╚═══╗  ║║  ║║ ║║        ║║  ║╚═══╗║║ ║╚══╝║ " + "\n" +
        @" ╚════╝  ╚╝  ╚╝ ╚╝        ╚╝  ╚════╝╚╝ ╚════╝ " + "\n" +
        $"{randomHint}");
    }
}
