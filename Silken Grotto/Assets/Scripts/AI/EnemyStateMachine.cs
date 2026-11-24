namespace SilkenGrotto
{
    /// <summary>
    /// Lightweight, reusable Finite State Machine for enemy AI behaviors.
    /// </summary>
    public class EnemyStateMachine
    {
        private IEnemyState _currentState;

        public IEnemyState CurrentState { get { return _currentState; } }
        public EnemyAIStateType CurrentStateType { get { return _currentState != null ? _currentState.StateType : EnemyAIStateType.Idle; } }

        public void ChangeState(IEnemyState nextState)
        {
            if (nextState == null || _currentState == nextState) return;
            if (_currentState != null) _currentState.Exit();
            _currentState = nextState;
            _currentState.Enter();
        }

        public void Tick(float dt)
        {
            if (_currentState != null) _currentState.Tick(dt);
        }

        public void FixedTick(float fixedDt)
        {
            if (_currentState != null) _currentState.FixedTick(fixedDt);
        }
    }
}
