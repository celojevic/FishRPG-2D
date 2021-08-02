using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.Serializing.Helping
{

    public class Comparers : MonoBehaviour
    {
        /// <summary>
        /// Returns if A equals B using EqualityCompare.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool EqualityCompare<T>(T a, T b)
        {
            return (EqualityComparer<T>.Default.Equals(a, b));
        }

    }
}
