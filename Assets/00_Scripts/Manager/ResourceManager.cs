using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

public class ResourceManager
{
    public static SpriteAtlas m_atlas = Resources.Load<SpriteAtlas>("atlas");
    public static Sprite GetSprite(string name) => m_atlas.GetSprite(name);

    public static Setting m_setting = Resources.Load<Setting>("Setting");

    public static HeroStat GetRandomHeroDataByRarityOrNull(Rarity rarity)
    {
        var datas = GetHeroDataByRarityOrNull(rarity);

        if (datas == null || datas.Count == 0)
        {
            return null; 
        }

        var idx = UnityEngine.Random.Range(0, datas.Count);
        return datas[idx];
    }

    public static List<HeroStat> GetHeroDataByRarityOrNull(Rarity rarity)
    {
        return Resources.LoadAll<HeroStat>($"HeroData/{rarity}").ToList();
    }

    public static Rarity GetRandomRarity()
    {
        // 0 ~ 100 사이 랜덤 값
        float rand = UnityEngine.Random.Range(0f, 100f);
        float cumulative = 0f;

        var data = ResourceManager.m_setting;
        for (int i = 0; i < data.m_rarity_percent.Length; i++)
        {
            cumulative += data.m_rarity_percent[i];
            if (rand <= cumulative)
            {
                return (Rarity)i;
            }
        }

        // 혹시라도 합이 100이 안 맞을 경우 마지막 값 리턴
        return (Rarity)(data.m_rarity_percent.Length - 1);
    }
}
