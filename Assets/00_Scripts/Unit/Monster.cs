using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class Monster : Character
{
    public bool isBoss = false;

    [Header("Variables")]
    [SerializeField] private float MOVE_SPEED = 1;
    private float mSpeed = 1;

    public TextAni txtHit;
    public Image imgHp;
    public Image imgHp2;

    private int curTargetIdx = 0;
    private Vector2 curTarget;

    private bool isDead = false;
    private bool isStun = false;
    List<Vector2> moveList = new();
    public int HP = 0, MaxHP = 100;

    [Header("Effects")]
    [SerializeField] private GameObject prfStun;

    protected void Start()
    {
        HP = (int)CalcuateMonsterHp(GameManager.Instance.curWave);
        MaxHP = HP; // ??...
        mSpeed = MOVE_SPEED;
        InitTarget();
    }

    public void Init(List<Vector2> moveList)
    {
        this.moveList = moveList;
    }

    double CalcuateMonsterHp(int waveLevel)
    {
        var baseHp = 50.0f;
        var powerMultiplier = Mathf.Pow(1.1f, waveLevel);

        if(waveLevel % 10 == 0)
        {
            powerMultiplier += 0.05f * (waveLevel / 10);
        }

        return baseHp * powerMultiplier * (isBoss ? 10 : 1);
    }

    private void Update()
    {
        imgHp2.fillAmount = Mathf.Lerp(imgHp2.fillAmount, imgHp.fillAmount, Time.deltaTime * 2.0f);

        if (isDead) return;
        if (isStun) return;

        // Move
        if (moveList.Count == 0)
            return;

        transform.position = Vector2.MoveTowards(transform.position, curTarget, Time.deltaTime * mSpeed);

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
        m_sprRr.flipX = targetIdx == 3 || targetIdx == 0; 
    }

    private int GetNextTargetIndex()
    {
        return (curTargetIdx + 1) % moveList.Count;
    }
    #endregion

 

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
        while(m_sprRr.color.a > 0.0f)
        {
            alpha -= Time.deltaTime;
            var color = m_sprRr.color;
            color.a = alpha;

            m_sprRr.color = color;

            yield return null;
        }
    }

    private IEnumerator CoDebuff(Action before, Action after, float duration)
    {
        before?.Invoke();
        yield return new WaitForSeconds(duration);
        after?.Invoke();
    }

    public void Slow(float amount, float duration)
    {
        var co = mCoDebuff[(int)Debuff.Slow];
        if (co != null)
        {
            return;
        }

        co = StartCoroutine(CoDebuff(
            ()=>
            {
                m_sprRr.color = Color.blue;
                var newSpeed = mSpeed - (mSpeed * amount);
                newSpeed = Mathf.Max(newSpeed, 0.1f);
                mSpeed = newSpeed;
            }, 
            ()=>
            {
                m_sprRr.color = Color.white;
                mSpeed = MOVE_SPEED;
                co = null;
            },
            duration));
    }

    public void Stun(float duration)
    {
        var co = mCoDebuff[(int)Debuff.Stun];
        if (co != null)
        {
            return;
        }

        co = StartCoroutine(CoDebuff(
            () =>
            {
                isStun = true;
                prfStun.gameObject.SetActive(true);
            },
            () =>
            {
                isStun = false;
                prfStun.gameObject.SetActive(false);
                co = null;
            },
            duration));
    }

    #region RPC
    [ClientRpc]
    private void BC_GetMoney_ClientRpc(int value)
    {
        GameManager.Instance.GetMoney(1);
    }

    [ClientRpc]
    private void BC_Hit_ClientRpc(int hp, int dmg, ClientRpcParams rpcParams = default)
    {
        HP = hp;
        imgHp.fillAmount = (float)HP / MaxHP;

        Instantiate(txtHit, transform.position, Quaternion.identity).Initialize(dmg);
    }

    [ClientRpc]
    public void BC_Debuff_ClientRpc(Debuff type, float[] values)
    {
        switch(type)
        {
            case Debuff.Slow:
                {
                    if(values.Length !=  2)
                    {
                        Debug.LogWarning("Not invalid params");
                        return;
                    }

                    var slowDuration = values[0];
                    var slowAmount = values[1];

                    Slow(slowAmount, slowDuration);
                    break;
                }
            case Debuff.Stun:
                {
                    if (values.Length != 1)
                    {
                        Debug.LogWarning("Not invalid params");
                        return;
                    }
                    var stunDuration = values[0];
                    Stun(stunDuration);
                    break;
                }
        }
    }

    [ClientRpc]
    private void BC_Dead_ClientRpc(int hp, int dmg, ClientRpcParams rpcParams = default)
    {
        isDead = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
        AnimChange("DEAD", true);
        StartCoroutine(CDead());
    }

    #endregion
}
