namespace FishRPG.AI
{
    using FishNet.Object;
    using UnityEngine;
    using FishRPG.AI.Movement;

    public class AIAgent : NetworkBehaviour
    {

        public Transform Target;

        [SerializeField] private float _speed = 2f;
        [SerializeField] private AIMovementBehaviorBase _aiBehavior = null;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;
            if (_aiBehavior == null || Target == null) return;

            _rb.velocity = Vector2.ClampMagnitude(_aiBehavior.Move(this, Target) * _speed, _speed);
        }

        public void SetTarget(Collider2D collider)
        {
            if (collider.transform == Target)
            {
                Target = null;
                return;
            }

            Target = collider.transform;
        }

        #region Editor
#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (_aiBehavior == null || Target == null) return;

            if (_aiBehavior is AIMovementComposite composite)
            {
                foreach (AIBehaviorWeights ai in composite.AIBehaviorWeights)
                {
                    Gizmos.color = ai.AIBehavior.GizmoColor;
                    Gizmos.DrawRay(transform.position, ai.AIBehavior.Move(this, Target) * ai.Weight);
                }
            }
            else
            {
                Gizmos.color = _aiBehavior.GizmoColor;
                Gizmos.DrawRay(transform.position, _aiBehavior.Move(this, Target));
            }
        }

#endif
        #endregion

    }
}
