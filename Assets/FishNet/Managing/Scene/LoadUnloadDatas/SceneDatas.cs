using FishNet.Object;
using UnityEngine.SceneManagement;

namespace FishNet.Managing.Scened.Data
{


    public class SingleSceneData
    {
        /// <summary>
        /// SceneReferenceData for each scene to load.
        /// </summary>
        public SceneReferenceData SceneReferenceData;
        /// <summary>
        /// NetworkIdentities to move to the new single scene.
        /// </summary>
        public NetworkObject[] MovedNetworkIdentities;

        /// <summary>
        /// String to display when a scene name is null or empty.
        /// </summary>
        private const string NULL_EMPTY_SCENE_NAME = "SingleSceneData is being generated using a null or empty sceneName. If this was intentional, you may ignore this warning.";

        public SingleSceneData()
        {
            SceneReferenceData = new SceneReferenceData();
            MovedNetworkIdentities = new NetworkObject[0];
        }

        public SingleSceneData(string sceneName) : this(sceneName, null) { }
        public SingleSceneData(string sceneName, NetworkObject[] movedNetworkIdentities)
        {
            if (string.IsNullOrEmpty(sceneName))
                UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

            SceneReferenceData = new SceneReferenceData() { Name = sceneName };

            if (movedNetworkIdentities == null)
                movedNetworkIdentities = new NetworkObject[0];
            MovedNetworkIdentities = movedNetworkIdentities;
        }

        public SingleSceneData(SceneReferenceData sceneReferenceData) : this(sceneReferenceData, null) { }
        public SingleSceneData(SceneReferenceData sceneReferenceData, NetworkObject[] movedNetworkIdentities)
        {
            SceneReferenceData = sceneReferenceData;

            if (movedNetworkIdentities == null)
                movedNetworkIdentities = new NetworkObject[0];
            MovedNetworkIdentities = movedNetworkIdentities;
        }
    }

    public class AdditiveScenesData
    {
        /// <summary>
        /// SceneReferenceData for each scene to load.
        /// </summary>
        public SceneReferenceData[] SceneReferenceDatas;
        /// <summary>
        /// NetworkIdentities to move to the new single scene.
        /// </summary>
        public NetworkObject[] MovedNetworkIdentities;

        /// <summary>
        /// String to display when scene names is null or of zero length.
        /// </summary>
        private const string NULL_SCENE_NAME_COLLECTION = "AdditiveScenesData is being generated using null or empty sceneNames. If this was intentional, you may ignore this warning.";
        /// <summary>
        /// String to display when a scene name is null or empty.
        /// </summary>
        private const string NULL_EMPTY_SCENE_NAME = "AdditiveSceneData is being generated using a null or empty sceneName. If this was intentional, you may ignore this warning.";

        public AdditiveScenesData()
        {
            SceneReferenceDatas = new SceneReferenceData[0];
            MovedNetworkIdentities = new NetworkObject[0];
        }

        public AdditiveScenesData(string[] sceneNames) : this(sceneNames, null) { }
        public AdditiveScenesData(string[] sceneNames, NetworkObject[] movedNetworkIdentities)
        {
            if (sceneNames == null || sceneNames.Length == 0)
                UnityEngine.Debug.LogWarning(NULL_SCENE_NAME_COLLECTION);

            SceneReferenceDatas = new SceneReferenceData[sceneNames.Length];
            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (string.IsNullOrEmpty(sceneNames[i]))
                    UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

                SceneReferenceDatas[i] = new SceneReferenceData { Name = sceneNames[i] };
            }

            if (movedNetworkIdentities == null)
                movedNetworkIdentities = new NetworkObject[0];
            MovedNetworkIdentities = movedNetworkIdentities;
        }


        public AdditiveScenesData(SceneReferenceData[] sceneReferenceDatas) : this(sceneReferenceDatas, null) { }

        public AdditiveScenesData(SceneReferenceData[] sceneReferenceDatas, NetworkObject[] movedNetworkIdentities)
        {
            SceneReferenceDatas = sceneReferenceDatas;

            if (movedNetworkIdentities == null)
                movedNetworkIdentities = new NetworkObject[0];
            MovedNetworkIdentities = movedNetworkIdentities;
        }


        public AdditiveScenesData(Scene[] scenes) : this(scenes, null) { }
        public AdditiveScenesData(Scene[] scenes, NetworkObject[] movedNetworkIdentities)
        {
            if (scenes == null || scenes.Length == 0)
                UnityEngine.Debug.LogWarning(NULL_SCENE_NAME_COLLECTION);

            SceneReferenceDatas = new SceneReferenceData[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                if (string.IsNullOrEmpty(scenes[i].name))
                    UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

                SceneReferenceDatas[i] = new SceneReferenceData(scenes[i]);
            }

            if (movedNetworkIdentities == null)
                movedNetworkIdentities = new NetworkObject[0];
            MovedNetworkIdentities = movedNetworkIdentities;
        }
    }


}