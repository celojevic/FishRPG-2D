using FishNet.Managing.Scened.Data;
using FishNet.Broadcast;

namespace FishNet.Managing.Scened.Messaging
{

    /// <summary>
    /// Sent to clients to load networked scenes.
    /// </summary>
    public struct LoadScenesMessage : IBroadcast
    {
        public LoadSceneQueueData SceneQueueData;
    }


    /// <summary>
    /// Sent to clients to unload networked scenes.
    /// </summary>
    public struct UnloadScenesMessage : IBroadcast
    {
        public UnloadSceneQueueData SceneQueueData;
    }


    /// <summary>
    /// Sent to server to indicate which scenes a client has loaded.
    /// </summary>
    public struct ClientScenesLoadedMessage : IBroadcast
    {
        public SceneReferenceData[] SceneDatas;
    }

    public struct ClientPlayerCreated : IBroadcast
    {

    }

}