using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public partial class NetManager : MonoBehaviour
{
    private Lobby curLobby;

    private const int maxPlayers = 2;
    private string gamePlaySceneName = "GamePlayScene";

    public Button btnStartMatchmaking;
    public Button btnCancelMatching;
    public GameObject matchingObj;
     
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        btnStartMatchmaking.onClick.AddListener(() => StartMatchmaking());


    }   
}
