using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Skill : MonoBehaviour
{
    [NonSerialized] public eSkill mType;
    [NonSerialized] public bool mIsReady = false;
    [SerializeField] private Image mCoolTimeFill;

    private Hero mHero;
    private SkillActive mSkillData;

    public int Damage
    {
        get
        {
            return (int)(mHero.ATK * (mSkillData.mSkillDamage / 100));
        }
    }

    private void Start()
    {
        mHero = GetComponent<Hero>();
        if (mHero == null)
        {
            Debug.LogWarning("Failed to find a component. Hero");
        }

        if (mCoolTimeFill == null)
        {
            Debug.LogWarning("Failed to find a component. imgCoolTimeFill");
        }

        eSkill? type = mHero?.m_Data?.activeSkill?.type;
        if (type == null)
        {
            Debug.LogWarning("Failed to find a skill type.");
        }
        mSkillData = mHero.m_Data.activeSkill;

        if (type != eSkill.None) 
        {
            mCoolTimeFill.transform.parent.gameObject.SetActive(true);
            StartCoroutine(CoSkillDealy());
        } 


    }

    private void Update()
    {
        if(mHero.attackTarget != null && mIsReady)
        {
            StartCoroutine(CoSkillDealy());
            var action = GetSkillAction(mHero.m_Data.activeSkill.type);
            action?.Invoke();
        }
    }

    private IEnumerator CoSkillDealy()
    {
        mIsReady = false;

        float coolTime = mHero.m_Data.activeSkill.mCoolTime;

        float elapsed = 0.0f;
        while (elapsed < coolTime) {
            elapsed += Time.deltaTime;
            mCoolTimeFill.fillAmount = elapsed / coolTime;
            yield return null;
        }
        mIsReady = true; 
    }

    private Action GetSkillAction(eSkill type)
    {
        switch (type)
        {
            case eSkill.Gun: return () => Gun();
            default: return null;
        }
    }

    //fixme 서버에서 판정하고 내려 주는 형태로 바꿔야 함
    private void Gun()
    {
        var pos = mHero.attackTarget.position;
        var effect = mHero.m_Data.activeSkill.mParticle;
        Instantiate(effect, pos, Quaternion.identity);

        var overlappedMonsters = GameManager.Instance.FindMonsters(pos, radius: 1.0f);
        foreach (var monster in overlappedMonsters)
        {
            monster.C2S_Debuff_ServerRpc(Debuff.Stun, new float[]{ 1.0f });
            monster.GetDamage(Damage);
        }
    }
}
