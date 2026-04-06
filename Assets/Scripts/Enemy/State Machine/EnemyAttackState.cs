using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyAI enemy) : base(enemy) { }

    public override void Tick()
    {
        if (!enemy.CanSeePlayer())
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        if (!enemy.IsPlayerInAttackRange(enemy.meleeRange * 1.15f))
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.FacePlayer();
        enemy.TryAttack();
    }
}