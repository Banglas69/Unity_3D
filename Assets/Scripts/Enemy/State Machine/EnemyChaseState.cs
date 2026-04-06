using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyAI enemy) : base(enemy) { }

    public override void Tick()
    {
        if (!enemy.CanSeePlayer())
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        if (enemy.IsPlayerInAttackRange())
        {
            enemy.ChangeState(enemy.AttackState);
            return;
        }

        enemy.ChasePlayer();
    }
}