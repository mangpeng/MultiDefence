using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using WebSocketSharp;

public partial class NetManager: MonoBehaviour
{
    public async void JoinGameWithCode(string inputJoinCode)
    {
        if (string.IsNullOrEmpty(inputJoinCode))
        {
            Debug.Log("Invalid JoinCode");
            return;
        }

        try
        {
            Debug.Log("Enter lobby with gamecode");

            curLobby = await FindAvailableLobby();
            await JoinLobby(curLobby.Id);
            
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Fail to enter game lobby with join code. " + e);
            var queryRes = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log(inputJoinCode);
        }
    }
    public async void StartMatchmaking()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("couldn't log in");
            return;
        }

        matchingObj.gameObject.SetActive(true);
        curLobby = await FindAvailableLobby();
        
        if (curLobby == null)
        {
            await CreateNewLobby();
        }
        else
        {
            await JoinLobby(curLobby.Id);
        }

        
    }

    private async Task<Lobby> FindAvailableLobby()
    {
        try
        {
            var queryRes = await LobbyService.Instance.QueryLobbiesAsync();
            if (queryRes.Results.Count > 0)
            {
                return queryRes.Results[0];
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Faild to find lobby" + e);
        }

        return null;
    }

    private async void DestroyLobby(string lobbyId)
    {
        try
        {
            if(!lobbyId.IsNullOrEmpty())
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }

            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                matchingObj.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async Task CreateNewLobby()
    {
        try
        {
            curLobby = await LobbyService.Instance.CreateLobbyAsync("랜덤 매칭 방", maxPlayers);
            Debug.Log("success to create new lobby" + curLobby.Id);
            await AllocateRelayServerAndJoin(curLobby);
            btnCancelMatching.onClick.RemoveAllListeners();
            btnCancelMatching.onClick.AddListener(() => DestroyLobby(curLobby.Id));
            //StartHost();

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Faild to create lobby" + e);
        }
    }

    private async Task JoinLobby(string lobbyId)
    {
        try
        {
            // curLobby 에 할당할 필요가 있나?
            curLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("entered to the room. lobbyID: " + lobbyId);
            StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("failed to join: " + e);
            throw;
        }
    }

    private async Task AllocateRelayServerAndJoin(Lobby lobby)
    {
        try
        {
            var alloc = await RelayService.Instance.CreateAllocationAsync(lobby.MaxPlayers);
            StartHost();

            // caution!!
            // you need to generate joincode after binding(host) to allocation
            // https://discussions.unity.com/t/relay-troubleshooting-join-code-not-found/880750/8
            var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            // txtJoinCode.text = code;

            Debug.Log("success to allocate relay server. join code: " + code);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Faild to allocate relay server" + e);
        }
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("start host");

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        OnPlayerJoined();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if(clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("start client");
    }
}
