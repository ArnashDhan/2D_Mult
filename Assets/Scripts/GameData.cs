using System.Collections.Generic;

public static class GameData
{
    public static string LobbyId { get; set; } // Stores the ID of the lobby
    public static Dictionary<string, int> Teams { get; set; } = new Dictionary<string, int>(); // Player ID to team mapping
    public static float PrizePool { get; set; } = 0; // Prize pool amount for the match
    public static string WinningTeamId { get; set; } // ID of the winning team
    public static Dictionary<string, float> PlayerContributions { get; set; } = new Dictionary<string, float>(); // Player contribution mapping

    /// <summary>
    /// Resets all game-related data after the game is completed or when starting a new session.
    /// </summary>
    public static void ResetGameData()
    {
        LobbyId = null;
        Teams.Clear();
        PlayerContributions.Clear();
        PrizePool = 0;
        WinningTeamId = null;
    }

    /// <summary>
    /// Assigns a player's contribution to the prize pool.
    /// </summary>
    /// <param name="playerId">The player's ID.</param>
    /// <param name="contributionAmount">The amount contributed by the player.</param>
    public static void AddPlayerContribution(string playerId, float contributionAmount)
    {
        if (PlayerContributions.ContainsKey(playerId))
        {
            PlayerContributions[playerId] += contributionAmount;
        }
        else
        {
            PlayerContributions[playerId] = contributionAmount;
        }

        // Update the total prize pool
        PrizePool += contributionAmount;
    }

    /// <summary>
    /// Calculates and returns the contribution percentage for a given player.
    /// </summary>
    /// <param name="playerId">The player's ID.</param>
    /// <returns>The player's contribution percentage.</returns>
    public static float GetPlayerContributionPercentage(string playerId)
    {
        if (PrizePool == 0 || !PlayerContributions.ContainsKey(playerId))
            return 0;

        return (PlayerContributions[playerId] / PrizePool) * 100f;
    }
}
