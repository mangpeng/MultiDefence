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
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public partial class NetManager : MonoBehaviour
{
    private Lobby curLobby;

    private const int maxPlayers = 2;
    private string gamePlaySceneName = "GamePlayScene";

    public Button btnStartMatchmaking;
    public Button btnCancelMatching;
    public GameObject matchingObj;

    public GameObject m_objNickNameUI;
    public TMP_InputField m_inputNickName;
    public Button m_btnNickNameConfirm;

    private async void Start()
    {
        btnStartMatchmaking.onClick.AddListener(StartMatchmaking);
        m_btnNickNameConfirm.onClick.AddListener(OnBtnNicknameConfirm);

        await UnityServices.InitializeAsync();
        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            //await CloudManager.Instance.DeletePlayerData();
            // await CloudManager.Instance.SavePlayerData(new DataPlayer("kim", 12));
            var data = await CloudManager.Instance.LoadPlayerData();
            if (data.m_id.IsNullOrEmpty())
            {
                m_objNickNameUI.SetActive(true);
            }
        }
    }   

    private void OnBtnNicknameConfirm()
    {
        if(m_inputNickName.text.IsNullOrEmpty())
        {
            Debug.LogWarning("Empty input field text");
            return;
        }

        CloudManager.Instance.m_dataPlayer.m_id = m_inputNickName.text;

        _ = CloudManager.Instance.SaveAsync();
        m_objNickNameUI.SetActive(false);
    }
}
