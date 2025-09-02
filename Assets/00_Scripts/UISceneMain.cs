using TMPro;
using UnityEngine;

public class UISceneMain : Singleton<UISceneMain>
{
    [SerializeField] private TextMeshProUGUI m_txtLevel;

    public void SetLevel(int level)
    {
        m_txtLevel.text = $"Lv.{level}";
    }
}
