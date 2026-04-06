using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        enemy.BeginIdlePatrol();
    }

    public override void Tick()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.UpdateIdlePatrol();
    }
}