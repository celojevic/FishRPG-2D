using FishNet.Object;
using UnityEngine;

public class ResourceNode : NetworkBehaviour
{

    public ResourceData Data;

    [SerializeField] private SpriteRenderer _sr = null;

    [Server]
    public bool MeetsRequirements(Player player)
    {
        if (!player.HasTool(Data.ToolReq))
        {
            // TODO throws error
            //PlayerMessageHandler.SendPlayerMsg(player.Owner, MessageType.Action, "Wrong tool!", Color.gray);
            return false;
        }

        // TODO level req

        return true;
    }

    [ObserversRpc]
    public void RpcSpawnImpactAnim()
    {
        if (Data.OnHitFX)
            Instantiate(Data.OnHitFX, transform.position + new Vector3(0f,0.5f), Quaternion.identity);
    }

    #region Editor
#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_sr != null && Data != null)
            _sr.sprite = Data.FullSprite;
    }

#endif
    #endregion

}
