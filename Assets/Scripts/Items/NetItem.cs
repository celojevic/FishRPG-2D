using System;

[Serializable]
public class NetItem
{

    public Guid ItemBaseGuid;

    #region Constructors

    public NetItem() { }

    public NetItem(ItemBase item)
    {
        ItemBaseGuid = item.Guid;
    }

    #endregion

}
