using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Monster : Character
{
    [Header("Variables")]
    [SerializeField] private float MOVE_SPEED = 1;

    public TextAni txtHit;
    public Image imgHp;
    public Image imgHp2;

    private int curTargetIdx = 0;
    private Vector2 curTarget;

    private bool isDead = false;
    List<Vector2> moveList = new();
    public int HP = 0, MaxHP = 100;

    protected void Start()
    {
        HP = MaxHP;
        InitTarget();
    }

    public void Init(List<Vector2> moveList)
    {
        this.moveList = moveList;
    }

    private void Update()
    {
        imgHp2.fillAmount = Mathf.Lerp(imgHp2.fillAmount, imgHp.fillAmount, Time.deltaTime * 2.0f);

        if (isDead) return;

        // Move
        if (moveList.Count == 0)
            return;

        transform.position = Vector2.MoveTowards(transform.position, curTarget, Time.deltaTime * MOVE_SPEED);

        // Check next target and Change
        if (CanChangeTarget())
        {
            curTargetIdx = GetNextTargetIndex();
            ChangeCurrentTarget(curTargetIdx);
        }
    }

    #region Target
    private void InitTarget()
    {
        if (curTargetIdx >= moveList.Count)
        {
            Debug.LogError($"Invalid move target idx. index: {curTargetIdx}, targetListCount: {moveList.Count}");
            return;
        }

        var target = moveList[curTargetIdx];
        if (target == null)
        {
            Debug.LogError($"MoveTarget is null. targetIndex: {curTargetIdx}");
            return;
        }

        this.curTarget = target;
    }

    private bool CanChangeTarget()
    {
        // Check valid next target index
        var nextTargetIdx = GetNextTargetIndex();
        if (nextTargetIdx >= moveList.Count)
        {
            Debug.LogWarning($"Invalid move target idx. index: {nextTargetIdx}, targetListCount: {moveList.Count}");
            return false;
        }

        var nextTarget = moveList[nextTargetIdx];
        if (nextTarget == null)
        {
            Debug.LogWarning($"MoveTarget is null. targetIndex: {nextTargetIdx}");
            return false;
        }

        // Check distance
        if (Vector2.Distance(transform.position, curTarget) <= 0.01f)
            return true;

        return false;
    }

    private void ChangeCurrentTarget(int targetIdx)
    {
        if (targetIdx >= moveList.Count)
        {
            Debug.LogWarning($"Invalid move target idx. index: {targetIdx}, targetListCount: {moveList.Count}");
            return;
        }

        var target = moveList[targetIdx];
        if (target == null)
        {
            Debug.LogWarning($"MoveTarget is null. targetIndex: {targetIdx}");
            return;
        }

        this.curTarget = target;
        sprRr.flipX = targetIdx == 3 || targetIdx == 0; 
    }

    private int GetNextTargetIndex()
    {
        return (curTargetIdx + 1) % moveList.Count;
    }
    #endregion

    public void GetDamage(int dmg)
    {
        if (!IsServer) return;
        if (isDead) return;

        HP -= dmg;

        isDead = HP <= 0;

        if(isDead)
        {
            BC_Dead_ClientRpc(HP, dmg);

            // 서버에서 삭제
            // 주의! 서버에서 미리 삭제하면 ClientRpc는 작동 안함
            //NetworkObject.Despawn(false);
            GameManager.instance.RemoveMonster(this);
            StartCoroutine(CoDespawnAfter(1.0f));
        } else
        {
            BC_Hit_ClientRpc(HP, dmg);
        }
    }

    IEnumerator CoDespawnAfter(float t)
    {
        // 최소한 한 프레임은 기다리기 (t가 0이어도 null 한번)
        if (t <= 0f) yield return null;
        else yield return new WaitForSeconds(t);

        if (NetworkObject && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    IEnumerator CDead()
    {
        imgHp.transform.parent.gameObject.SetActive(false);
        float alpha = 1.0f;
        while(sprRr.color.a > 0.0f)
        {
            alpha -= Time.deltaTime;
            var color = sprRr.color;
            color.a = alpha;

            sprRr.color = color;

            yield return null;
        }
        

        // Destroy(this);
        // Debug.Log($"{NetworkManager.Singleton.LocalClientId} {NetworkObjectId}");
    }

    #region Network
    // C->S => CS
    // S->C => SC
    // S-> All C => BC
    // S-> C ... C => SCC

    [ClientRpc]
    private void BC_Hit_ClientRpc(int hp, int dmg, ClientRpcParams rpcParams = default)
    {
        HP = hp;
        imgHp.fillAmount = (float)HP / MaxHP;

        Instantiate(txtHit, transform.position, Quaternion.identity).Initialize(dmg);
    }

    [ClientRpc]
    private void BC_Dead_ClientRpc(int hp, int dmg, ClientRpcParams rpcParams = default)
    {
        isDead = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
        AnimChange("DEAD", true);
        StartCoroutine(CDead());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SC_MonsterDeadServerRpc()
    {
        NetworkManager.Destroy(this);
    }

    #endregion
}
