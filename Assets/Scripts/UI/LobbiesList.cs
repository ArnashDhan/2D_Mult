using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbiesList : MonoBehaviour
{
    private bool isRefreshing;
    private bool isJoining;

    [SerializeField] private Transform lobbyItemParent;
    [SerializeField] private LobbyItem lobbyItemPrefab;
    private void OnEnable()
    {
        RefreshList();

    }
    public async void RefreshList()
    {
        if (isRefreshing) { return; }

        isRefreshing = true;
        try
        {
            var options = new QueryLobbiesOptions();
            options.Count = 4;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value:"0"),
                  new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked,
                    op: QueryFilter.OpOptions.EQ,
                    value:"0"),
            };

            var lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
            foreach (Transform child in lobbyItemParent)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in lobbies.Results)
            {
                var lobbyInstance = Instantiate(lobbyItemPrefab, lobbyItemParent);
                lobbyInstance.Initialise(this, lobby);
            }
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
            isRefreshing = false;
            throw;
        }

        isRefreshing = false;
    }

    public async void JoinAsync(Lobby lobby)
    {
        isJoining = true;
        /*
        try
        {
            
            var joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync("lobbyCode");
            //await ClientManager.Instance.StartClient(joinCode);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
        */
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        isJoining = false;
    }

    public async void CreateLobby()
    {
        try
        {
            string lobbyName = "Lobby";
            int maxPlayers = 4;
            CreateLobbyOptions options = new CreateLobbyOptions();
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
}
