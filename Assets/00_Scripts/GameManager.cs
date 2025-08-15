using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnMoneyEventHandler();

public class GameManager : MonoBehaviour
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

    public event OnMoneyEventHandler OnMoney;

    public void GetMoney(int value)
    {
        Money += value;
        OnMoney?.Invoke();
    }

    public void AddMonster(Monster m)
    {
        Monsters.Add(m);
    }

    public void RemoveMonster(Monster m)
    {
        Monsters.Remove(m);
    }

}
