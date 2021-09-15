using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Resources/Data")]
public class ResourceData : ScriptableObject
{

    [Header("Requirements")]
    public ToolType ToolReq;
    public int LevelReq = 1;

    [Header("Visuals")]
    public Sprite FullSprite;
    public Sprite DepletedSprite;
    public RuntimeAnimatorController FullController;
    public RuntimeAnimatorController DepletedController;
    public GameObject OnHitFX;

}
