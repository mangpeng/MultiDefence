using UnityEngine;

public class Character : MonoBehaviour
{
    protected Animator anim;
    protected SpriteRenderer sprRr;

    protected virtual void Start()
    {
        anim = transform.GetChild(0).GetComponent<Animator>();
        sprRr = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }
}
