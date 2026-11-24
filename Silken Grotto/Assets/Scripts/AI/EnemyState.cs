namespace SilkenGrotto
{
    public enum EnemyAIStateType
    {
        Idle,
        Patrol,
        Chase,
        Windup,
        Attack,
        Recover,
        Stagger,
        Death
    }

    public interface IEnemyState
    {
        EnemyAIStateType StateType { get; }
        void Enter();
        void Tick(float dt);
        void FixedTick(float fixedDt);
        void Exit();
    }
}
