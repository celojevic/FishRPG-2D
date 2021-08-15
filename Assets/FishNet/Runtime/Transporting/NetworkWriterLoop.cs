using FishNet.Managing.Transporting;
using UnityEngine;

namespace FishNet.Transporting
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(short.MaxValue)]
    public class NetworkWriterLoop : MonoBehaviour
    {
        #region Private.
        /// <summary>
        /// TransportManager this loop is for.
        /// </summary>
        private TransportManager _transportManager = null;
        #endregion

        private void Awake()
        {
            _transportManager = GetComponent<TransportManager>();
        }

        private void LateUpdate()
        {
            Iterate();
        }

        /// <summary>
        /// Performs read on transport.
        /// </summary>
        private void Iterate()
        {
            if (_transportManager != null)
            {
                _transportManager.IterateOutgoing(true);
                _transportManager.IterateOutgoing(false);
            }
        }

    }


}