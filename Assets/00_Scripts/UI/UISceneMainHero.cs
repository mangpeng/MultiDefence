using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISceneMainHero : MonoBehaviour
{
    [SerializeField] private Transform m_heroContentRoot;
    [SerializeField] private GameObject m_prfHero;

    [SerializeField] private GameObject m_popupHeroInfo;
    [SerializeField] private Image m_heroInfoIcon;
    [SerializeField] private Image m_heroInfoRarity;

    private void OnEnable()
    {
        UpdateHero();
    }

    private void ShowHeroInfo(string heroName, string iconName)
    {
        m_popupHeroInfo.SetActive(true);
        m_heroInfoIcon.sprite = ResourceManager.GetSprite(iconName);
        m_heroInfoRarity.color = UtilManager.GetColorByRarity(CloudManager.Instance.m_dataPlayer.m_dicHero[heroName].m_rarity); 
    }

    private void UpdateHero()
    {
        m_heroContentRoot.DestroyAllChildren();

        var heros = CloudManager.Instance.m_dataPlayer.m_dicHero;

        foreach (var (name, data) in heros)
        {
            //if (data.m_count == 0)
            //    continue;

            var heroStat = UtilManager.GetHeroStatDataByNameOrNull(data.m_name);
            if(heroStat == null)
            {
                // 아이콘 없으면 기본 이미지로 세팅
            }
            var go = Instantiate(m_prfHero, m_heroContentRoot);
            go.transform.Find("txtName").GetComponent<TextMeshProUGUI>().text = data.m_name;
            go.transform.Find("icon").GetComponent<Image>().sprite = ResourceManager.GetSprite(heroStat.IconName);
            go.transform.Find("icon").GetComponent<Image>().color = data.m_count == 0 ? Color.black : Color.white;
            go.transform.Find("rarity").GetComponent<Image>().color = UtilManager.GetColorByRarity(data.m_rarity);
            go.transform.Find("count/txtCount").GetComponent<TextMeshProUGUI>().text = $"{data.m_count}/100";
            go.transform.Find("count/imgFill").GetComponent<Image>().fillAmount = data.m_count / 100.0f;
            go.transform.Find("imgLevelUp").gameObject.SetActive(data.m_count >= 100);
            go.GetComponent<Button>().onClick.AddListener(()=> ShowHeroInfo(data.m_name, heroStat.IconName));
        }
    }


}
