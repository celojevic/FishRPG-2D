using FishNet.Broadcast;
using UnityEngine;

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
