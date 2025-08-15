using System;
using System.Collections;
using System.Runtime.CompilerServices;
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

    public int HP = 0, MaxHP = 100;

    protected override void Start()
    {
        base.Start();
        HP = MaxHP;

        InitTarget();
    }



    private void Update()
    {
        imgHp2.fillAmount = Mathf.Lerp(imgHp2.fillAmount, imgHp.fillAmount, Time.deltaTime * 2.0f);

        if (isDead) return;

        // Move
        transform.position = Vector2.MoveTowards(transform.position, curTarget, Time.deltaTime * MOVE_SPEED);

        // Check next target and Change
        if(CanChangeTarget())
        {
            curTargetIdx = GetNextTargetIndex();
            ChangeCurrentTarget(curTargetIdx);
        }
    }

    #region Target
    private void InitTarget()
    {
        if (curTargetIdx >= CharacterSpawner.monsterMoveList.Count)
        {
            Debug.LogError($"Invalid move target idx. index: {curTargetIdx}, targetListCount: {CharacterSpawner.monsterMoveList.Count}");
            return;
        }

        var target = CharacterSpawner.monsterMoveList[curTargetIdx];
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
        if (nextTargetIdx >= CharacterSpawner.monsterMoveList.Count)
        {
            Debug.LogWarning($"Invalid move target idx. index: {nextTargetIdx}, targetListCount: {CharacterSpawner.monsterMoveList.Count}");
            return false;
        }

        var nextTarget = CharacterSpawner.monsterMoveList[nextTargetIdx];
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
        if (targetIdx >= CharacterSpawner.monsterMoveList.Count)
        {
            Debug.LogWarning($"Invalid move target idx. index: {targetIdx}, targetListCount: {CharacterSpawner.monsterMoveList.Count}");
            return;
        }

        var target = CharacterSpawner.monsterMoveList[targetIdx];
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
        return (curTargetIdx + 1) % CharacterSpawner.monsterMoveList.Count;
    }
    #endregion

    public void GetDamage(int dmg)
    {
        if (isDead) return;

        HP -= dmg;
        imgHp.fillAmount = (float)HP / MaxHP;
        Instantiate(txtHit, transform.position, Quaternion.identity).Initialize(dmg);
        
        if(HP <= 0)
        {
            isDead = true;
            GameManager.instance.GetMoney(1);
            GameManager.instance.RemoveMonster(this);
            gameObject.layer = LayerMask.NameToLayer("Default");
            AnimChange("DEAD", true);
            StartCoroutine(CDead());
        }
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

        Destroy(this.gameObject);
    }
}
