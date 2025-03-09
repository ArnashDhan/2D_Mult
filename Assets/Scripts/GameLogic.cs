using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using System.Threading.Tasks;
using System.Linq;

public class GameLogic : MonoBehaviour
{
    private bool isDistributingPrizes = false;

    private void Start()
    {
        Debug.Log("GameLogic script initialized.");
        InitializeGameData();
        AssignRandomContributionsForTesting(); // Assign random contributions to players for testing
        CalculateWinnerAndDistributePrizes(); // Automatically start the test process
    }

    private void InitializeGameData()
    {
        Debug.Log("Initializing GameData...");

        Debug.Log($"Lobby ID: {GameData.LobbyId}");
        Debug.Log($"Prize Pool: {GameData.PrizePool}");
        Debug.Log($"Teams Count: {GameData.Teams.Count}");

        foreach (var team in GameData.Teams)
        {
            Debug.Log($"Player {team.Key} is in Team {team.Value}");
        }

        if (GameData.PlayerContributions.Count > 0)
        {
            foreach (var contribution in GameData.PlayerContributions)
            {
                Debug.Log($"Player {contribution.Key} contributed {contribution.Value} to the prize pool.");
            }
        }
        else
        {
            Debug.Log("No player contributions recorded yet.");
        }
    }

    private void AssignRandomContributionsForTesting()
    {
        Debug.Log("Assigning random contributions for testing...");

        foreach (string playerId in GameData.Teams.Keys)
        {
            if (!GameData.PlayerContributions.ContainsKey(playerId))
            {
                float randomContribution = Random.Range(1f, 5f); // Random contribution between 1 and 5
                GameData.PlayerContributions[playerId] = randomContribution;
                Debug.Log($"Assigned random contribution of {randomContribution} to Player {playerId}.");
            }
        }
    }

    private async Task WaitForPlayerSync()
    {
        bool allSynced = false;

        while (!allSynced)
        {
            await Task.Delay(1000); // Wait 1 second before checking again.

            Dictionary<string, int> teams = GameData.Teams;
            Dictionary<string, float> contributions = GameData.PlayerContributions;

            allSynced = teams.Keys.All(playerId => contributions.ContainsKey(playerId));

            Debug.Log("Waiting for all players to sync their data...");
        }

        Debug.Log("All players are synced!");
    }

    public async void CalculateWinnerAndDistributePrizes()
    {
        if (isDistributingPrizes)
        {
            Debug.LogWarning("Prize distribution is already in progress.");
            return;
        }

        isDistributingPrizes = true;
        try
        {
            await WaitForPlayerSync();

            Dictionary<int, float> teamTotals = CalculateTeamTotals(GameData.Teams);
            int winningTeam = DetermineWinningTeam(teamTotals);

            if (winningTeam == -1)
            {
                Debug.LogError("No valid winning team found.");
                return;
            }

            await DistributePrizes(winningTeam, teamTotals);
        }
        finally
        {
            isDistributingPrizes = false;
        }
    }

    private Dictionary<int, float> CalculateTeamTotals(Dictionary<string, int> teams)
    {
        Dictionary<int, float> teamTotals = new Dictionary<int, float>();

        foreach (KeyValuePair<string, int> entry in teams)
        {
            string playerId = entry.Key;
            int teamId = entry.Value;

            if (!teamTotals.ContainsKey(teamId))
            {
                teamTotals[teamId] = 0;
            }

            if (GameData.PlayerContributions.TryGetValue(playerId, out float contribution))
            {
                teamTotals[teamId] += contribution;
                Debug.Log($"Adding contribution of {contribution} from Player {playerId} to Team {teamId}.");
            }
        }

        return teamTotals;
    }

    private int DetermineWinningTeam(Dictionary<int, float> teamTotals)
    {
        int winningTeam = -1;
        float highestContribution = 0;

        foreach (KeyValuePair<int, float> entry in teamTotals)
        {
            if (entry.Value > highestContribution)
            {
                highestContribution = entry.Value;
                winningTeam = entry.Key;
            }
        }

        Debug.Log($"Winning Team: {winningTeam} with {highestContribution} contribution.");
        return winningTeam;
    }

    private async Task DistributePrizes(int winningTeam, Dictionary<int, float> teamTotals)
    {
        float totalPrizePool = GameData.PrizePool;
        float winningTeamTotal = teamTotals.ContainsKey(winningTeam) ? teamTotals[winningTeam] : 0;

        foreach (KeyValuePair<string, int> entry in GameData.Teams)
        {
            string playerId = entry.Key;
            int teamId = entry.Value;

            if (GameData.PlayerContributions.TryGetValue(playerId, out float playerContribution))
            {
                if (teamId == winningTeam)
                {
                    float playerShare = (playerContribution / winningTeamTotal) * totalPrizePool;
                    await UpdatePlayerTokens(playerId, playerShare);

                    Debug.Log($"Player {playerId} receives {playerShare} tokens as their prize.");
                }
                else
                {
                    Debug.Log($"Player {playerId} loses their contribution of {playerContribution}.");
                }
            }
        }

        GameData.PrizePool = 0;
    }

    private async Task UpdatePlayerTokens(string playerId, float amount)
    {
        try
        {
            if (amount > 0)
            {
                PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("TOKEN", (int)amount);
                Debug.Log($"Added {amount} tokens to Player {playerId}. New balance: {newBalance.Balance}.");
            }
        }
        catch (EconomyException e)
        {
            Debug.LogError($"Failed to update tokens for Player {playerId}: {e.Message}");
        }
    }
}
