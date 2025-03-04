using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using TMPro;
using Unity.Services.Authentication;

public class DisplayCoin : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinBalanceText;
    [SerializeField] private float updateInterval = 5f; // Update interval in seconds

    private bool isFetching = false;

    private async void Start()
    {
        await InitializeUnityServices();

        await EnsurePlayerIsSignedIn();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            await UpdateBalance(); // Initial fetch
            InvokeRepeating(nameof(PeriodicUpdate), updateInterval, updateInterval);
        }
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    private async Task EnsurePlayerIsSignedIn()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                Debug.Log("Player not signed in. Attempting to sign in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Player signed in successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to sign in: {e.Message}");
            }
        }
    }

    private async void PeriodicUpdate()
    {
        if (isFetching) return; // Prevent overlapping fetch calls

        isFetching = true;
        await UpdateBalance();
        isFetching = false;
    }

    private async Task UpdateBalance()
    {
        string playerId = AuthenticationService.Instance.PlayerId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        try
        {
            long balance = await GetPlayerTokenBalance(playerId);
            coinBalanceText.text = balance.ToString();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update balance: {e.Message}");
        }
    }

    private async Task<long> GetPlayerTokenBalance(string playerId)
    {
        try
        {
            PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("TOKEN", 0);
            return newBalance.Balance;
        }
        catch (EconomyException e)
        {
            Debug.LogError($"Failed to get token balance for Player {playerId}: {e.Message}");
            return 0;
        }
    }
}
