using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Resources/Data")]
public class ResourceData : ScriptableObject
{

    [Header("Visuals")]
    public Sprite FullSprite;
    public Sprite DepletedSprite;
    public RuntimeAnimatorController FullController;
    public RuntimeAnimatorController DepletedController;

}
