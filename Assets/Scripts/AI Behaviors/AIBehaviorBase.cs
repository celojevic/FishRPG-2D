using UnityEngine;

//[CreateAssetMenu(fileName = "AIBehavior_", menuName = "FishRPG/AI Behavior/Base")]
public abstract class AIBehaviorBase : ScriptableObject
{

#if UNITY_EDITOR
    public Color GizmoColor;
#endif

    public abstract Vector2 Move(AIAgent agent, Transform target);

}
