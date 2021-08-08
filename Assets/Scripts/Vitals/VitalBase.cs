using FishNet.Object;
using System;
using UnityEngine;

public class VitalBase : NetworkBehaviour
{

    private int _current;
    internal int CurrentVital
    {
        get => _current;
        set
        {
            // same value
            if (value == _current) return;

            _current = value;
            OnVitalChanged?.Invoke();
        }
    }

    internal float Percent => (float)CurrentVital / (float)MaxVital;

    [Header("Base")]
    public int MaxVital = 50;
    public int MinVital = 0;

    /// <summary>
    /// Invoked whenever the vital hits 0. Kind of like OnDeath for Health.
    /// </summary>
    protected event Action OnDepleted;
    /// <summary>
    /// Invoked any time the vital changes.
    /// </summary>
    public event Action OnVitalChanged;

    private void Start()
    {
        CurrentVital = MaxVital;
    }

    public void Add(int amount)
    {
        CurrentVital = Mathf.Clamp(CurrentVital + amount, MinVital, MaxVital);
    }

    public void Subtract(int amount)
    {
        CurrentVital = Mathf.Clamp(CurrentVital - amount, MinVital, MaxVital);

        if (CurrentVital == MinVital)
            OnDepleted?.Invoke();
    }

}

public enum VitalType
{
    Health,
    Mana,
}
