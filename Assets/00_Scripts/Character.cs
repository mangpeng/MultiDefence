using Unity.Netcode;
using UnityEngine;

public class Character : NetworkBehaviour
{
    protected Animator anim;
    protected SpriteRenderer sprRr;

    protected virtual void Start()
    {
        anim = transform.GetChild(0).GetComponent<Animator>();
        sprRr = transform.GetChild(0).GetComponent<SpriteRenderer>();
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
