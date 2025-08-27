using IGN.Common.Actions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

public class Hero : Character
{
    public int ATK
    {
        get
        {
            var addAtk = GameManager.Instance.mUpgrade[UpgradeIndex()] == 0 ? 100 : (100 + GameManager.Instance.mUpgrade[UpgradeIndex()] * 10);
            return (int)(m_Data.ATK * (addAtk / 100.0f));
        }
        set { }
    }

    [SerializeField] private float attackRange = 1.0f;
    private float attackSpeed = 0.0f;

    private Transform attackTarget = null;
    private LayerMask enemyLayer;
    public HeroStat m_Data;

    public HeroHolder parentHolder;

    bool isMove = false;

    [SerializeField] private GameObject prfSpawnEffect;

    public Color[] circleColor;
    public SpriteRenderer circleSrr;


    private int UpgradeIndex()
    {
        switch (m_Data.rarity)
        {
            case Rarity.Common:
            case Rarity.Uncommon:
            case Rarity.Rare:
                return 0;
            case Rarity.Hero: 
                return 1;
            case Rarity.Lengendary:
                return 2;
        }

        return 0;
    }

    public void Initdata(HeroStatData data, HeroHolder holder, string rarity)
    {
        m_Data = Resources.Load<HeroStat>($"HeroData/{rarity}/{data.heroName}");

        attackRange = data.heroRange;
        ATK = data.heroAtk;
        attackSpeed = data.heroAtk_speed;
        parentHolder = holder;
        circleSrr.color = circleColor[(int)data.heroRarity]; 
        GetInitCharacter(data.heroName, rarity);
        
        Instantiate(prfSpawnEffect, transform.parent.position, Quaternion.identity);

        if (rarity == "Uncommon")
        {
            sprRr.color = Color.red;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        enemyLayer = LayerMask.GetMask("Monster");        
    }

    public void ChangePosition(HeroHolder holder, List<Vector2> poss, int myIdx)
    {
        isMove = true;
        AnimChange("MOVE", false);

        parentHolder = holder;

        // network object는 서버에서만 변경 가능
        if(IsServer)
        {
            transform.parent = holder.transform;
        }

        // 왜 ? 로컬 위치로 비교하지??
        int sign = (int)Mathf.Sign(poss[myIdx].x - transform.position.x);
        switch (sign)
        {
            case -1: sprRr.flipX = true; break;
            case 1: sprRr.flipX = false; break;
        }

        StartCoroutine(CMove(poss[myIdx]));
    }

    private IEnumerator CMove(Vector2 endPos)
    {
        float current = 0.0f;
        float percent = 0.0f;
        Vector2 start = transform.position;
        Vector2 end = endPos;

        while(percent < 1.0f)
        {
            current += Time.deltaTime;
            percent = current / 0.5f;

            Vector2 lerpPos = Vector2.Lerp(start, end, percent);
            transform.position = lerpPos;
            yield return null;
        }

        isMove = false;
        AnimChange("IDLE", false);
        sprRr.flipX = true;
    }

    private void Update()
    {
        if (isMove) return;

        CheckForEnemies();
    }

    private void CheckForEnemies()
    {
        Collider2D[] colEnemies = Physics2D.OverlapCircleAll(parentHolder.transform.position, attackRange, enemyLayer);

        if (colEnemies.Length == 0)
            return;

        var closestEnemy = colEnemies.OrderBy(col => Vector2.Distance(transform.position, col.transform.position)).FirstOrDefault();
        if (closestEnemy == null)
        {
            attackTarget = null;
            return;
        }

        if (attackTarget != closestEnemy.transform)
        {
            attackTarget = closestEnemy.transform;
        }

        attackSpeed += Time.deltaTime;
        if(attackTarget)
        {
            if(attackSpeed >= 1.0f)
            {
                attackSpeed = 0.0f;
                AnimChange("ATTACK", true);
                GetBullet();
                // CS_AttackMonsterServerRpc(attackTarget.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    public void GetBullet()
    {
        var go = Instantiate<Bullet>(m_Data.prfBullet, transform.position + new Vector3(0.0f, 0.1f), Quaternion.identity);
        go.Init(attackTarget.transform, this);
    }

    public void Sell(ulong clientId, ActionContext ctx)
    {
        parentHolder.C2S_SellHero_ServerRpc(clientId, ctx);
    }

    public void SetDamage()
    {
        if (attackTarget == null) return;

        // 합성으로 인해 공격 시도한 히어로의 NetworkObject가 의미 무효일 수 있음
        var no = NetworkObject;
        if (no == null || !no.IsSpawned)
        {
            Debug.LogWarning($"[Hero] Cannot send ServerRpc. NetworkObject null or not spawned. no={no != null} spawned={no?.IsSpawned}");
            return;
        }

        // 공격 대상 몬스터의 NetworkObject가 의미 무효일 수 있음
        var targetNO = attackTarget.GetComponent<NetworkObject>();
        if (targetNO == null || !targetNO.IsSpawned)
        {
            Debug.LogWarning($"[Hero] Target invalid for RPC. targetNO={(targetNO != null)} spawned={targetNO?.IsSpawned}");
            return;
        }

        CS_AttackMonsterServerRpc(attackTarget.GetComponent<NetworkObject>().NetworkObjectId);
    }

    #region Network
    [ServerRpc(RequireOwnership = false)]
    public void CS_AttackMonsterServerRpc(ulong targetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var netObj))
        {
            var monster = netObj.GetComponent<Monster>();
            if (monster == null)
                return;

            monster.GetDamage(ATK);
        }
    }

    #endregion
}

