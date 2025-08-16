using TMPro;
using UnityEngine;

public class UIMain : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtMonsterCount;
    [SerializeField] private TextMeshProUGUI txtSummon;
    [SerializeField] private TextMeshProUGUI txtMoney;

    [SerializeField] private Animator animAsset;

    private void Start()
    {
        GameManager.instance.OnMoney += MoneyAni;

    }
    private void Update()
    {
        txtMonsterCount.text = $"{GameManager.instance.MonsterCount.ToString()} / 100";
        txtSummon.text = GameManager.instance.SummonCount.ToString();
        txtMoney.text = GameManager.instance.Money.ToString();    
        txtSummon.color = GameManager.instance.Money >= GameManager.instance.SummonCount ? Color.white : Color.gray;
    }

    private void MoneyAni()
    {
        animAsset.SetTrigger("GET");
    }
}
