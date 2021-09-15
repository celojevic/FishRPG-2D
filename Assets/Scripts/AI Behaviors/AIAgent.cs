using FishNet.Object;
using UnityEngine;

public class AIAgent : NetworkBehaviour
{

    public Transform Target;

    [SerializeField] private float _speed = 2f;
    [SerializeField] private AIBehaviorBase _aiBehavior = null;

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

    #region Editor
#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (_aiBehavior == null || Target == null) return;

        if (_aiBehavior is AICompositeBehavior composite)
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
