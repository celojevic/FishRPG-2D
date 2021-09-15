using UnityEngine;

[CreateAssetMenu(fileName = "AIBehavior_FollowTarget", menuName = "FishRPG/AI Behavior/Follow Target")]
public class AIFollowTarget : AIBehaviorBase
{

    public override Vector2 Move(AIAgent agent, Transform target)
    {
        Vector2 dir = (target.position - agent.transform.position).normalized;

        return dir;
    }

}
