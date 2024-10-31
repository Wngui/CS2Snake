﻿using System.Drawing;
using System.Numerics;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Snake.Model;

namespace CS2Snake.Menu;

public class GameState
{
    public CCSPlayerController _player { get; set; }
    public PlayerButtons Buttons { get; set; }
    public string? CenterHtml { get; private set; }
    public bool GameRunning { get; set; } = false;
    public Database Database { get; init; }

    private List<(int X, int Y)> snake;
    private (int X, int Y) direction;
    private (int X, int Y) food;
    private readonly int gridWidth = 19;
    private readonly int gridHeight = 6;
    private Random random;
    private bool gameOver = false;
    private bool showingGameOver = false;
    private int _score;


    public GameState()
    {
        random = new Random();
        ResetGame();
    }

    // Initialize the game
    private void ResetGame()
    {
        int startX = gridWidth / 2;
        int startY = gridHeight / 2;

        // Initialize the snake with 3 parts, moving left
        snake =
        [
            (startX, startY),       // Head (center)
            (startX + 1, startY),   // Second part (right of the head)
            (startX + 2, startY)    // Third part (right of the second part)
        ];

        direction = (-1, 0); // Start moving left
        food = GenerateFoodPosition();
        gameOver = false;
        showingGameOver = false;
        _score = 0; // Reset score
    }


    private (int X, int Y) GenerateFoodPosition()
    {
        int foodX, foodY;
        do
        {
            foodX = random.Next(0, gridWidth);
            foodY = random.Next(0, gridHeight);
        } while (snake.Contains((foodX, foodY))); // Ensure food isn't placed on the snake
        return (foodX, foodY);
    }

