using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float mSpeed;
    [SerializeField] private GameObject prfDestroy;

    private Hero mHero;

    private Vector3? targetPos = null;
    private int mDamage;

    void Update()
    {
        if (!targetPos.HasValue)
            return;

        float distance = Vector2.Distance(transform.position, targetPos.Value);
        if(distance >= 0.1f) {
            transform.position = Vector2.MoveTowards(transform.position, targetPos.Value, mSpeed * Time.deltaTime);
        } else
        {
            Instantiate(prfDestroy, transform.position, Quaternion.identity);
            mHero.SetDamage();
            Destroy(this.gameObject);
        }
    }

    public void Init(Transform t, Hero hero)
    {
        targetPos = t.transform.position;
        mHero = hero;
    }
}
