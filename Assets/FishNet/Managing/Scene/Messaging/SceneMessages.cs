using FishNet.Managing.Scened.Data;
using FishNet.Broadcast;
using FishNet.Serializing.Helping;

namespace FishNet.Managing.Scened.Messaging
{

    /// <summary>
    /// Sent to clients to load networked scenes.
    /// </summary>
    [CodegenIncludeInternal]   
    public struct LoadScenesMessage : IBroadcast
    {   
        public LoadSceneQueueData SceneQueueData;
    }
           
    /// <summary>       
    /// Sent to clients to unload networked scenes.
    /// </summary>     
    [CodegenIncludeInternal]
    public struct UnloadScenesMessage : IBroadcast
    {
        public UnloadSceneQueueData SceneQueueData;
    }
       
    /// <summary> 
    /// Sent to server to indicate which scenes a client has loaded.
    /// </summary>
    [CodegenIncludeInternal]
    public struct ClientScenesLoadedMessage : IBroadcast
    {
        public SceneReferenceData[] SceneDatas;
    }
     
}