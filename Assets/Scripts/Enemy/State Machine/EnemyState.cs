public abstract class EnemyState
{
    protected EnemyAI enemy;

    protected EnemyState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract void Tick();
}