using FishNet.Managing.Transporting;
using UnityEngine;

namespace FishNet.Transporting
{

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(short.MinValue)]
    public class NetworkReaderLoop : MonoBehaviour
    {
        #region Private.
        /// <summary>
        /// TransportManager this loop is for.
        /// </summary>
        private TransportManager _transportManager = null;
        /// <summary>
        /// Last frame which iteration occurred.
        /// </summary>
        private int _lastIterateFrame = -1;
        #endregion

        private void Awake()
        {
            _transportManager = GetComponent<TransportManager>();
        }

        private void FixedUpdate()
        {
            Iterate();
        }
        private void Update()
        {
            //Don't iterate if in fixed step as fixedUpdate already iterated first.
            if (!Time.inFixedTimeStep)
                Iterate();
        }

        /// <summary>
        /// Performs read on transport.
        /// </summary>
        private void Iterate()
        {
            //No reason to iterate more than once. Can occur from fixedupdate iterate call.
            if (Time.frameCount == _lastIterateFrame)
                return;
            _lastIterateFrame = Time.frameCount;

            if (_transportManager != null)
            {
                _transportManager.IterateIncoming(true);
                _transportManager.IterateIncoming(false);
            }
        }

    }


}