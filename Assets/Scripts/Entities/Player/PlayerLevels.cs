using FishNet.Object;
using System;
using UnityEngine;

public class PlayerLevels : NetworkBehaviour
{

    public SkillLevel[] Levels;

    [Server]
    public void AddExp(SkillBase skill, int amount)
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            if (Levels[i].Skill == skill)
            {
                Levels[i].AddExp(amount);
                break;
            }
        }
    }


}

[Serializable]
public class SkillLevel
{

    public SkillBase Skill;
    public int MaxLevel = 99;
    public float ExpTNL = -1;

    private int _currentLevel = 1;
    public int CurrentLevel
    {
        get => _currentLevel;
        set
        {
            _currentLevel = value;
            OnLevelChanged?.Invoke();
        }
    }

    private float _currentExp;
    public float CurrentExp
    {
        get => _currentExp;
        set
        {
            _currentExp = value;
            OnExpChanged?.Invoke();
        }
    }

    public event Action OnLevelChanged;
    public event Action OnExpChanged;

    public void AddExp(int amount)
    {
        if (ExpTNL < 0)
            CalcExpTNL();

        CurrentExp = Mathf.Clamp(CurrentExp + amount, 0f, ExpTNL);
        if (CurrentExp >= ExpTNL)
        {
            CurrentLevel++;
            CurrentExp = 0;
            CalcExpTNL();
        }
    }

    void CalcExpTNL() => ExpTNL = Experience.CalcExpTNL(CurrentLevel);

}

public static class Experience
{

    public static float CalcExpTNL(int level)
    {
        return Mathf.Round((4 * (Mathf.Pow(level, 3))) / 5);
    }
}
