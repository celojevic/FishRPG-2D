namespace FishRPG.AI.Movement
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AIMovement_Composite", menuName = "FishRPG/AI/Movement/Composite")]
    public class AIMovementComposite : AIMovementBehaviorBase
    {

        public AIBehaviorWeights[] AIBehaviorWeights;

        public override Vector2 Move(AIAgent agent, Transform target)
        {
            if (!AIBehaviorWeights.IsValid()) return Vector2.zero;

            Vector2 compositeDir = Vector2.zero;

            for (int i = 0; i < AIBehaviorWeights.Length; i++)
            {
                compositeDir +=
                    AIBehaviorWeights[i].AIBehavior.Move(agent, target) * AIBehaviorWeights[i].Weight;
            }

            return compositeDir;
        }

    }

    [System.Serializable]
    public class AIBehaviorWeights
    {
        public AIMovementBehaviorBase AIBehavior;

        [Range(0f, 5f)]
        public float Weight = 1f;
    }
}
