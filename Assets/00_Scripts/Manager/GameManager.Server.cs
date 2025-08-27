using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class GameManager
{
    private const int DEFAULT_REMAIN_TIME = 30;
    [System.NonSerialized] public int remainTime = DEFAULT_REMAIN_TIME;
    [System.NonSerialized] public int curWave = 1;

    private int beforeWave = 1;

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
            Debug.Log(remainTime);
            bool changedWave = beforeWave != curWave;
            beforeWave = curWave;
            BC_UpdateTime_ClientRpc(remainTime, curWave, changedWave);

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