namespace FishRPG.AI.Movement
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AIBehavior_CircleTarget", menuName = "FishRPG/AI/Movement/Circle Target")]
    public class AICircleTarget : AIMovementBehaviorBase
    {

        public override Vector2 Move(AIAgent agent, Transform target)
        {
            Vector2 dirToTarget = (target.position - agent.transform.position).normalized;

            return Vector2.Perpendicular(dirToTarget);
        }

    }
}
