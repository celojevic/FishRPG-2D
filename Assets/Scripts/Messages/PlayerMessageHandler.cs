using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class PlayerMessageHandler : MonoBehaviour
{

    [SerializeField] private Canvas _worldCanvas = null;
    [SerializeField] private UIActionMsg _actionMsgPrefab = null;

    private void Awake()
    {
        InstanceFinder.ClientManager.RegisterBroadcast<SendMsg>(HandleMessage);
    }

    private void OnDisable()
    {
        InstanceFinder.ClientManager.UnregisterBroadcast<SendMsg>(HandleMessage);
    }

    void HandleMessage(SendMsg msg)
    {
        switch (msg.Type)
        {
            case MessageType.Action:
                SpawnActionMsg(transform.position, msg);
                break;

            case MessageType.Chat:
                break;
        }
    }

    public void SpawnActionMsg(Vector2 pos, SendMsg msg)
    {
        var actionMsg = Instantiate(_actionMsgPrefab,
            pos + (Vector2.up + Random.insideUnitCircle) / 2f, // adds offset and randomness
            Quaternion.identity,
            _worldCanvas.transform
        );
        actionMsg.Setup(msg.Text, msg.Color);
    }

    #region Server

    // TODO if conn == null, send to observers
    //NetworkManager.ServerManager.Broadcast(NetworkObject.Observers, msg, true, FishNet.Transporting.Channel.Reliable);
    [Server]
    public static void SendPlayerMsg(NetworkConnection conn, MessageType type, string text, 
        Color color = new Color())
    {
        if (conn != null)
        {
            //TargetSendActionMsg(conn, text, color);
            conn.Broadcast(new SendMsg
            {
                Type = type,
                Text = text,
                Color = color
            });
        }
    }

    #endregion

}

public struct SendMsg : IBroadcast
{
    public MessageType Type;
    public string Text;
    public Color Color;
}

public enum MessageType
{
    Action,
    Chat,
}