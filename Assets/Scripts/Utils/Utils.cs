using UnityEngine;
using UnityEngine.EventSystems;

public static class Utils
{

    public static bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();

    public static Vector3 GetWorldMousePos()
    {
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        return mousePos;
    }



}
