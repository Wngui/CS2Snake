using CounterStrikeSharp.API.Core;

namespace CS2Snake.Menu;

public static class SnakeManager
{
    public static void OpenMainMenu(CCSPlayerController? player, GameState? gameState)
    {
        if (player == null)
            return;
        Controller.Players[player.Slot].OpenGame(gameState);
    }

    public static void CloseGame(CCSPlayerController? player)
    {
        if (player == null)
            return;
        Controller.Players[player.Slot].OpenGame(null);
    }

    public static GameState StartNewGame(string title = "")
    {
        GameState menu = new()
        {
            Title = title,
        };
        return menu;
    }
}