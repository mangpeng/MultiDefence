using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtMonsterCount;
    [SerializeField] private Image imgMonsterCount;
    [SerializeField] private TextMeshProUGUI txtSummon;
    [SerializeField] private TextMeshProUGUI txtMoney;
    [SerializeField] private TextMeshProUGUI txtWave;
    [SerializeField] private TextMeshProUGUI txtTime;

    [SerializeField] private Animator animAsset;

    private void Start()
    {
        GameManager.instance.OnMoney += MoneyAni;

        GameManager.instance.OnUpdateUIWave += UpdateUIWave;
        GameManager.instance.OnUpdateUITime += UpdateUITime;
    }

    private void Update()
    {
        txtMonsterCount.text = $"{GameManager.instance.MonsterCount.ToString()} / 100";
        imgMonsterCount.fillAmount = GameManager.instance.MonsterCount / 100.0f;

        txtSummon.text = GameManager.instance.SummonCount.ToString();
        txtMoney.text = GameManager.instance.Money.ToString();    
        txtSummon.color = GameManager.instance.Money >= GameManager.instance.SummonCount ? Color.white : Color.gray;
    }

    #region UI

    private void UpdateUIWave()
    {
        txtWave.text = $"WAVE{GameManager.instance.curWave:D2}";  
    }

    private void UpdateUITime()
    {
        int minutes = GameManager.instance.remainTime / 60;
        int seconds = GameManager.instance.remainTime % 60;
        txtTime.text = $"{minutes:D2}:{seconds:D2}";
    }

    #endregion

    private void MoneyAni()
    {
        animAsset.SetTrigger("GET");
    }
}
