using UnityEngine;

public class ResourceNode : MonoBehaviour
{

    public ResourceData Data;

    [SerializeField] private SpriteRenderer _sr = null;


    #region Editor
#if UNITY_EDITOR

    private void OnValidate()
    {
        if (_sr != null && Data != null)
            _sr.sprite = Data.FullSprite;
    }

#endif
    #endregion

}
