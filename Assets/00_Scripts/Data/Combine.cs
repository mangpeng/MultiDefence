using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroStat", menuName = "Scriptable Objects/Combine")]
public class Combine : ScriptableObject
{
    public HeroStat m_resultHeroStat;
    public List<HeroStat> m_materialHeroStatList = new();
}

