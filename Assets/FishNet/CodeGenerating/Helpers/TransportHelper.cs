using Mono.Cecil;
using FishNet.Transporting;

namespace FishNet.CodeGenerating.Helping
{
    internal static class TransportHelper
    {
        #region Reflection references.        
        internal static TypeReference Channel_TypeRef;
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {
           // Channel_ParameterDef_FullName = typeof(Channel).FullName;
            Channel_TypeRef = moduleDef.ImportReference(typeof(Channel));

            return true;
        }

    }
}