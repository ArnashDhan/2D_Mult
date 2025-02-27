using System.Collections.Generic;

public static class GameData
{
    public static string LobbyId;                   // Stores the ID of the lobby
    public static Dictionary<string, int> Teams { get; set; } = new Dictionary<string, int>();   // Player ID to team mapping
    public static float PrizePool { get; set; } = 0;                // Prize pool amount for the match
    public static string WinningTeamId;             // ID of the winning team
                                                    //below is the PlayerId and contribution mapping that is used for the winning team
    public static Dictionary<string, float> PlayerContributions { get; set; } = new Dictionary<string, float>();



    //something to reset the data after game is completed
    public static void ResetGameData()
    {
        LobbyId = null;
        Teams.Clear();
        PlayerContributions.Clear();
        PrizePool = 0;
    }
}