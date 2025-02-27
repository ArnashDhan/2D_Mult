using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class GameLogic : MonoBehaviour
{
    private bool isDistributingPrizes = false;

    private void Start()
    {
        // Access team and contribution data
        Dictionary<string, int> teams = GameData.Teams;
        Dictionary<string, float> playerContributions = GameData.PlayerContributions;
        float totalPrizePool = GameData.PrizePool;

        Debug.Log($"Teams: {teams.Count}, Prize Pool: {totalPrizePool} at start");
        ONClickofthis();
    }

    private async void ONClickofthis()
    {
        int contributionAmount = Random.Range(1, 10); // Randomize token contribution for testing

        // Attempt a contribution
        await UpdatePlayerContribution(AuthenticationService.Instance.PlayerId, contributionAmount);

        // Calculate winners and distribute prizes
        CalculateWinnerAndDistributePrizes();
    }

    public async Task UpdatePlayerContribution(string playerId, float contributionAmount)
    {
        long playerBalance = await GetPlayerTokenBalance(playerId);

        if (playerBalance < contributionAmount)
        {
            Debug.LogError($"Player {playerId} attempted to contribute {contributionAmount}, but only has {playerBalance} tokens.");
            return;
        }

        // Increment player contributions
        if (!GameData.PlayerContributions.ContainsKey(playerId))
        {
            GameData.PlayerContributions[playerId] = 0;
        }
        GameData.PlayerContributions[playerId] += contributionAmount;
        GameData.PrizePool += contributionAmount;

        Debug.Log($"Player {playerId} contributed {contributionAmount}. Total: {GameData.PlayerContributions[playerId]}");

        // Deduct contribution amount from player's token balance
        await UpdatePlayerTokens(playerId, -contributionAmount);
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
            // Log current game data for debugging
            Debug.Log("Current Player Contributions:");
            foreach (var contribution in GameData.PlayerContributions)
            {
                Debug.Log($"Player {contribution.Key}: {contribution.Value} tokens");
            }

            Debug.Log("Current Teams:");
            foreach (var team in GameData.Teams)
            {
                Debug.Log($"Player {team.Key} belongs to Team {team.Value}");
            }

            Dictionary<int, float> teamTotals = CalculateTeamTotals(GameData.Teams);
            int winningTeam = DetermineWinningTeam(teamTotals);

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
            else
            {
                Debug.LogWarning($"No contribution found for Player {playerId}.");
            }
        }

        foreach (var team in teamTotals)
        {
            Debug.Log($"Team {team.Key} has a total contribution of {team.Value}.");
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
        float winningTeamTotal = teamTotals[winningTeam];

        foreach (KeyValuePair<string, int> entry in GameData.Teams)
        {
            string playerId = entry.Key;
            int teamId = entry.Value;

            if (GameData.PlayerContributions.TryGetValue(playerId, out float playerContribution))
            {
                if (teamId == winningTeam)
                {
                    // Calculate player's share of the prize pool
                    float playerShare = (playerContribution / winningTeamTotal) * totalPrizePool;
                    await UpdatePlayerTokens(playerId, playerShare);

                    Debug.Log($"Player {playerId} receives {playerShare} tokens as their prize.");
                }
                else
                {
                    // Deduct contribution amount from losing team players
                    //await UpdatePlayerTokens(playerId, playerContribution);
                    // Contributions were already deducted
                    Debug.Log($"Player {playerId} loses {playerContribution} tokens as their contribution.");
                }
            }
        }

        // Reset prize pool after distribution
        GameData.PrizePool = 0;
    }

    private async Task<long> GetPlayerTokenBalance(string playerId)
    {
        try
        {
            PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("TOKEN", 0);
            Debug.Log($"Current balance for {newBalance.CurrencyId} is {newBalance.Balance}.");
            return newBalance.Balance;
        }
        catch (EconomyException e)
        {
            Debug.LogError($"Failed to get token balance for Player {playerId}: {e.Message}");
            return 0;
        }
    }

    private async Task UpdatePlayerTokens(string playerId, float amount)
    {
        if (amount == 0)
        {
            Debug.LogWarning($"No tokens to update for Player {playerId}. Skipping token update.");
            return;
        }

        try
        {
            if (amount > 0)
            {
                PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("TOKEN", (int)amount);
                Debug.Log($"New balance for {newBalance.CurrencyId} is {newBalance.Balance}. Added {amount} tokens to Player {playerId}.");
            }
            else
            {
                PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.DecrementBalanceAsync("TOKEN", (int)Mathf.Abs(amount));
                Debug.Log($"New balance for {newBalance.CurrencyId} is {newBalance.Balance}. Deducted {Mathf.Abs(amount)} tokens from Player {playerId}.");
            }
        }
        catch (EconomyException e)
        {
            Debug.LogError($"Failed to update tokens for Player {playerId}: {e.Message}");
        }
    }
}
