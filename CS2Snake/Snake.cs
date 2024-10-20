using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2Snake.Menu;

namespace CS2Snake
{
    public class Snake : BasePlugin
    {
        public override string ModuleName => "Snake";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "BoinK";
        public override string ModuleDescription => "Play snake!";

        public static Snake? Instance { get; set; }

        public override void Load(bool hotReload)
        {
            Instance ??= this;
            Controller.Load(this, hotReload);

            //Debug
            foreach (var player in Utilities.GetPlayers())
            {
                player.ExecuteClientCommandFromServer("css_snake");
            }
        }

        [ConsoleCommand("css_snake")]
        public void SnakeCommand(CCSPlayerController? player, CommandInfo? info)
        {
            var menu = SnakeManager.StartNewGame("Snake");
            SnakeManager.OpenMainMenu(player, menu);
        }
    }
}