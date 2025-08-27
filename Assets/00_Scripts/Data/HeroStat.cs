using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct HeroStatData : INetworkSerializable
{
    public string heroName;
    public int heroAtk;
    public float heroAtk_speed;
    public float heroRange;
    public Rarity heroRarity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            heroName ??= string.Empty;
        }

        serializer.SerializeValue(ref heroName);
        serializer.SerializeValue(ref heroAtk);
        serializer.SerializeValue(ref heroAtk_speed);
        serializer.SerializeValue(ref heroRange);
        serializer.SerializeValue(ref heroRarity);
    }
}
[CreateAssetMenu(fileName = "HeroStat", menuName = "Scriptable Objects/HeroStat")]
public class HeroStat : ScriptableObject
{
    public string Name;
    public int ATK;
    public float ATK_Speed;
    public float Range;
    public RuntimeAnimatorController animatorController;
    public Rarity rarity;
    public Bullet prfBullet;

    public HeroStatData GetData()
    {
        return new HeroStatData
        {
            heroName = Name,
            heroAtk = ATK,
            heroAtk_speed = ATK_Speed,
            heroRange = Range,
            heroRarity = rarity
        };
    }
}
