﻿namespace FishNet.Managing.Scened.Data
{
    public class LoadParams
    {
        /// <summary>
        /// Objects which are included in callbacks on the server when loading a scene. Can be useful for including unique information about the scene, such as match id. These are not sent to clients; use ClientParams for this.
        /// </summary>
        [System.NonSerialized]
        public object[] ServerParams = null;
        /// <summary>
        /// Bytes which are sent to clients during scene loads. Can contain any information.
        /// </summary>
        public byte[] ClientParams = null;
    }

}