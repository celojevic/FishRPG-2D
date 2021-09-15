namespace FishRPG.AI.Movement
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AIBehavior_Flee", menuName = "FishRPG/AI/Movement/Flee")]
    public class AIFlee : AIMovementBehaviorBase
    {

        public override Vector2 Move(AIAgent agent, Transform target)
        {
            Vector2 dir = (agent.transform.position - target.position).normalized;
            return dir;
        }

    }
}