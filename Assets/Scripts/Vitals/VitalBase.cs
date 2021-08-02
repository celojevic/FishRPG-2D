using FishNet.Object;
using System;
using UnityEngine;

public class VitalBase : NetworkBehaviour
{

    internal int CurrentVital;
    internal float Percent => (float)CurrentVital / (float)MaxVital;

    [Header("Base")]
    public int MaxVital = 50;
    public int MinVital = 0;

    protected event Action OnDepleted;

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
