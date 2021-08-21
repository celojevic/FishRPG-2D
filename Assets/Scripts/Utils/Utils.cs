using UnityEngine;

public static class Utils
{


    public static Vector3 GetWorldMousePos()
    {
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        return mousePos;
    }


}
