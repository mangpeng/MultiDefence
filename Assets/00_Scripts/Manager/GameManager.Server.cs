using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class GameManager
{
    private const int DEFAULT_REMAIN_TIME = 10;
    [System.NonSerialized] public int remainTime = DEFAULT_REMAIN_TIME;
    [System.NonSerialized] public int curWave = 1;

    private int beforeWave = 1;

    public bool inBoss = false;
    public Coroutine coCountDown;
    public Dictionary<ulong/*client id*/, int/*accumulated damage*/> dicAccDamage = new();
    public Dictionary<ulong/*client id*/, string/*id*/> dicPlayers = new();

    private bool m_isChangingScene = false;

    private void StartServer()
    {
        //fixme 클라 접속 완료시 처리시 아래 작업 되도록 수정 필요
        dicAccDamage.Add(0, 0);
        dicAccDamage.Add(1, 0);

        coCountDown = StartCoroutine(CoCountdown());        
    }

    private void UpdateServer()
    {
        
    }

    IEnumerator CoCountdown()
    {
        bool isBossWave = curWave % 5 == 0;
        if (isBossWave)
        {
            remainTime = 60;
        }
        else
        {
            remainTime = DEFAULT_REMAIN_TIME;
        }

        while (remainTime > 0)
        {
            bool changedWave = beforeWave != curWave;
            beforeWave = curWave;
            BC_UpdateTime_ClientRpc(remainTime, curWave, changedWave);

            yield return new WaitForSeconds(1); // timeScale 영향 받음
            remainTime--;            
        }
                    
        ++curWave;

        coCountDown = StartCoroutine(CoCountdown());
    }

    public static void DespawnAllNetworkObjects()
    {
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true); // true → 씬에서 Destroy도 같이
            }
        }
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)] 
    public void CS_ChangeScene_ServerRpc()
    {
        if (m_isChangingScene)
            return;

        // GameManager.DespawnAllNetworkObjects();
        m_isChangingScene = true;
        NetworkManager.Singleton.SceneManager.LoadScene("MainScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CS_UpdateNickName_ServerRpc(ulong clientId, string id)
    {
        Debug.Log(2);
        if (dicPlayers.TryAdd(clientId, id))
        {
            Debug.Log($"{clientId}, {id}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CS_SelectNickName_ServerRpc(ulong sender)
    {
        Debug.Log($"sender: {sender}");
        foreach (var (clientId, id) in dicPlayers)
        {
            var rpcParamsToSender = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { sender }
                }
            };
            S2C_SelectNickName_ClientRpc(clientId, id, rpcParamsToSender);
        }
    }
    #endregion
}