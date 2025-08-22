using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    protected Animator anim;
    protected SpriteRenderer sprRr;

    protected virtual void Awake()
    {
        anim = transform.GetChild(0).GetComponent<Animator>();
        sprRr = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    public void GetInitCharacter(string heroeName, string rarity)
    {
        anim.runtimeAnimatorController = Resources.Load<HeroStat>($"HeroData/{rarity}/{heroeName}").animatorController;
    }

    protected void AnimChange(string temp, bool trigger)
    {
        if (trigger)
        {
            anim.SetTrigger(temp);
            return;
        }

        anim.SetBool("IDLE", false);
        anim.SetBool("MOVE", false);
        anim.SetBool(temp, true);
    }
}
