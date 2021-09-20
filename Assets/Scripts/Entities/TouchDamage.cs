using FishNet.Object;
using FishRPG.Vitals;
using UnityEngine;

public class TouchDamage : NetworkBehaviour
{

    [SerializeField] private string _otherTag = "Player";
    [SerializeField] private int _damage = 1;
    [SerializeField] private bool _tickOnStay = true;
    [Tooltip("How often the other will be damaged while staying within the collider.")]
    [SerializeField] private float _tickDelay = 0.5f;

    private float _stayTimer;
    private void ResetStayTimer() => _stayTimer = Time.time + _tickDelay;

    [Server]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        ProcessOther(other);
    }

    [Server]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsServer) return;
        if (!_tickOnStay) return;
        if (Time.time < _stayTimer) return;

        ProcessOther(other);
    }

    [Server]
    void ProcessOther(Collider2D other)
    {
        ResetStayTimer();
        if (other.CompareTag(_otherTag) && other.isTrigger)
        {
            Health h = other.GetComponent<Health>();
            if (h)
                h.Subtract(_damage);
        }
    }

}
