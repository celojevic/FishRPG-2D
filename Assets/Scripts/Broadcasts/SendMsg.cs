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
    /// <summary>
    /// Action messages are floating messages shown in the world, such as damage numbers.
    /// </summary>
    Action,

    Chat,
}
