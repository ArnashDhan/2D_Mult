using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;

public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private float heartbeatTimer;
    private string playerName = "T";
    private float lobbyUpdateTimer;
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        InitializeGameData();
        await QuickJoinOrCreateLobby();
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }

        }
    }

    private async Task CreateLobby()
    {
        try
        {
            string lobbyName = "Lobby";
            int maxPlayers = 4;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "Teams", new DataObject(DataObject.VisibilityOptions.Public, "") }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
          
            Debug.Log("Created Lobby! " + hostLobby.Name + " " + hostLobby.MaxPlayers);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task QuickJoinOrCreateLobby()
    {
        try
        {
            // Attempt to quick join any available lobby
            Debug.Log("Attempting to quick join a lobby...");
            hostLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
           
            Debug.Log($"Quick joined lobby: {hostLobby.Name}");
        }
        catch (LobbyServiceException ex)
        {
            // If no lobby is available, create a new one
            if (ex.Reason == LobbyExceptionReason.NoOpenLobbies)
            {
                Debug.Log("No available lobby found. Creating a new one...");
                await CreateLobby();
            }
            else
            {
                Debug.LogError($"Failed to quick join lobby: {ex.Message}");
            }
        }
    }


    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            Debug.Log("Lobbies Found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void JoinLobby(string lobbyId)
    {
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            Debug.Log($"Joined Lobby: {joinedLobby.Name}");

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    private async void HandleLobbyUpdates()
    {
        if (hostLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 5; // Check lobby status every 5 seconds
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                try
                {
                    hostLobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);

                    Debug.Log($"Lobby Updated: {hostLobby.Players.Count}/{hostLobby.MaxPlayers} players.");
                    CheckIfLobbyIsFull();
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogError($"Failed to update lobby: {e.Message}");
                }
            }
        }
    }
    private void CheckIfLobbyIsFull()
    {
        if (hostLobby.Players.Count == hostLobby.MaxPlayers)
        {
            Debug.Log("Lobby is full. Starting game...");
            StartGame(hostLobby);
        }
    }
    private void StartGame(Lobby lobby)
    {
        if (lobby.Players.Count == lobby.MaxPlayers)
        {
            Debug.Log("Lobby is full. Assigning players to teams and starting game...");

            // Assign players to teams
            Dictionary<string, int> teams = AssignPlayersToTeams(lobby);
            GameData.Teams = teams;
            foreach (var team in GameData.Teams)
            {
                Debug.Log($"Player {team.Key} is in Team {team.Value}");
            }
            StopLobbyUpdates();
            // Update the lobby data with team assignments
            UpdateLobbyWithTeams(lobby, teams);

            // Transition to the game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameLobby");
        }
    }
    private void StopLobbyUpdates()
    {
        hostLobby = null; // Clear the reference to stop updates
        lobbyUpdateTimer = 0; // Reset the timer
    }

    private Dictionary<string, int> AssignPlayersToTeams(Lobby lobby)
    {
        Dictionary<string, int> teams = new Dictionary<string, int>();

        int team1Count = 0;
        int team2Count = 0;

        foreach (Player player in lobby.Players)
        {
            // Alternate assignment to Team 1 and Team 2
            if (team1Count <= team2Count)
            {
                teams[player.Id] = 1; // Assign to Team 1
                team1Count++;
            }
            else
            {
                teams[player.Id] = 2; // Assign to Team 2
                team2Count++;
            }

            Debug.Log($"Assigned Player {player.Id} to Team {teams[player.Id]}");
        }
        Debug.Log($"Total players assigned to teams: {teams.Count}");
        return teams;
    }

    private async void UpdateLobbyWithTeams(Lobby lobby, Dictionary<string, int> teams)
    {
        try
        {
            // Convert team assignments to JSON
            string teamsJson = JsonUtility.ToJson(teams);

            // Update the lobby data
            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { "Teams", new DataObject(DataObject.VisibilityOptions.Public, teamsJson) }
            }
            });

            Debug.Log("Lobby updated with team assignments.");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby with teams: {e.Message}");
        }
    }
    public void InitializeGameData()
    {
        // Initialize Teams
        if (GameData.Teams == null)
        {
            GameData.Teams = new Dictionary<string, int>();
        }

        // Initialize Player Contributions
        if (GameData.PlayerContributions == null)
        {
            GameData.PlayerContributions = new Dictionary<string, float>();
        }

        // Set Prize Pool (example value)
        GameData.PrizePool = 0;

        Debug.Log("GameData initialized in LobbyManager.");
    }

}

