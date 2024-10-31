using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2Snake.Menu;

namespace CS2Snake
{
    public class Snake : BasePlugin
    {
        private Database _database;
        private SnakeGame _snakeGame;

        public override string ModuleName => "Snake";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "BoinK";
        public override string ModuleDescription => "Play snake!";

        public static Snake? Instance { get; set; }

        public override void Load(bool hotReload)
        {
            Instance ??= this;

            _database = new Database();
            _database.Initialize(ModuleDirectory);

           _snakeGame = new SnakeGame();
            _snakeGame.Load(this, hotReload, _database);
        }

        [ConsoleCommand("css_snake")]
        public void SnakeCommand(CCSPlayerController? player, CommandInfo? info)
        {
            SnakeGame.Start(player);
        }
    }
}