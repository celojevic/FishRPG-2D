using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public class VitalBase : NetworkBehaviour
{

    [SyncVar(OnChange = nameof(OnCurrentChanged))] private int _current;
    internal int CurrentVital
    {
        get => _current;
        set
        {
            // same value
            if (value == _current) return;

            _current = value;
        }
    }
    void OnCurrentChanged(int oldVal, int newVal, bool asServer) => OnVitalChanged?.Invoke();

    internal float Percent => (float)CurrentVital / (float)MaxVital;

    [Header("Base")]
    public int MaxVital = 50;
    /// <summary>
    /// Minimum vital the object can have.
    /// Kept private to avoid accidentally changing in inspector.
    /// </summary>
    private int _minVital = 0;

    /// <summary>
    /// Invoked whenever the vital hits 0. Kind of like OnDeath for Health.
    /// </summary>
    public event Action OnDepleted;
    /// <summary>
    /// Invoked any time the vital changes.
    /// </summary>
    public event Action OnVitalChanged;

    public override void OnStartServer()
    {
        CurrentVital = MaxVital;
    }

    [Server]
    public virtual void Add(int amount)
    {
        CurrentVital = Mathf.Clamp(CurrentVital + amount, _minVital, MaxVital);
    }

    [Server]
    public virtual void Subtract(int amount)
    {
        CurrentVital = Mathf.Clamp(CurrentVital - amount, _minVital, MaxVital);

        if (CurrentVital == _minVital)
            OnDepleted?.Invoke();
    }

}

public enum VitalType
{
    Health,
    Mana,

    // KEEP LAST
    Count
}
