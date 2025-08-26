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
    public int ATK;
    [SerializeField] private float attackRange = 1.0f;
    private float attackSpeed = 0.0f;

    private Transform attackTarget = null;
    private LayerMask enemyLayer;
    public HeroStat m_Data;

    public HeroHolder parentHolder;

    bool isMove = false;

    [SerializeField] private GameObject prfSpawnEffect;

    public void Initdata(HeroStatData data, HeroHolder holder, string rarity)
    {
        m_Data = Resources.Load<HeroStat>($"HeroData/{rarity}/{data.heroName}");

        attackRange = data.heroRange;
        ATK = data.heroAtk;
        attackSpeed = data.heroAtk_speed;
        parentHolder = holder;
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
