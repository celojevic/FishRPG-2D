namespace FishRPG.Entities.Player
{
    using FishNet.Object;
    using UnityEngine;

    public class PlayerMovement : NetworkBehaviour
    {

        [Tooltip("How fast the player moves.")]
        [SerializeField] private float _moveSpeed = 3f;

        /// <summary>
        /// The master Player script.
        /// </summary>
        private Player _player;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            MoveRigidbody();
        }

        void MoveRigidbody()
        {
            _rb.velocity = Vector2.zero;

            if (_player.Input.InputVector == Vector2.zero) return;

            _rb.velocity = _player.Input.InputVector.normalized * _moveSpeed;
        }

    }
}
