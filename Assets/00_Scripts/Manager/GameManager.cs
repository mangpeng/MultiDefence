using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public delegate void OnMoneyEventHandler();
public delegate void OnUpdateUIEventHandler();


public partial class GameManager : NetworkBehaviour
{
    public static GameManager Instance => Singleton<GameManager>.Instance;

    public event OnUpdateUIEventHandler OnUpdateUIWave;
    public event Action<bool> OnUpdateUITime;

    public int Money = 50;
    public int SummonNeedMoney = 20;
    public int HeroCount = 0;
    public const int MAX_HERO_COUNT = 25;

    public List<Monster> Monsters = new();
    public int MonsterCount;

    public List<Monster> BossMonsters = new();

    public event OnMoneyEventHandler OnMoney;

    public int[] mUpgrade = new int[4];

    private void Start()
    {
        if (IsServer)
        {
            StartClient();
        }

        if (IsClient)
        {
            StartServer();
        }
    }
    private void Update()
    {        
        if (IsServer)
        {
            UpdateServer();
        } 
        
        if(IsClient) 
        {
            UpdateClient();
        }
    }

    private void StartClient()
    {

    }


    private void UpdateClient()
    {

    }

    public void GetMoney(int value)
    {
        Money += value;
        OnMoney?.Invoke();
    }

    public void AddMonster(Monster m, bool isBoss)
    {
        if(isBoss) 
        {
            BossMonsters.Add(m);
        } 
        else
        {
            Monsters.Add(m);
        }
            
        MonsterCount++;
        BC_ClientMonsterCount_ClientRpc(MonsterCount, false); //TODO 서버에게 요청 하고 처리 하도록 변경 필요
    }

    public void RemoveMonster(Monster m, bool isBoss)
    {
        bool deadBoss = false;

        if (isBoss)
        {
            BossMonsters.Remove(m);
            if(BossMonsters.Count == 0)
            {
                inBoss = false;
                deadBoss = true;

                //
                if (coCountDown != null)
                {
                    StopCoroutine(coCountDown);
                }
                remainTime = DEFAULT_REMAIN_TIME;
                ++curWave;
                coCountDown = StartCoroutine(CoCountdown());

                StartCoroutine(Spawner.instance.CSpawnMonster());
                //


            }
        }
        else
        {
            Monsters.Remove(m);
        }
        
        MonsterCount--;
        BC_ClientMonsterCount_ClientRpc(MonsterCount, deadBoss); //TODO 서버에게 요청 하고 처리 하도록 변경 필요
    }

    #region RPC
    [ClientRpc]
    private void BC_ClientMonsterCount_ClientRpc(int count, bool isDeadBoss)
    {
        // Debug.Log($"[S->C]{nameof(BC_ClientMonsterCount_ClientRpc)}");

        MonsterCount = count;

        if(isDeadBoss)
        {
            UIMain.Instance.objBossWaveCount.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void BC_UpdateTime_ClientRpc(int remainTime, int curWave, bool changedWave)
    {
        // Debug.Log($"[S->C]{nameof(BC_UpdateTime_ClientRpc)}");

        this.remainTime = remainTime;
        this.curWave = curWave;

        string bossName = string.Empty;
        bool isBossWave = curWave % Spawner.BOSS_WAVE == 0;

        OnUpdateUIWave?.Invoke();
        OnUpdateUITime?.Invoke(isBossWave);
        
        if (changedWave)
        {
            if (isBossWave)
            {
                var bossIdx = curWave / Spawner.BOSS_WAVE - 1;
                bossName = Spawner.instance.dataBoss.listBossData[bossIdx].bossName;
            }

            UIMain.Instance.OnWavePopup(curWave, bossName);
        }
    }
    #endregion
}
