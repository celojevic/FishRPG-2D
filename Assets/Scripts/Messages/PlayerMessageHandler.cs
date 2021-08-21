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
                SpawnActionMsg(msg);
                break;

            case MessageType.Chat:
                break;
        }
    }

    public void SpawnActionMsg(SendMsg msg)
    {
        var actionMsg = Instantiate(
            _actionMsgPrefab,
            (Vector2)msg.Go.transform.position + (Vector2.up + Random.insideUnitCircle) / 2f, // adds offset and randomness
            Quaternion.identity,
            msg.Go.GetComponentInChildren<Canvas>()?.transform
        );
        actionMsg.Setup(msg.Text, msg.Color);
    }

    #region Server

    [Server]
    public static void SendPlayerMsg(NetworkConnection conn, MessageType type, string text, 
        Color color = new Color(), GameObject go = null)
    {
        var msg = new SendMsg
        {
            Type = type,
            Text = text,
            Color = color,
            Go = go
        };

        if (conn != null)
        {
            conn.Broadcast(msg);
        }
        else
        {
            InstanceFinder.ServerManager.Broadcast(msg);
        }
    }

    #endregion

}

public struct SendMsg : IBroadcast
{

    public MessageType Type;

    public string Text;

    public Color Color;

    public GameObject Go;

}

public enum MessageType
{
    Action,
    Chat,
}