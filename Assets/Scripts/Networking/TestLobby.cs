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
    public static TestLobby Instance { get; private set; } // Singleton instance

    private Lobby hostLobby;
    private float heartbeatTimer;
    private string playerName = "T";
    private float lobbyUpdateTimer;

    private void Awake()
    {
        // Ensure a single instance of TestLobby persists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make this GameObject persistent across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        };

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch
        {
            Debug.Log("Already Signed IN");
        }

        InitializeGameData();
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyUpdates();
    }

    public async void StartLobbyProcess()
    {
        Debug.Log("The lobby is starting...");
        await QuickJoinOrCreateLobby();
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
            int maxPlayers = 2;

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
    public async void start()
    {
        await QuickJoinOrCreateLobby();
    }

    public async Task QuickJoinOrCreateLobby()
    {
        try
        {
            Debug.Log("Attempting to quick join a lobby...");
            hostLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            Debug.Log($"Quick joined lobby: {hostLobby.Name}");
        }
        catch (LobbyServiceException ex)
        {
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

    private async void HandleLobbyUpdates()
    {
        if (hostLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 5;
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
            Dictionary<string, int> teams = AssignPlayersToTeams(lobby);
            GameData.Teams = teams;
            foreach (var team in GameData.Teams)
            {
                Debug.Log($"Player {team.Key} is in Team {team.Value}");
            }
            StopLobbyUpdates();
            UpdateLobbyWithTeams(lobby, teams);
            UnityEngine.SceneManagement.SceneManager.LoadScene("ClashRoom");
        }
    }

    private void StopLobbyUpdates()
    {
        hostLobby = null;
        lobbyUpdateTimer = 0;
    }

    private Dictionary<string, int> AssignPlayersToTeams(Lobby lobby)
    {
        Dictionary<string, int> teams = new Dictionary<string, int>();

        int team1Count = 0;
        int team2Count = 0;

        foreach (Player player in lobby.Players)
        {
            if (team1Count <= team2Count)
            {
                teams[player.Id] = 1;
                team1Count++;
            }
            else
            {
                teams[player.Id] = 2;
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
            string teamsJson = JsonUtility.ToJson(teams);

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
        if (GameData.Teams == null)
        {
            GameData.Teams = new Dictionary<string, int>();
        }

        if (GameData.PlayerContributions == null)
        {
            GameData.PlayerContributions = new Dictionary<string, float>();
        }

        GameData.PrizePool = 0;
        Debug.Log("GameData initialized in LobbyManager.");
    }
}
