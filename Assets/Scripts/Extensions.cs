using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{

    #region RectOffset

    public static RectInt ToRectInt(this RectOffset rectOffset)
    {
        return new RectInt(rectOffset.left, rectOffset.bottom, rectOffset.right, rectOffset.top);
    }

    #endregion

    #region RectInt

    public static RectOffset ToRectOffset(this RectInt rectInt)
    {
        return new RectOffset(rectInt.xMin, rectInt.yMin, rectInt.width, rectInt.height);
    }

    #endregion

    #region T - IsValid

    public static bool IsValid<T>(this T[] array) => array != null && array.Length > 0;

    public static bool IsValid<T>(this List<T> list) => list != null && list.Count > 0;

    public static bool IsValid<T>(this Queue<T> queue) => queue != null && queue.Count > 0;

    #endregion

}
