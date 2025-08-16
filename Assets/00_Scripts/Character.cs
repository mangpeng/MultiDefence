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

    public void GetInitCharacter(string animator)
    {
        anim.runtimeAnimatorController = Resources.Load<HeroStat>("HeroData/" + animator).animatorController;
    }

    protected void AnimChange(string temp, bool trigger)
    {
        if(trigger)
        {
            anim.SetTrigger(temp);
        }
        else
        {
            anim.SetBool(temp, true);
        }
    }
}
