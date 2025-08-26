using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float mSpeed;
    [SerializeField] private GameObject prfDestroy;

    private Hero mHero;

    private Transform target;
    private int mDamage;

    void Update()
    {
        float distance = Vector2.Distance(transform.position, target.position);
        if(distance >= 0.1f) {
            transform.position = Vector2.MoveTowards(transform.position, target.position, mSpeed * Time.deltaTime);
        } else
        {
            Instantiate(prfDestroy, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
            mHero.SetDamage();
        }
    }

    public void Init(Transform t, Hero hero)
    {
        target = t;
        mHero = hero;
    }
}
