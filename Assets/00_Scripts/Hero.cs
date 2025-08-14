using System.Collections;
using System.Linq;
using UnityEngine;

public class Hero : Character
{
    [SerializeField] private float attackRange = 1.0f;
    private float attackSpeed = 0.0f;

    private Transform attackTarget = null;
    private LayerMask enemyLayer;

    protected override void Start()
    {
        base.Start();
        enemyLayer = LayerMask.GetMask("Monster");        
    }

    private void Update()
    {
        CheckForEnemies();
    }

    private void CheckForEnemies()
    {
        Collider2D[] colEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (colEnemies.Length == 0)
            return;

        var closestEnemy = colEnemies.OrderBy(col => Vector2.Distance(transform.position, col.transform.position)).FirstOrDefault();
        if (closestEnemy == null)
        {
            attackTarget = null;
            return;
        }

        if (attackTarget != closestEnemy.transform)
        {
            attackTarget = closestEnemy.transform;
            Debug.Log($"New target!! {attackTarget.name}");
        }

        attackSpeed += Time.deltaTime;
        if(attackTarget)
        {
            if(attackSpeed >= 1.0f)
            {
                attackSpeed = 0.0f;
                Attack(attackTarget);
            }
        }
    }

    private void Attack(Transform target)
    {
        Debug.Log("Attack");
        Debug.DrawLine(transform.position, target.position, Color.blue, 0.5f);
        AnimChange("ATTACK", true);

        var monster = target.GetComponent<Monster>();
        monster.GetDamage(10);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);


    }
}
