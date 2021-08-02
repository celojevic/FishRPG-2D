using FishNet.Object;
using FishNet.Object.Helping;

namespace FishNet.CodeGenerating.Helping
{

    public static class AttributeHelper
    {
        #region Reflection references.
        private static string ServerAttribute_FullName;
        private static string ClientAttribute_FullName;
        private static string ServerRpcAttribute_FullName;
        private static string ObserversRpcAttribute_FullName;
        private static string TargetRpcAttribute_FullName;
        private static string SyncVarAttribute_FullName;
        private static string SyncObjectAttribute_FullName;
        #endregion   

        static AttributeHelper()
        {
            ServerAttribute_FullName = typeof(ServerAttribute).FullName;
            ClientAttribute_FullName = typeof(ClientAttribute).FullName;
            ServerRpcAttribute_FullName = typeof(ServerRpcAttribute).FullName;
            ObserversRpcAttribute_FullName = typeof(ObserversRpcAttribute).FullName;
            TargetRpcAttribute_FullName = typeof(TargetRpcAttribute).FullName;
            SyncVarAttribute_FullName = typeof(SyncVarAttribute).FullName;
            SyncObjectAttribute_FullName = typeof(SyncObjectAttribute).FullName;
        }

        /// <summary>
        /// Returns type of Rpc attributeFullName is for.
        /// </summary>
        /// <param name="attributeFullName"></param>
        /// <returns></returns>
        public static RpcType GetRpcAttributeType(string attributeFullName)
        {
            if (attributeFullName == ServerRpcAttribute_FullName)
                return RpcType.Server;
            else if (attributeFullName == ObserversRpcAttribute_FullName)
                return RpcType.Observers;
            else if (attributeFullName == TargetRpcAttribute_FullName)
                return RpcType.Target;
            else
                return RpcType.None;
        }


        /// <summary>
        /// Returns type of Rpc attributeFullName is for.
        /// </summary>
        /// <param name="attributeFullName"></param>
        /// <returns></returns>
        internal static QolAttributeType GetQolAttributeType(string attributeFullName)
        {
            if (attributeFullName == ServerAttribute_FullName)
                return QolAttributeType.Server;
            else if (attributeFullName == ClientAttribute_FullName)
                return QolAttributeType.Client;
            else
                return QolAttributeType.None;
        }


        /// <summary>
        /// Returns if attribute if a SyncVarAttribute.
        /// </summary>
        /// <param name="attributeFullName"></param>
        /// <returns></returns>
        public static bool IsSyncVarAttribute(string attributeFullName)
        {
            return (attributeFullName == SyncVarAttribute_FullName);
        }

        /// <summary>
        /// Returns if attribute if a SyncObjectAttribute.
        /// </summary>
        /// <param name="attributeFullName"></param>
        /// <returns></returns>
        public static bool IsSyncObjectAttribute(string attributeFullName)
        {
            return (attributeFullName == SyncObjectAttribute_FullName);
        }
    }

}