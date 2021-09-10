using FishNet.Object;
using UnityEngine;

public class Enemy : Entity
{

    public EnemyRewards Rewards;

    protected override void Awake()
    {
        base.Awake();
        Rewards = GetComponent<EnemyRewards>();

        GetVital(VitalType.Health).OnDepleted += Enemy_OnDepleted;
    }

    private void Enemy_OnDepleted()
    {

        Despawn();
    }

    private void OnDestroy()
    {
        GetVital(VitalType.Health).OnDepleted -= Enemy_OnDepleted;
    }


}
