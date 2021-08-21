using FishNet.Object;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{

    [Header("Basic Attack")]
    [SerializeField] private GameObject _basicSwingPrefab = null;
    [SerializeField] private GameObject _basicImpactPrefab = null;
    [SerializeField] private float _aoeRadius = 0.5f;
    [SerializeField] private float _range = 1f;
    [SerializeField] private int _baseDmg = 10;

    private Player _player;
    private float _attackTimer;
    private Vector2 _attackDir;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (!base.IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            _attackDir = (Utils.GetWorldMousePos() - transform.position).normalized;
            CmdBasicAttack(_attackDir);
        }
    }

    [ServerRpc]
    void CmdBasicAttack(Vector2 dir)
    {
        dir.Normalize();
        SpawnSwingAnimation(_player.GetCenter(), dir);

        // TODO this is only if skill animation doesnt have collider
        var hits = Physics2D.OverlapCircleAll(_player.GetCenter() + dir * _range, _aoeRadius, 
            LayerMask.GetMask("Enemy"));
        if (hits.IsValid())
        {
            foreach (var item in hits)
            {
                if (item.isTrigger)
                {
                    Health h = item.GetComponent<Health>();
                    if (h)
                        h.Subtract(_baseDmg);
                    SpawnImpactAnim(item.transform.position);
                }
            }
        }
    }

    [ObserversRpc]
    void SpawnImpactAnim(Vector2 pos)
    {
        if (_basicImpactPrefab)
            Instantiate(_basicImpactPrefab, pos, Quaternion.identity);
    }

    [ObserversRpc]
    void SpawnSwingAnimation(Vector2 pos, Vector2 dir)
    {
        if (_basicSwingPrefab)
        {
            Instantiate(_basicSwingPrefab, pos, 
                Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward)
            );
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(_player.GetCenter(), _attackDir);
        //Gizmos.DrawWireSphere(_player.GetCenter() + _attackDir.normalized * _range, _aoeRadius);
    }

}
