namespace FishRPG.Triggers
{
    using FishNet.Object;
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class TriggerRadius : NetworkBehaviour
    {

        [SerializeField] private LayerMask _triggeringLayers = default;
        [SerializeField] private bool _useTrigger = true;

        [Header("Unity Events")]
        public UnityEvent<Collider2D> OnTriggerEnterActions;
        public UnityEvent<Collider2D> OnTriggerExitActions;

        public event Action<Collider2D> OnTriggerEntered;
        public event Action<Collider2D> OnTriggerExited;

        private Collider2D _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            if (_triggerCollider && !_triggerCollider.isTrigger)
                Debug.LogWarning($"Collider on {transform.root.name}'s child TriggerRadius object is not a trigger!");
        }

        [Server]
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            if ((_triggeringLayers & (1 << other.gameObject.layer)) != 0)
            {
                if (other.isTrigger == _useTrigger)
                {
                    OnTriggerEntered?.Invoke(other);
                    OnTriggerEnterActions?.Invoke(other);
                }
            }
        }

        [Server]
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;

            if ((_triggeringLayers & (1 << other.gameObject.layer)) != 0 && other.isTrigger)
            {
                if (other.isTrigger == _useTrigger)
                {
                    OnTriggerExited?.Invoke(other);
                    OnTriggerExitActions?.Invoke(other);
                }
            }
        }

    }

}
