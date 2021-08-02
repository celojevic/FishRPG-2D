using FishNet.Connection;
using FishNet.Object;
using FishNet.Observering;
using UnityEngine;

namespace FishNet.Managing
{

    [CreateAssetMenu(menuName = "FishNet/Observers/Scene Condition", fileName = "New Scene Condition")]
    public class SceneCondition : ObserverCondition
    {
        #region Serialized.
        /// <summary>
        /// True to synchronize which scene the object was spawned in to clients. When true this object will be moved to the clients equivelant of the scene it was spawned in on the server.
        /// </summary>
        [Tooltip("True to synchronize which scene the object was spawned in to clients. When true this object will be moved to the clients equivelant of the scene it was spawned in on the server.")]
        [SerializeField]
        private bool _synchronizeScene = false;
        /// <summary>
        /// True to continuously update network visibility. False to only update on creation or when PerformCheck is called. You may want to use true if this object will move between scenes without using the network scene manager.
        /// </summary>
        [Tooltip("True to continuously update network visibility. False to only update on creation or when PerformCheck is called. You may want to use true if this object will move between scenes without using the network scene manager.")]
        [SerializeField]
        private bool _timed = false;
        #endregion

        //private void OnEnable()
        //{
        //    if (NetworkServer.active)
        //        FlexSceneManager.AddSceneChecker(this);
        //}
        //private void OnDisable()
        //{
        //    /* Server may not be active OnDisable if object is disabled
        //     * after server shutsdown. To prevent checkers being added
        //     * but not removed RemoveSceneChecker will be called
        //     * OnDisable regardless if server or not. If client the
        //     * scene checkers list will be empty since they're only
        //     * added on server, and this will incur no penalty. */
        //    FlexSceneManager.RemoveSceneChecker(this);
        //}

        //public override bool OnSerialize(NetworkWriter writer, bool initialState)
        //{
        //    if (_synchronizeScene && initialState)
        //    {
        //        writer.WriteString(gameObject.scene.name);
        //    }
        //    return base.OnSerialize(writer, initialState);
        //}

        //public override void OnDeserialize(NetworkReader reader, bool initialState)
        //{
        //    if (_synchronizeScene && initialState)
        //    {
        //        string sceneName = reader.ReadString();
        //        Scene s = SceneManager.GetSceneByName(sceneName);
        //        if (!string.IsNullOrEmpty(s.name))
        //            SceneManager.MoveGameObjectToScene(gameObject, s);
        //        else
        //            Debug.LogWarning($"Scene could not be found for {sceneName}.");
        //    }
        //    base.OnDeserialize(reader, initialState);
        //}

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection"></param>
        public override bool ConditionMet(NetworkConnection connection)
        {
            return (connection.Scenes.Contains(NetworkObject.gameObject.scene));
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed()
        {
            return _timed;
        }

 
    }
}
