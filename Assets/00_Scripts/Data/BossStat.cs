using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BossData
{
    public string bossName;
    public Monster prfMonster;
}

[CreateAssetMenu(fileName = "BossStat", menuName = "Scriptable Objects/BossStat")]
public class BossStat : ScriptableObject
{
    public List<BossData> listBossData = new();
}
