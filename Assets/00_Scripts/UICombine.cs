using IGN.Common.Actions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class UICombine : MonoBehaviour
{
    public Combine[] m_combines;

    public Image m_imgResultCharacter;

    public Transform m_horizontalContent;
    public GameObject m_objMaterialChracter;
    public GameObject m_objPlus;
    public Button m_btnCombine;

    public LocalizeStringEvent m_localStrResultCharacterTitle;
    public LocalizeStringEvent m_localStrResultCharacterDesc;

    private int m_characterIndex = 0;
    private bool initialized = false;

    void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        Deinitialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        m_btnCombine.onClick.AddListener(OnCombine);
        m_combines = Resources.LoadAll<Combine>("Combine");
        SetSprite();
    }

    private void Deinitialize()
    {
        m_characterIndex = 0;
    }

    private void SetSprite()
    {
        m_horizontalContent.DestroyAllChildren();

        var combineData = m_combines[m_characterIndex];
        var resultCharacterData = combineData.m_resultHeroStat;
        var materialCharacterData = combineData.m_materialHeroStatList;

        m_imgResultCharacter.sprite = ResourceManager.GetSprite(resultCharacterData.IconName);
        m_localStrResultCharacterTitle.StringReference = LocalizationManager.GetHeroLocalString(resultCharacterData.Name.ToUpper());
        m_localStrResultCharacterDesc.StringReference = LocalizationManager.GetHeroLocalString($"{resultCharacterData.Name.ToUpper()}_DESC");
        
        for (int i = 0; i < materialCharacterData.Count; i++)
        {
            // generate material-character-ui
            var character = Instantiate(m_objMaterialChracter, m_horizontalContent);
            var imgIcon = character.transform.Find("Icon")?.GetComponent<Image>();
            var imgShadow = character.transform.Find("Shadow")?.GetComponent<Image>();
            if(imgIcon == null)
            {
                Debug.LogWarning("Something wrong... failed to find child object(Icon)");
                return;
            }
            if (imgShadow == null)
            {
                Debug.LogWarning("Something wrong... failed to find child object(Shadow)");
                return;
            }

            var mCharData = materialCharacterData[i];
            imgIcon.sprite = ResourceManager.GetSprite(mCharData.IconName);
            imgShadow.color = UtilManager.GetColorByRarity(mCharData.rarity);
            character.gameObject.SetActive(true);

            // generate plus-ui
            if (i < materialCharacterData.Count - 1)
            {
                var plus = Instantiate(m_objPlus, m_horizontalContent);
                plus.gameObject.SetActive(true);
            }
        }

        CheckCombineUIButton();
    }

    private void CheckCombineUIButton()
    {
        if (CanCombine())
        {
            m_btnCombine.interactable = true;
        }
        else
        {
            m_btnCombine.interactable = false;
        }
    }

    private bool CanCombine()
    {
        var requiredHeroes = m_combines[m_characterIndex].m_materialHeroStatList;

        var allHeroes = Spawner.instance.dicHolder[UtilManager.LocalID]
            .SelectMany(holder => holder.Heros)
            .ToList();

        // 재료 그룹핑
        var requiredGroups = requiredHeroes
            .GroupBy(h => h.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // 전체 그룹핑
        var allGroups = allHeroes
            .GroupBy(h => h.m_Data.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (key, value) in requiredGroups)
        {
            var heroName = key;
            var needCount = value;
            var haveCount = allGroups.ContainsKey(heroName) ? allGroups[heroName] : 0;

            if (haveCount < needCount)
            {
                Debug.Log($"재료 히어로 {heroName} {needCount}개 필요하지만 {haveCount}개만 있음");
                return false;
            }
        }

        return true;
    }

    private void Combine(List<HeroStat> requiredHeros, HeroStat resultHero)
    {
        if (!CanCombine())
            return;

        var holders = Spawner.instance.dicHolder[UtilManager.LocalID];
        if (holders == null)
            return;

        List<Hero> candidateHeros = new();
        foreach (var needHero in requiredHeros)
        {
            foreach (var holder in holders)
            {
                foreach (var hero in holder.Heros)
                {
                    if(needHero.Name == hero.m_Data.Name)
                    {
                        candidateHeros.Add(hero);
                    }
                }
            }
        }

        foreach (var hero in candidateHeros)
        {
            hero.Sell(UtilManager.LocalID, new ActionContext
            {
                ContentFlags = ContentFlag.ByComposition
            });
        }

        GameManager.Instance.HeroCount -= requiredHeros.Count;
        Spawner.instance.Summon("Rare", resultHero);

        CheckCombineUIButton();
    }
    
    public void OnCombine()
    {
        var requiredHeroes = m_combines[m_characterIndex].m_materialHeroStatList;
        var resultHero = m_combines[m_characterIndex].m_resultHeroStat;
        Combine(requiredHeroes, resultHero);
    }

    public void OnArrow(int value)
    {
        int len = m_combines?.Length ?? 0;
        if (len == 0)
        {
            Debug.LogWarning("No combine data.");
            return;
        }

        m_characterIndex = (m_characterIndex + value) % len;
        if (m_characterIndex < 0) m_characterIndex += len; // 음수 보정

        SetSprite();
    }
}
