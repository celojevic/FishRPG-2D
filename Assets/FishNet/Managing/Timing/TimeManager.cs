using System;
using UnityEngine;

namespace FishNet.Managing.Timing
{
    [DisallowMultipleComponent]
    public class TimeManager : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Called right before a tick occurs.
        /// </summary>
        public event Action<uint> OnPreTick;
        /// <summary>
        /// Called when a tick occurs.
        /// </summary>
        public event Action<uint> OnTick;
        /// <summary>
        /// Called after a tick occurs; physics would have simulated if using ManuallySimulatePhysics.
        /// </summary>
        public event Action<uint> OnPostTick;
        /// <summary>
        /// Called when MonoBehaviours call Update.
        /// </summary>
        public event Action OnUpdate;
        /// <summary>
        /// Called when MonoBehaviours call LateUpdate.
        /// </summary>
        public event Action OnLateUpdate;
        /// <summary>
        /// Called when MonoBehaviours call FixedUpdate.
        /// </summary>
        public event Action OnFixedUpdate;
        /// <summary>
        /// Current network tick.
        /// </summary>
        public uint Tick { get; private set; }
        #endregion

        #region Serialized.
        /// <summary>
        /// True to disable auto physics simulation and simulate physics after each tick.
        /// </summary>
        [Tooltip("True to disable auto physics simulation and simulate physics after each tick.")]
        [SerializeField]
        private bool _manuallySimulatePhysics = false;
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("How many times per second the server will simulate; simulation rate is used for state control.")]
        [SerializeField]
        private ushort _simulationRate = 60;
        /// <summary>
        /// How many times per second the server will simulate; simulation rate is used for state control.
        /// </summary>
        public ushort SimulationRate => _simulationRate;
        #endregion

        #region Private.
        /// <summary>
        /// Stopwatch used to calculate ticks.
        /// </summary>
        System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        /// <summary>
        /// Time elapsed after ticks. This is extra time beyond the simulation rate.
        /// </summary>
        private double _elapsedTime = 0f;
        /// <summary>
        /// NetworkManager used with this.
        /// </summary>
        private NetworkManager _networkManager;
        /// <summary>
        /// True if can tick.
        /// </summary>
        private bool CanTick => (_networkManager != null) && (_networkManager.IsServer || _networkManager.IsClient);
        #endregion

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        private void Update()
        {
            IncreaseTick();
            OnUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        internal void FirstInitialize(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            if (_manuallySimulatePhysics)
            {
                Physics.autoSimulation = false;
#if !UNITY_2020_2_OR_NEWER
                Physics2D.autoSimulation = false;
#else
                Physics2D.simulationMode = SimulationMode2D.Script;
#endif           
            }
        }

        /// <summary>
        /// Increases the based on simulation rate.
        /// </summary>
        private void IncreaseTick()
        {
            //Server nor client is running.
            if (!CanTick)
            {
                _stopwatch.Stop();
                return;
            }
            //Server or client is running.
            else
            {
                //If stopwatch isn't running then restart it.
                if (!_stopwatch.IsRunning)
                    _stopwatch.Restart();
            }

            double timePerSimulation = 1d / SimulationRate;
            _elapsedTime += (_stopwatch.ElapsedMilliseconds / 1000d);

            while (_elapsedTime >= timePerSimulation)
            {
                OnPreTick?.Invoke(Tick);

                Tick++;
                OnTick?.Invoke(Tick);

                if (_manuallySimulatePhysics)
                {
                    Physics.Simulate((float)timePerSimulation);
                    Physics2D.Simulate((float)timePerSimulation);
                }
                OnPostTick?.Invoke(Tick);

                _elapsedTime -= timePerSimulation;
            }

            _stopwatch.Restart();
        }

        /// <summary>
        /// Converts float time to ticks.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public uint TimeToTicks(float time)
        {
            return (uint)Mathf.RoundToInt(time / (1f / SimulationRate));
        }

    }

}