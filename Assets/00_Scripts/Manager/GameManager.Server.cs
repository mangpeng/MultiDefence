using System.Collections;
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

    private void StartServer()
    {
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

    #region RPC

  
    #endregion
}