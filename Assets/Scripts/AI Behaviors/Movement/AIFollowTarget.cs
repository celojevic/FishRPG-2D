namespace FishRPG.AI.Movement
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AIMovement_FollowTarget", menuName = "FishRPG/AI/Movement/Follow Target")]
    public class AIFollowTarget : AIMovementBehaviorBase
    {

        public override Vector2 Move(AIAgent agent, Transform target)
        {
            Vector2 dir = (target.position - agent.transform.position).normalized;

            return dir;
        }

    }
}
