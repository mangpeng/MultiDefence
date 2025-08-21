using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class GameManager
{
    private const int DEFAULT_REMAIN_TIME = 120;
    [HideInInspector] public int remainTime = DEFAULT_REMAIN_TIME;
    [HideInInspector] public int curWave = 1;

    private void StartServer()
    {
        StartCoroutine(CoCountdown());
    }

    private void UpdateServer()
    {
        
    }

    IEnumerator CoCountdown()
    {
        while (remainTime > 0)
        {
            BC_UpdateTime_ClientRpc(remainTime, curWave);
            yield return new WaitForSeconds(1); // timeScale 영향 받음
            remainTime--;
        }

        remainTime = DEFAULT_REMAIN_TIME;
        ++curWave;

        StartCoroutine(CoCountdown());
    }

    #region RPC


    #endregion
}