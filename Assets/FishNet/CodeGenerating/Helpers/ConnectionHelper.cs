using Mono.Cecil;
using FishNet.Connection;

namespace FishNet.CodeGenerating.Helping
{
    internal static class ConnectionHelper
    {
        #region Reflection references.
        private static TypeReference NetworkConnection_TypeRef;
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {         
            NetworkConnection_TypeRef = moduleDef.ImportReference(typeof(NetworkConnection));

            return true;
        }

    }
}