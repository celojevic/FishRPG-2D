using UnityEngine;

[CreateAssetMenu(fileName = "AIBehavior_Flee", menuName = "FishRPG/AI Behavior/Flee")]
public class AIFlee : AIBehaviorBase
{

    public override Vector2 Move(AIAgent agent, Transform target)
    {
        Vector2 dir = (agent.transform.position - target.position).normalized;
        return dir;
    }

}
