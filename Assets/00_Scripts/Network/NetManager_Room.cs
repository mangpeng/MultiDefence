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
            //var joinAlloc = await RelayService.Instance.JoinAllocationAsync(inputJoinCode.Trim());

            //// 접속한 호스트 정보로 서버데이터 변경
            //NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            //    joinAlloc.RelayServer.IpV4,
            //    (ushort)joinAlloc.RelayServer.Port,
            //    joinAlloc.AllocationIdBytes,
            //    joinAlloc.Key,
            //    joinAlloc.ConnectionData,
            //    joinAlloc.HostConnectionData
            //    );


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


    //public async Task JoinGameWithCode(string inputJoinCode)
    //{
    //    // 0) 입력 검증/정규화
    //    var code = inputJoinCode?.Trim().ToUpperInvariant();
    //    if (string.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code, "^[A-Z0-9]{6,8}$"))
    //    {
    //        Debug.LogError($"[JOIN] Invalid join code: '{inputJoinCode}'");
    //        return;
    //    }

    //    try
    //    {
    //        // 1) Unity Services 초기화 & 로그인
    //        if (UnityServices.State == ServicesInitializationState.Uninitialized)
    //            await UnityServices.InitializeAsync();

    //        if (!AuthenticationService.Instance.IsSignedIn)
    //            await AuthenticationService.Instance.SignInAnonymouslyAsync();

    //        if (RelayService.Instance == null)
    //        {
    //            Debug.LogError("[JOIN] RelayService.Instance is null");
    //            return;
    //        }

    //        // 2) Relay Join
    //        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
    //        if (joinAlloc == null)
    //        {
    //            Debug.LogError("[JOIN] joinAlloc is null");
    //            return;
    //        }

    //        // 3) NetworkManager / Transport 확보
    //        var nm = NetworkManager.Singleton;
    //        if (nm == null)
    //        {
    //            Debug.LogError("[JOIN] NetworkManager.Singleton is null (씬에 배치됐는지 확인).");
    //            return;
    //        }

    //        var transport = nm.GetComponent<UnityTransport>();
    //        if (transport == null)
    //        {
    //            Debug.LogError("[JOIN] UnityTransport component is missing on NetworkManager.");
    //            return;
    //        }

    //        // 4) RelayServerData 세팅 (버전에 따라 한 가지 선택)
    //        // (A) 최신 방식: RelayServerData 객체 사용
    //        transport.SetRelayServerData(
    //            joinAlloc.RelayServer.IpV4,
    //            (ushort)joinAlloc.RelayServer.Port,
    //            joinAlloc.AllocationIdBytes,
    //            joinAlloc.Key,
    //            joinAlloc.ConnectionData,
    //            joinAlloc.HostConnectionData
    //        );

    //        // 5) 즉시 바인드
    //        var ok = nm.StartClient(); // ← StartClient()는 NetworkManager 통해 호출
    //        if (!ok)
    //        {
    //            Debug.LogError("[JOIN] StartClient() returned false.");
    //            return;
    //        }

    //        Debug.Log($"[JOIN] StartClient OK. AllocationId={joinAlloc.AllocationId}");
    //    }
    //    catch (RelayServiceException e)
    //    {
    //        Debug.LogError($"[JOIN] RelayServiceException: {e.Message}\n{e}");
    //        // 필요 시 로비 조회 등 추가 진단
    //        try
    //        {
    //            var queryRes = await LobbyService.Instance.QueryLobbiesAsync();
    //            Debug.Log($"[JOIN] Lobby count: {queryRes.Results?.Count}");
    //        }
    //        catch { /* 로비 커넥트 안 돼 있으면 실패할 수 있음 */ }
    //    }
    //    catch (System.Exception e)
    //    {
    //        Debug.LogError($"[JOIN] {e.GetType().Name}: {e.Message}\n{e}");
    //    }
    //}


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
        Debug.Log($"{clientId}");
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
