namespace FishRPG.Dialogue.Runtime
{
    using System.Collections.Generic;
    using UnityEngine;

    public class DialogueData : ScriptableObject
    {

        public List<DialogueNodeData> Nodes = new List<DialogueNodeData>();
        public List<EdgeData> Edges = new List<EdgeData>();

    }
}
