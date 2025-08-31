using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICombine : MonoBehaviour
{
    public Combine[] m_combines;

    public Image m_imgResultCharacter;

    public Transform m_horizontalContent;
    public GameObject m_objMaterialChracter;
    public GameObject m_objPlus;

    public TextMeshProUGUI m_txtResultCharacterTitle;
    public TextMeshProUGUI m_txtResultCharacterDesc;

    private int m_characterIndex;

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
        
    }

    private void Initialize()
    {
        if (initialized)
            return;

        m_combines = Resources.LoadAll<Combine>("Combine");
        SetSprite();
    }

    private void SetSprite()
    {
        m_horizontalContent.DestroyAllChildren();

        var combineData = m_combines[m_characterIndex];
        var resultCharacterData = combineData.m_resultHeroStat;
        var materialCharacterData = combineData.m_materialHeroStatList;

        m_imgResultCharacter.sprite = ResourceManager.GetSprite(resultCharacterData.IconName);
        m_txtResultCharacterTitle.text = LocalizationManager.GetHeroText(resultCharacterData.Name.ToUpper());
        m_txtResultCharacterDesc.text = LocalizationManager.GetHeroText($"{resultCharacterData.Name.ToUpper()}_DESC");

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
