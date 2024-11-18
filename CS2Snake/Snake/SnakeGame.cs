using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2Snake.Menu;

public class SnakeGame
{

    public static readonly Dictionary<int, GameState> Players = [];
    public static int GameSpeed = 100;

    public void Load(BasePlugin plugin, bool hotReload, Database database, Config config)
    {
        GameSpeed = config.GameSpeed;

        plugin.RegisterEventHandler<EventPlayerActivate>((@event, info) =>
        {
            if (@event.Userid != null)
                Players[@event.Userid.Slot] = new GameState
                {
                    Database = database,
                    _player = @event.Userid,
                    Buttons = 0
                };
            return HookResult.Continue;
        });

        plugin.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid != null) Players.Remove(@event.Userid.Slot);
            return HookResult.Continue;
        });

        plugin.RegisterListener<Listeners.OnTick>(OnTick);

        if (hotReload)
            foreach (var pl in Utilities.GetPlayers())
            {
                Players[pl.Slot] = new GameState
                {
                    Database = database,
                    _player = pl,
                    Buttons = pl.Buttons
                };
            }
    }

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100); // delay between updates
    private static DateTime lastUpdate = DateTime.Now;

    public static void OnTick()
    {
        DateTime now = DateTime.Now;
        TimeSpan delta = now - lastUpdate;

        foreach (var game in Players.Values.Where(p => p.GameRunning))
        {
            // Render the updated HTML to the player's screen if not empty
            if (!string.IsNullOrEmpty(game.CenterHtml))
            {
                Server.NextFrame(() =>
                    game._player.PrintToCenterHtml(game.CenterHtml)
                );
            }

            // Always handle input, even if not updating game state
            if ((game.Buttons & PlayerButtons.Forward) == 0 && (game._player.Buttons & PlayerButtons.Forward) != 0)
            {
                game.Up();
            }
            else if ((game.Buttons & PlayerButtons.Back) == 0 && (game._player.Buttons & PlayerButtons.Back) != 0)
            {
                game.Down();
            }
            else if ((game.Buttons & PlayerButtons.Moveleft) == 0 && (game._player.Buttons & PlayerButtons.Moveleft) != 0)
            {
                game.Left();
            }
            else if ((game.Buttons & PlayerButtons.Moveright) == 0 && (game._player.Buttons & PlayerButtons.Moveright) != 0)
            {
                game.Right();
            }
            else if ((game.Buttons & PlayerButtons.Reload) == 0 && (game._player.Buttons & PlayerButtons.Reload) != 0)
            {
                game.Retry();
            }

            // Check if menu should be closed
            if (((long)game._player.Buttons & 8589934592) == 8589934592)
            {
                game.CloseGame();
            }

            // Only proceed with game logic if enough time has passed (for throttling)
            if (delta >= UpdateInterval)
            {
                // Update the last update time
                lastUpdate = now;

                // Update the game screen (center HTML)
                game.UpdateCenterHtml();

                // Sync player's buttons with the game state
                game.Buttons = game._player.Buttons;
            }
        }
    }

    public static void Start(CCSPlayerController? player)
    {
        if (player == null) return;
        Players[player.Slot].StartGame();
    }

    public static void ShowHighscores(CCSPlayerController? player)
    {
        if (player == null) return;
        Players[player.Slot].StartGame();
        Players[player.Slot].ShowHighscores();
    }
}
