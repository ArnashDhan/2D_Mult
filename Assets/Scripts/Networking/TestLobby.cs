using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private float heartbeatTimer;
    private string playerName = "T";

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
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

    private async void CreateLobby()
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

            AssignPlayerToTeam(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void AssignPlayerToTeam(Lobby lobby)
    {
        if (!lobby.Data.ContainsKey("Teams")) return;

        string teamsData = lobby.Data["Teams"].Value;

        // Parse existing team data
        Dictionary<string, int> teams = new Dictionary<string, int>();
        if (!string.IsNullOrEmpty(teamsData))
        {
            teams = JsonUtility.FromJson<Dictionary<string, int>>(teamsData);
        }

        // Assign player to a team
        string playerId = AuthenticationService.Instance.PlayerId;
        int team = teams.Count % 2 == 0 ? 1 : 2; // Alternate between Team 1 and 2
        teams[playerId] = team;

        // Update lobby data
        await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "Teams", new DataObject(DataObject.VisibilityOptions.Public, JsonUtility.ToJson(teams)) }
            }
        });

        Debug.Log($"Assigned Player {playerId} to Team {team}");
    }

    private async void StartGame(Lobby lobby)
    {
        if (lobby.Players.Count == lobby.MaxPlayers)
        {
            Debug.Log("Lobby is full. Starting game...");

            // Example: Transition to a new scene
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }
}
