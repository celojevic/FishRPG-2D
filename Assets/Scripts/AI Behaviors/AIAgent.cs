using FishNet.Object;
using UnityEngine;

public class AIAgent : NetworkBehaviour
{

    public Transform Target;

    [SerializeField] private float _speed = 1f;
    [SerializeField] private AIBehaviorBase _aiBehavior = null;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_aiBehavior == null || Target == null) return;

        _rb.velocity = _speed * _aiBehavior.Move(this, Target);
    }


}
