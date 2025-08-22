using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public delegate void OnMoneyEventHandler();
public delegate void OnUpdateUIEventHandler();


public partial class GameManager : NetworkBehaviour
{
    public static GameManager Instance => Singleton<GameManager>.Instance;

    public event OnUpdateUIEventHandler OnUpdateUIWave;
    public event OnUpdateUIEventHandler OnUpdateUITime;

    public int Money = 50;
    public int SummonNeedMoney = 20;
    public int HeroCount = 0;
    public const int MAX_HERO_COUNT = 25;

    public List<Monster> Monsters = new();
    public int MonsterCount;

    public event OnMoneyEventHandler OnMoney;

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

    public void AddMonster(Monster m)
    {
        Monsters.Add(m);
        MonsterCount++;
        BC_ClientMonsterCount_ClientRpc(MonsterCount); //TODO 서버에게 요청 하고 처리 하도록 변경 필요
    }

    public void RemoveMonster(Monster m)
    {
        Monsters.Remove(m);
        MonsterCount--;
        BC_ClientMonsterCount_ClientRpc(MonsterCount); //TODO 서버에게 요청 하고 처리 하도록 변경 필요
    }

    #region RPC
    [ClientRpc]
    private void BC_ClientMonsterCount_ClientRpc(int count)
    {
        // Debug.Log($"[S->C]{nameof(BC_ClientMonsterCount_ClientRpc)}");

        MonsterCount = count;
    }

    [ClientRpc]
    private void BC_UpdateTime_ClientRpc(int remainTime, int curWave)
    {
        // Debug.Log($"[S->C]{nameof(BC_UpdateTime_ClientRpc)}");

        this.remainTime = remainTime;
        this.curWave = curWave;

        OnUpdateUIWave?.Invoke();
        OnUpdateUITime?.Invoke();
    }
    #endregion
}
