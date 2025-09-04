using TMPro;
using UnityEngine;

public class UISceneMain : MonoBehaviour
{
    public static UISceneMain Instance = null;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [SerializeField] private TextMeshProUGUI m_txtLevel;
    [SerializeField] private TextMeshProUGUI m_txtWave;

    public void SetLevel(int level)
    {
        m_txtLevel.text = $"Lv.{level}";

        var curWave = CloudManager.Instance.m_dataPlayer.m_wave;
        bool isNeedToActivate = curWave > 0;
        m_txtWave.transform.parent.gameObject.SetActive(isNeedToActivate);
        m_txtWave.text = $"{curWave}";
    }
}
