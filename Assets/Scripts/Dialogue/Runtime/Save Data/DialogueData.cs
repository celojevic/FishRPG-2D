namespace FishRPG.Dialogue.Runtime
{
    using System.Collections.Generic;
    using UnityEngine;

    public class DialogueData : ScriptableObject
    {

        public List<DialogueNodeData> Nodes = new List<DialogueNodeData>();
        public List<NodeEdgeData> Edges = new List<NodeEdgeData>();

    }
}
