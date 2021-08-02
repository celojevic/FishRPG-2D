using FishNet.Connection;
using FishNet.Object;
using FishNet.Observering;
using UnityEngine;

namespace FishNet.Component.Observing
{
    [CreateAssetMenu(menuName = "FishNet/Observers/Distance Condition", fileName = "New Distance Condition")]
    public class DistanceCondition : ObserverCondition
    {
        #region Serialized.
        /// <summary>
        /// Maximum distance a client must be within this object to see it.
        /// </summary>
        [Tooltip("Maximum distance a client must be within this object to see it.")]
        [SerializeField]
        private float _maximumDistance = 100f;
        #endregion

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection"></param>
        public override bool ConditionMet(NetworkConnection connection)
        {
            float sqrMaximumDistance = (_maximumDistance * _maximumDistance);
            Vector3 thisPosition = base.NetworkObject.transform.position;
            foreach (NetworkObject nob in connection.Objects) 
            {
                //If within distance.
                if (Vector3.SqrMagnitude(nob.transform.position - thisPosition) <= sqrMaximumDistance)
                    return true;
            }

            /* If here no client objects are within distance. */
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
