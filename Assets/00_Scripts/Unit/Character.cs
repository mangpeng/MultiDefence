using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    protected Animator m_anim;
    protected SpriteRenderer m_sprRr;

    protected virtual void Awake()
    {
        m_anim = transform.GetChild(0).GetComponent<Animator>();
        m_sprRr = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    public void GetInitCharacter(string heroeName, string rarity)
    {
        m_anim.runtimeAnimatorController = Resources.Load<HeroStat>($"HeroData/{rarity}/{heroeName}").animatorController;
    }

    protected void AnimChange(string temp, bool trigger)
    {        
        if(temp != "ATTACK")
        {
            m_anim.speed = 1.0f;
        }

        if (trigger)
        {
            m_anim.SetTrigger(temp);
            return;
        }

        m_anim.SetBool("IDLE", false);
        m_anim.SetBool("MOVE", false);
        m_anim.SetBool(temp, true);
    }
}
