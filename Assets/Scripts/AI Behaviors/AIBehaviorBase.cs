using UnityEngine;

//[CreateAssetMenu(fileName = "AIBehavior_", menuName = "FishRPG/AI Behavior/Base")]
public abstract class AIBehaviorBase : ScriptableObject
{

    public abstract Vector2 Move(AIAgent agent, Transform target);

}
