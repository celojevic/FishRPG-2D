namespace FishRPG.AI.Movement
{
    using UnityEngine;

    //[CreateAssetMenu(fileName = "AIBehavior_", menuName = "FishRPG/AI/Movement/Base")]
    public abstract class AIMovementBehaviorBase : ScriptableObject
    {

#if UNITY_EDITOR
        public Color GizmoColor;
#endif

        public abstract Vector2 Move(AIAgent agent, Transform target);

    }
}
