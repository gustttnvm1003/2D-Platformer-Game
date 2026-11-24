using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Completely decoupled animation presenter for enemies.
    /// Reads high-level state from EnemyController without containing AI decision logic.
    /// </summary>
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private EnemyController enemyController;

        private bool _hasValidController;
        private bool _hasTurnParam;
        private bool _hasChargeParam;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (enemyController == null) enemyController = GetComponent<EnemyController>();
            RefreshControllerCache();
        }

        private void RefreshControllerCache()
        {
            _hasValidController = animator != null && animator.runtimeAnimatorController != null;
            _hasTurnParam = false;
            _hasChargeParam = false;
            if (!_hasValidController) return;

            int count = animator.parameterCount;
            for (int i = 0; i < count; i++)
            {
                AnimatorControllerParameter p = animator.GetParameter(i);
                if (p == null) continue;
                if (p.nameHash == GameConstants.EnemyAnimIsTurning) _hasTurnParam = true;
                else if (p.nameHash == GameConstants.EnemyAnimIsCharging) _hasChargeParam = true;
            }
        }

        private void LateUpdate()
        {
            if (enemyController == null) return;
            if (!_hasValidController)
            {
                RefreshControllerCache();
                if (!_hasValidController) return;
            }

            EnemyAIStateType state = enemyController.CurrentAIState;
            bool isDead = !enemyController.IsAlive || state == EnemyAIStateType.Death;
            bool isHurt = state == EnemyAIStateType.Stagger && !isDead;
            bool isWindup = state == EnemyAIStateType.Windup && !isDead && !isHurt;
            bool isAttack = state == EnemyAIStateType.Attack && !isDead && !isHurt;
            bool isTurn = enemyController.IsTurning && !isDead && !isHurt;
            bool isCharge = enemyController.IsCharging && !isDead && !isHurt && !isTurn;
            float speed = (isDead || isHurt || isWindup || isAttack || isTurn) ? 0f : enemyController.MovementSpeed;

            animator.SetBool(GameConstants.EnemyAnimIsDead, isDead);
            animator.SetBool(GameConstants.EnemyAnimIsHurt, isHurt);
            animator.SetBool(GameConstants.EnemyAnimIsWindup, isWindup);
            animator.SetBool(GameConstants.EnemyAnimIsAttacking, isAttack);
            if (_hasTurnParam) animator.SetBool(GameConstants.EnemyAnimIsTurning, isTurn);
            if (_hasChargeParam) animator.SetBool(GameConstants.EnemyAnimIsCharging, isCharge);
            animator.SetFloat(GameConstants.EnemyAnimSpeed, speed);
        }
    }
}
