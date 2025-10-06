using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISceneMainShop : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_txtDia;
    [SerializeField] private GameObject m_panelGatcha;
    [SerializeField] private Transform m_gatchaContentRoot;
    [SerializeField] private GameObject m_prfGatchaHero;

    [SerializeField] private TextMeshProUGUI m_txtSummon01;
    [SerializeField] private TextMeshProUGUI m_txtSummon02;

    [SerializeField] private TextMeshProUGUI m_txtResummonNeedDia;

    private int m_prvSummonCount = 0;

    private void Start()
    {
        CloudManager.Instance.onAddDiaEvent += UpdateDiaText;
        CloudManager.Instance.onRemoveDiaEvent += UpdateDiaText;
    }

    private void OnDestroy()
    {
        CloudManager.Instance.onAddDiaEvent -= UpdateDiaText;
        CloudManager.Instance.onRemoveDiaEvent -= UpdateDiaText;
    }

    public void UpdateSummonDia()
    {
        var currentDia = CloudManager.Instance.m_dataPlayer.m_dia;
        if (currentDia < 100)
        {
            m_txtSummon01.color = Color.gray;
            m_txtSummon02.color = Color.gray;
        } else
        {
            if(currentDia >= 1000)
            {
                m_txtSummon02.color = Color.green;
            } else
            {
                m_txtSummon01.color = Color.green;
                m_txtSummon02.color = Color.gray;
            }
        }

        if(m_prvSummonCount == 10)
        {
            if (currentDia < 1000)
            {
                m_txtResummonNeedDia.color = Color.gray;
            } else
            {
                m_txtResummonNeedDia.color = Color.green;
            }
        } else if(m_prvSummonCount == 1)
        {
            if (currentDia < 100)
            {
                m_txtResummonNeedDia.color = Color.gray;
            }
            else
            {
                m_txtResummonNeedDia.color = Color.green;
            }
        }
    }

    public void OnResummon()
    {
        if(m_prvSummonCount != 0)
        {
            OnSummon(m_prvSummonCount);
        }
    }

    public void OnSummon(int count)
    {
        m_gatchaContentRoot.DestroyAllChildren();

        int needDia = count * 100;
        var curDia = CloudManager.Instance.m_dataPlayer.m_dia;
        AddDia(-needDia);

        m_panelGatcha.SetActive(true);
        m_prvSummonCount = count;
        m_txtResummonNeedDia.text = count == 10 ? "100" : "1000";

        StartCoroutine(CoSummon(count));

        _ = CloudManager.Instance.SaveAsync();
    }

    private IEnumerator CoSummon(int count)
    {
        List<HeroStat> pickedHeroDataList = new();
        for (int i = 0; i < count; i++)
        {
            var randomRarity = ResourceManager.GetRandomRarity();
            var randomHeroData = ResourceManager.GetRandomHeroDataByRarityOrNull(randomRarity);

            //if (randomHeroData == null)
            //{
            //    Debug.LogError("Failed to pick up the random hero data");
            //    return;
            //}

            if (randomHeroData == null)
                continue;

            pickedHeroDataList.Add(randomHeroData);
        }

        foreach (var data in pickedHeroDataList)
        {            
            var go = Instantiate<GameObject>(m_prfGatchaHero, m_gatchaContentRoot);
            go.SetActive(true);

            var ranCount = UnityEngine.Random.Range(1, 11);
            go.transform.Find("imgShadow").GetComponent<Image>().color = UtilManager.GetColorByRarity(data.rarity);
            go.transform.Find("imgIcon").GetComponent<Image>().sprite = ResourceManager.GetSprite(data.IconName);            
            go.transform.Find("txtCount").GetComponent<TextMeshProUGUI>().text = $"{ranCount}";

            if(CloudManager.Instance.m_dataPlayer.m_dicHero.TryGetValue(data.Name, out var heroData))
            {
                heroData.m_count += ranCount;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void AddDia(int addValue)
    {
        if(!CloudManager.Instance.AddDia(addValue))
        {
            Debug.LogError("Failed to add dia");
        }
    }

    private void UpdateDiaText()
    {
        m_txtDia.text = CloudManager.Instance.m_dataPlayer.m_dia.ToString();
        UpdateSummonDia();
    }


}
