using UnityEngine;

[CreateAssetMenu(fileName = "AIBehavior_CircleTarget", menuName = "FishRPG/AI Behavior/Circle Target")]
public class AICircleTarget : AIBehaviorBase
{

    public override Vector2 Move(AIAgent agent, Transform target)
    {
        Vector2 dirToTarget = (target.position - agent.transform.position).normalized;

        return Vector2.Perpendicular(dirToTarget);
    }

}