    public void CloseGame()
    {
        GameRunning = false;
        _player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(_player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
        Utilities.SetStateChanged(_player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
    }

    // Called when the player opens the menu
    public void StartGame()
    {
        ResetGame();
        if (_player.PlayerPawn.Value != null && _player.PlayerPawn.Value.IsValid)
        {
            _player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
            Schema.SetSchemaValue(_player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
            Utilities.SetStateChanged(_player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
        }

        GameRunning = true;
        UpdateCenterHtml();
    }

    public void Down()
    {
        if (direction != (0, -1)) // Prevent reversing direction
            direction = (0, 1);
    }

    public void Up()
    {
        if (direction != (0, 1))
            direction = (0, -1);
    }

    public void Left()
    {
        if (direction != (1, 0))
            direction = (-1, 0);
    }

    public void Right()
    {
        if (direction != (-1, 0))
            direction = (1, 0);
    }

    internal void Retry()
    {
        StartGame();
    }

    // This function updates the game state and renders the grid
    public void UpdateCenterHtml()
    {
        if (showingGameOver) return;
        if (gameOver)
        {
            ShowGameOver();
            return;
        }

        MoveSnake();

        StringBuilder builder = new();
        builder.AppendLine($"<font class='{FontSizes.FontSizeM}' color='lightgreen'>Snake</font><font class='{FontSizes.FontSizeSm}' color='white'> - Score: {_score}</font>"); // Updated to display score

        // Render the grid
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (snake.Contains((x, y)))
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.LightGreen.Name}'>█</font>");
                }
                else if (food == (x, y))
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.Red.Name}'>█</font>");
                }
                else
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.DimGray.Name}'>█</font>");
                }
            }
            builder.AppendLine();

            if (y == gridHeight - 1) { builder.Append('_'); } // Fixes a layout issue
        }
        CenterHtml = builder.ToString();
    }

    private void ShowGameOver()
    {
        showingGameOver = true;

        if (_player.IsValid)
        {
            //Save Highscore
            Database.SaveHighScore(new HighScore() { SteamId = _player.SteamID, Score = _score, Name = _player.PlayerName });
        }

        StringBuilder builder = new();
        //List player score
        builder.Append($"<font color='red'>Game Over</font><br><font class='{FontSizes.FontSizeSm}' color='white'>Your Score: </font><font class='{FontSizes.FontSizeSm}' color='gold'>{_score}</font>");
        builder.Append("<br>"); 
        AppendHighscores(builder);

        CenterHtml = builder.ToString();
        PrintScoreToChat();
    }

    private void AppendHighscores(StringBuilder builder)
    {
        // Define maximum name length for alignment purposes
        const int maxNameLength = 13;

        // List high scores
        var highScores = Database.GetHighScores();

        int rank = 1; // Start rank at 1

        // Display two high scores per row
        for (int i = 0; i < highScores.Count; i += 2)
        {
            // First score
            string name1 = highScores[i].Name.Length > maxNameLength
                ? highScores[i].Name.Substring(0, maxNameLength)
                : highScores[i].Name;

            var playerColor = highScores[i].SteamId == _player.SteamID ? Color.Orange : Color.White;

            builder.Append("<center>");

            // Display rank in dark grey
            builder.Append($"</center><font color='lightgreen' class='{FontSizes.FontSizeS}'>{rank}. </font><font class='{FontSizes.FontSizeS}' color='{playerColor.Name}'>{name1}: </font><font class='{FontSizes.FontSizeS}' color='gold'>{highScores[i].Score}</font>");
            // Increment rank for next score
            rank++;

            // Second score, if it exists
            if (i + 1 < highScores.Count)
            {
                string name2 = highScores[i + 1].Name.Length > maxNameLength
                    ? highScores[i + 1].Name.Substring(0, maxNameLength)
                    : highScores[i + 1].Name;

                playerColor = highScores[i+1].SteamId == _player.SteamID ? Color.Orange : Color.White;

                //Calculate padding
                var padding = Math.Clamp(maxNameLength - name1.Length, 4, maxNameLength);
                for (int j = 0; j < padding; j++)
                {
                    builder.Append("&nbsp;");
                }

                // Display rank for the second score in dark grey
                builder.Append($"<font color='lightgreen' class='{FontSizes.FontSizeS}'>{rank}. </font><font class='{FontSizes.FontSizeS}' color='{playerColor.Name}'>{name2}: </font><font class='{FontSizes.FontSizeS}' color='gold'>{highScores[i + 1].Score}</font>&#9;");

                // Increment rank for next iteration
                rank++;
            }

            builder.Append("</center>");
            builder.Append("<br>"); // New line after two pairs
        }

        // Footer for retry/exit options
        builder.Append("<center><font color='green' class='fontSize-sm'>Retry: </font><font color='orange' class='fontSize-s'>[R]</font><font color='white' class='fontSize-sm'> | </font><font color='red' class='fontSize-sm'>Exit: </font><font color='orange' class='fontSize-s'>[Tab]</font></center>");
    }


    private void PrintScoreToChat()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot) continue;
            player.PrintToChat($" [{ChatColors.Green}Snake{ChatColors.Default}] {ChatColors.LightBlue}{_player.PlayerName}{ChatColors.Default} scored {ChatColors.Gold}{_score}{ChatColors.Default} points!");
        }
    }

    // Moves the snake based on the current direction
    private void MoveSnake()
    {
        if (gameOver) return;

        // Calculate new head position with wrapping
        (int X, int Y) newHead = (snake[0].X + direction.X, snake[0].Y + direction.Y);

        // Wrap around horizontally (left/right)
        if (newHead.X < 0) newHead.X = gridWidth - 1; // Wrap from left to right
        if (newHead.X >= gridWidth) newHead.X = 0;    // Wrap from right to left

        // Wrap around vertically (top/bottom)
        if (newHead.Y < 0) newHead.Y = gridHeight - 1; // Wrap from top to bottom
        if (newHead.Y >= gridHeight) newHead.Y = 0;    // Wrap from bottom to top

        // Check for collision with self
        if (snake.Contains(newHead))
        {
            gameOver = true;
            return;
        }

        // Move the snake's body
        snake.Insert(0, newHead); // Add the new head
        if (newHead == food)
        {
            // Snake ate the food, increase the score
            _score += 10; // Increment the score
            // Generate new food and don't remove the tail (snake grows)
            food = GenerateFoodPosition();
        }
        else
        {
            snake.RemoveAt(snake.Count - 1); // Remove the tail (move forward)
        }
    }
}
