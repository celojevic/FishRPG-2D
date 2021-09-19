namespace FishRPG.Entities.Player
{
    using FishNet.Object;
    using FishNet.Object.Synchronizing;
    using UnityEngine;

    public class PlayerCombat : NetworkBehaviour
    {

        [Header("Basic Attack")]
        [SerializeField] private DamageSpell _basicMeleeAttack = null;

        [SyncVar] private float _attackTimer;
        private Player _player;
        private Vector2 _attackDir;

        // TODO make this based on stats
        [SerializeField] private float _attackDelay = 0.5f;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Update()
        {
            if (!base.IsOwner) return;
            if (Utils.IsMouseOverUI()) return;
            if (Time.time < _attackTimer) return;

            if (Input.GetMouseButton(0))
            {
                _attackDir = (Utils.GetWorldMousePos() - transform.position).normalized;
                CmdBasicAttack(_attackDir);
            }
        }

        [ServerRpc]
        void CmdBasicAttack(Vector2 dir)
        {
            if (Time.time < _attackTimer) return;
            ResetAttackTimer();

            dir.Normalize();
            SpawnSwingAnimation(_player.GetCenter(), dir);

            // TODO this is only if skill animation doesnt have collider, make separate script for
            //      anim colliders when they (ontriggerenter)
            var hits = Physics2D.OverlapCircleAll(_player.GetCenter() + dir * _basicMeleeAttack.Range,
                _basicMeleeAttack.AoeRadius, LayerMask.GetMask("Enemy", "Resource"));
            if (hits.IsValid())
            {
                foreach (var item in hits)
                {
                    // enemy
                    if (item.isTrigger && item.GetComponent<Enemy>())
                    {
                        item.GetComponent<Health>()?.Subtract(_basicMeleeAttack.BaseDamage);
                        SpawnImpactAnim(item.transform.position);
                    }

                    // resource
                    if (item.isTrigger)
                    {
                        ResourceNode node = item.GetComponent<ResourceNode>();
                        if (node != null && node.MeetsRequirements(_player))
                            node.RpcSpawnImpactAnim();
                    }

                }
            }
        }

        private void ResetAttackTimer() => _attackTimer = Time.time + _attackDelay;

        [ObserversRpc]
        void SpawnImpactAnim(Vector2 pos)
        {
            if (_basicMeleeAttack.OnHitAnimPrefab)
                Instantiate(_basicMeleeAttack.OnHitAnimPrefab, pos, Quaternion.identity);
        }

        [ObserversRpc]
        void SpawnSwingAnimation(Vector2 pos, Vector2 dir)
        {
            if (_basicMeleeAttack.CastAnimPrefab)
            {
                Instantiate(_basicMeleeAttack.CastAnimPrefab, pos,
                    Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward)
                );
            }
        }

    }
}