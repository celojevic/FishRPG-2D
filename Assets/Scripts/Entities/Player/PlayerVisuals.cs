namespace FishRPG.Entities.Player
{
    using FishNet.Component.Animating;
    using FishNet.Object;
    using UnityEngine;

    public class PlayerVisuals : NetworkBehaviour
    {

        [Tooltip("The SpriteRenderer for the player's base sprite.")]
        [SerializeField] private SpriteRenderer _sr = null;
        public SpriteRenderer GetBaseRenderer() => _sr;

        [SerializeField, EnumNameArray(typeof(EquipmentSlot))]
        private SpriteRenderer[] _equipmentRenderers = new SpriteRenderer[(int)EquipmentSlot.Count];

        /// <summary>
        /// The master Player script.
        /// </summary>
        private Player _player;
        private Anim _currentAnimation;
        private NetworkAnimator _netAnimator;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _netAnimator = GetComponent<NetworkAnimator>();
            if (_sr == null)
                GetComponentInChildren<SpriteRenderer>();
            AssignClassData();
        }

        void AssignClassData()
        {
            if (_player.Class == null) return;

            var app = _player.GetAppearance();
            _sr.sprite = app.Sprite;
            _netAnimator.SetController(app.Controller);
        }

        private void Update()
        {
            if (!IsOwner) return;

            CheckSpriteFlip();
            CheckAnimation();
        }

        void CheckSpriteFlip()
        {
            if (_player.Input.InputVector.x < 0)
            {
                FlipSprite(true);
            }
            else if (_player.Input.InputVector.x > 0)
            {
                FlipSprite(false);
            }
        }

        void FlipSprite(bool flip)
        {
            if (_sr.flipX == flip) return;

            _sr.flipX = flip;
            foreach (var item in _equipmentRenderers)
                item.flipX = flip;
        }

        void CheckAnimation()
        {
            if (_player.Input.InputVector != Vector2.zero)
            {
                ChangeAnimation(Anim.Walk);
            }
            else
            {
                ChangeAnimation(Anim.Idle);
            }
        }

        void ChangeAnimation(Anim animation)
        {
            if (_currentAnimation == animation) return;

            _currentAnimation = animation;
            _netAnimator.Play(animation.ToString());
        }

        public void SetEquipmentSprite(EquipmentSlot slot, Sprite sprite)
        {
            _equipmentRenderers[(int)slot].sprite = sprite;
        }

        private enum Anim : byte
        {
            Idle,
            Walk
        }

    }

}
