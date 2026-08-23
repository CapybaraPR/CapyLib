using Exiled.API.Features;

namespace Capy.Patches;

public static class ServerBanner {
    public static void Show() {
        string[] hints = [
            "504 Gateway Time-out",
            "When you build a shell, build an army in your mind",
            "Made with vscode",
        ];

        Log.Info("This server is powered by: \n" +
        @" ╔════╗  ╔════╗ ╔════╗ ╔════╗ ╔════╗ ╔════╗╔╗    ╔╗ ╔╗      " + "\n" +
        @" ║╔══╗║  ║╔═══╝ ║╔══╗║ ║╔═══╝ ║╔═══╝ ╚══╗═╝║║    ╠╣ ║╚══╗   " + "\n" +
        @" ║╚══╝║  ║╚═══╗ ║╚══╝║ ║║═══╗ ║║      ║═║  ║║    ║║ ║╔══╗║  " + "\n" +
        @" ║╔══╗║  ╚═══╗║ ║╔═══╝ ║║═══╝ ║║      ║═║  ║║    ║║ ║║  ║║  " + "\n" +
        @" ║║  ║║  ╔═══╝║ ║║     ║╚═══╗ ║╚═══╗  ║═║  ║╚═══╗║║ ║╚══╝║  " + "\n" +
        @" ╚╝  ╚╝  ╚════╝ ╚╝     ╚════╝ ╚════╝  ╚═╝  ╚════╝╚╝ ╚════╝  " + "\n" +
        $"{hints.RandomItem()}");
    }
}
