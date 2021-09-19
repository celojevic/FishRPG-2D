#if UNITY_EDITOR
namespace FishRPG.Dialogue.Editor
{
    using UnityEditor.Experimental.GraphView;

    public class DialogueNode : Node
    {

        public string Guid;

        public string Text;

        public bool IsEntryPoint;

    }
}
#endif