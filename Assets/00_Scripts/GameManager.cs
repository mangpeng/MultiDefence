using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public delegate void OnMoneyEventHandler();

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    public int Money = 50;
    public int SummonCount = 20;

    public List<Monster> Monsters = new();
    public int MonsterCount;

    public event OnMoneyEventHandler OnMoney;

    public void GetMoney(int value)
    {
        Money += value;
        OnMoney?.Invoke();
    }

    public void AddMonster(Monster m)
    {
        Monsters.Add(m);
        MonsterCount++;
        BC_ClientMonsterCount_ClientRpc(MonsterCount);
    }

    public void RemoveMonster(Monster m)
    {
        Monsters.Remove(m);
        MonsterCount--;
        BC_ClientMonsterCount_ClientRpc(MonsterCount);
    }

    [ClientRpc]
    private void BC_ClientMonsterCount_ClientRpc(int count)
    {
        MonsterCount = count;
    }
}
