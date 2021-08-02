using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FishNet.Observering
{
    /// <summary>
    /// Can use multiple observer conditions to grant a connection awareness of this object.
    /// Timed checks will continue to run even when an object is disabled, unless it's deinitialized. 
    /// </summary>
    public class NetworkObserver : NetworkBehaviour
    {
        #region Serialized.
        /// <summary>
        /// Conditions connections must met to be added as an observer.
        /// </summary>
        [Tooltip("Conditions connections must met to be added as an observer.")]
        [SerializeField]
        private List<ObserverCondition> _observerConditions = new List<ObserverCondition>();
        #endregion

        #region Private.
        /// <summary>
        /// NetworkObject this belongs to.
        /// </summary>
        private NetworkObject _networkObject;
        /// <summary>
        /// Becomes true when registered with ServerObjects as Timed observers.
        /// </summary>
        private bool _registeredAsTimed = false;
        /// <summary>
        /// True if has timed conditions.
        /// </summary>
        private bool _hasTimedConditions = false;
        /// <summary>
        /// Found renderers on and beneath this object.
        /// </summary>
        private Renderer[] _renderers = new Renderer[0];
        #endregion

        private void OnEnable()
        {
            if (_networkObject != null && _networkObject.IsServer)
                RegisterTimedConditions();
        }
        private void OnDisable()
        {
            if (_networkObject != null && _networkObject.Deinitializing)
                UnregisterTimedConditions();
        }
        private void OnDestroy()
        {
            if (_networkObject != null)
                UnregisterTimedConditions();
        }

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        /// <param name="networkManager"></param>
        internal void PreInitialize(NetworkObject networkObject)
        {
            _networkObject = networkObject;

            bool observerFound = false;
            for (int i = 0; i < _observerConditions.Count; i++)
            {
                if (_observerConditions[i] != null)
                {
                    observerFound = true;
                    _observerConditions[i].FirstInitialize(_networkObject);
                    //If timed also register as containing timed conditions.
                    if (_observerConditions[i].Timed())
                        _hasTimedConditions = true;
                }
                else
                {
                    _observerConditions.RemoveAt(i);
                    i--;
                }
            }
            //No observers specified 
            if (!observerFound)
            {
                Debug.LogWarning($"NetworkObserver exist on {gameObject.name} but there are no observer conditions. This script has been removed.");
                Destroy(this);
                return;
            }


            RegisterTimedConditions();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>
        /// Returns ObserverStateChange by comparing conditions for a connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>True if added to Observers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ObserverStateChange RebuildObservers(NetworkConnection connection)
        {
            bool currentlyAdded = (_networkObject.Observers.Contains(connection));
            //True if all conditions are met.
            bool allConditionsMet = true;

            //Only check if not owner. Owner should always be aware of their objects.
            if (connection != _networkObject.Owner)
            {
                for (int i = 0; i < _observerConditions.Count; i++)
                {
                    ObserverCondition condition = _observerConditions[i];
                    /* If any observer returns removed then break
                     * from loop and return removed. If one observer has
                     * removed then there's no reason to iterate
                     * the rest. */
                    bool conditionMet = condition.ConditionMet(connection);
                    //Condition not met.
                    if (!conditionMet)
                    {
                        allConditionsMet = false;
                        break;
                    }
                }
            }

            //If all conditions met.
            if (allConditionsMet)
                return ReturnPassedConditions(currentlyAdded);
            else
                return ReturnFailedCondition(currentlyAdded);
        }

        /// <summary>
        /// Registers timed conditions.
        /// </summary>
        private void RegisterTimedConditions()
        {
            if (!_hasTimedConditions)
                return;
            //Already registered or no timed conditions.
            if (_registeredAsTimed)
                return;

            _registeredAsTimed = true;
            _networkObject.NetworkManager.ServerManager.Objects.AddTimedNetworkObserver(_networkObject);
        }

        /// <summary>
        /// Unregisters timed conditions.
        /// </summary>
        private void UnregisterTimedConditions()
        {
            if (!_hasTimedConditions)
                return;
            if (!_registeredAsTimed)
                return;

            _registeredAsTimed = false;
            _networkObject.NetworkManager.ServerManager.Objects.RemoveTimedNetworkObserver(_networkObject);
        }

        /// <summary>
        /// Returns an ObserverStateChange when a condition fails.
        /// </summary>
        /// <param name="currentlyAdded"></param>
        /// <returns></returns>
        private ObserverStateChange ReturnFailedCondition(bool currentlyAdded)
        {
            if (currentlyAdded)
            {
                SetRenderers(false);
                return ObserverStateChange.Removed;
            }
            else
            {
                return ObserverStateChange.Unchanged;
            }
        }

        /// <summary>
        /// Returns an ObserverStateChange when all conditions pass.
        /// </summary>
        /// <param name="currentlyAdded"></param>
        /// <returns></returns>
        private ObserverStateChange ReturnPassedConditions(bool currentlyAdded)
        {
            if (currentlyAdded)
            {
                return ObserverStateChange.Unchanged;
            }
            else
            {
                SetRenderers(true);
                return ObserverStateChange.Added;
            }
        }

        /// <summary>
        /// Sets renderers enabled state.
        /// </summary>
        /// <param name="enable"></param>
        private void SetRenderers(bool enable)
        {
            //If client isn't active no reason to update renderers.
            if (!_networkObject.IsClient)
                return;

            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].enabled = enable;
        }

    }
}
