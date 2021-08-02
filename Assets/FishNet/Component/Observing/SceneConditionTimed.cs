using FishNet.Connection;
using FishNet.Object;
using FishNet.Observering;
using UnityEngine;

namespace FishNet.Component.Observing
{
    [CreateAssetMenu(menuName = "FishNet/Observers/Scene Condition Timed", fileName = "New Scene Condition Timed")]
    public class SceneConditionTImed : ObserverCondition
    {

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection"></param>
        public override bool ConditionMet(NetworkConnection connection)
        {
            foreach (NetworkObject nob in connection.Objects)
            {
                //In same scene.
                if (nob.gameObject.scene.handle == base.NetworkObject.gameObject.scene.handle)
                    return true;
            }

            /* If here condition failed. */
            return false;
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed()
        {
            return true;
        }
    }
}
