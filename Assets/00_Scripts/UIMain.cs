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

    [Header("##TrailEffect")]
    [SerializeField] private GameObject prfTrail;
    [SerializeField] private float trailSpeed;
    
    [UnityEngine.Range(0.0f, 30.0f)]
    [SerializeField] private float yPosMin, yPosMax;
    [SerializeField] private float xPos;

    [Header("##Upgarde")]
    [SerializeField] private TextMeshProUGUI mTxtUgradeMoney;
    [SerializeField] private TextMeshProUGUI[] mTxtUgradeLevel;
    [SerializeField] private TextMeshProUGUI[] mTxtUgradeAsset;


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

        mTxtUgradeMoney.text = GameManager.Instance.Money.ToString();
        for (int i = 0; i < mTxtUgradeLevel.Length; i++)
        {
            mTxtUgradeLevel[i].text = $"Lv.{GameManager.Instance.mUpgrade[i] + 1}";
            mTxtUgradeAsset[i].text = $"{30 + GameManager.Instance.mUpgrade[i]}";
        }
    }
    
    #region UI
    public void OnUpgrade(int idx)
    {
        var curMoney = GameManager.Instance.Money;
        var needMoney = 30 + GameManager.Instance.mUpgrade[idx];

        if (curMoney < needMoney)
            return;

        GameManager.Instance.Money -= needMoney;
        ++GameManager.Instance.mUpgrade[idx];
    }

    public void BtnSummon()
    {
        if (GameManager.Instance.Money < GameManager.Instance.SummonNeedMoney)
            return;

        if (GameManager.Instance.HeroCount >= GameManager.MAX_HERO_COUNT)
            return;

        GameManager.Instance.Money -= GameManager.Instance.SummonNeedMoney;
        GameManager.Instance.SummonNeedMoney += 2;
        ++GameManager.Instance.HeroCount;

        StartCoroutine(CoSummonTrail());
    }

    private Vector3 GenerateRandomPoint(Vector3 start, Vector3 end)
    {
        var mid = (start + end) / 2;

        float randomHeight = Random.Range(yPosMin, yPosMax);
        mid += Vector3.up * randomHeight;

        mid += new Vector3(Random.Range(-xPos, xPos), 0.0f);

        return mid;
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

        var startPoint = btnSummonWorldPos;
        var endPoint = target;
        var midControlPoint = GenerateRandomPoint(startPoint, endPoint);

        float elapsedTime = 0.0f;
        while(elapsedTime < trailSpeed)
        {
            var t = elapsedTime / trailSpeed;
            Vector3 curvePos = CalculateBezierCurve(t, startPoint, midControlPoint, endPoint);
            go.transform.position = new Vector3(curvePos.x, curvePos.y, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //while (Vector3.Distance(go.transform.position, target) > 0.1f)
        //{
        //    go.transform.position = Vector3.MoveTowards(go.transform.position, target, Time.deltaTime * trailSpeed);
        //    yield return null;
        //}

        Destroy(go);
        
        Spawner.instance.Summon("Common", data);
    }

    private Vector3 CalculateBezierCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
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
