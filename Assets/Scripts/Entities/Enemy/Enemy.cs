namespace FishRPG.Entities.Enemy
{
    using FishNet.Object;
    using UnityEngine;

    public class Enemy : Entity
    {
        [Header("Enemy Components")]
        public EnemyRewards Rewards;

        protected override void Awake()
        {
            base.Awake();

            Rewards = GetComponent<EnemyRewards>();
        }

        public override void OnStartServer()
        {
            GetVital(VitalType.Health).OnDepleted += Enemy_OnDepleted;
        }

        public override void OnStopServer()
        {
            GetVital(VitalType.Health).OnDepleted -= Enemy_OnDepleted;
        }

        [Server]
        private void Enemy_OnDepleted()
        {
            Rewards.DropItems();
        }

    }
}
