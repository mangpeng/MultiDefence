using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : Singleton<UIMain>
{
    [SerializeField] private TextMeshProUGUI txtMonsterCount;
    [SerializeField] private Image imgMonsterCount;
    [SerializeField] private TextMeshProUGUI txtSummon;
    [SerializeField] private TextMeshProUGUI txtMoney;
    [SerializeField] private TextMeshProUGUI txtWave;
    [SerializeField] private TextMeshProUGUI txtTime;
    [SerializeField] private TextMeshProUGUI txtHeroCount;

    [SerializeField] private TextMeshProUGUI txtNavigation;
    [SerializeField] private Transform trNavigation;

    [SerializeField] private Animator animAsset;
    [SerializeField] private Button btnSummon;
    [SerializeField] private GameObject prfTrail;
    [SerializeField] private float trailSpeed;

    private List<TextMeshProUGUI> listNaviTxt = new();

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        GameManager.Instance.OnMoney += MoneyAni;
        GameManager.Instance.OnUpdateUIWave += UpdateUIWave;
        GameManager.Instance.OnUpdateUITime += UpdateUITime;

        btnSummon.onClick.AddListener(BtnSummon);
    }

    private void Update()
    {
        txtMonsterCount.text = $"{GameManager.Instance.MonsterCount.ToString()} / 100";
        imgMonsterCount.fillAmount = GameManager.Instance.MonsterCount / 100.0f;
        txtHeroCount.text = $"{GameManager.Instance.HeroCount:D2} / {GameManager.MAX_HERO_COUNT:D2}";

        txtSummon.text = GameManager.Instance.SummonNeedMoney.ToString();
        txtMoney.text = GameManager.Instance.Money.ToString();    
        txtSummon.color = GameManager.Instance.Money >= GameManager.Instance.SummonNeedMoney ? Color.white : Color.gray;
    }

    #region UI

    public void BtnSummon()
    {
        StartCoroutine(CoSummonTrail());
    }

    IEnumerator CoSummonTrail()
    {
        var data = Spawner.instance.GetRandomHeroCommonData();
        var emptyHolder = Spawner.instance.FindEmptyHereHolderOrNull(UtilManager.LocalID, data.Name);
        
        if(emptyHolder == null)
        {
            Debug.LogError("Not enough to place a new hero");
        }

        var btnSummonWorldPos = Camera.main.ScreenToWorldPoint(btnSummon.transform.position);
        var go = Instantiate(prfTrail);
        go.transform.position = btnSummonWorldPos;

        var target = emptyHolder.transform.position;
        while (Vector3.Distance(go.transform.position, target) > 0.1f)
        {
            go.transform.position = Vector3.MoveTowards(go.transform.position, target, Time.deltaTime * trailSpeed);
            yield return null;
        }

        Destroy(go);
        
        Spawner.instance.Summon("Common", data);
    }
    public void AddNavigationText(string msg)
    {
        if(listNaviTxt.Count > 7)
        {
            var first = listNaviTxt.First();
            listNaviTxt.RemoveAt(0);
            Destroy(first);
        }

        var go = Instantiate(txtNavigation, trNavigation);
        go.gameObject.SetActive(true);
        go.transform.SetAsFirstSibling();
        go.text = msg;
        listNaviTxt.Add(go);
        Destroy(go, 1.0f);
    }

    private void UpdateUIWave()
    {
        txtWave.text = $"WAVE{GameManager.Instance.curWave:D2}";  
    }

    private void UpdateUITime()
    {
        int minutes = GameManager.Instance.remainTime / 60;
        int seconds = GameManager.Instance.remainTime % 60;
        txtTime.text = $"{minutes:D2}:{seconds:D2}";
    }

    #endregion

    private void MoneyAni()
    {
        animAsset.SetTrigger("GET");
    }
}
