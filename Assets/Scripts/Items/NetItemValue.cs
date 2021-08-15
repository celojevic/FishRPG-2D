
[System.Serializable]
public class NetItemValue
{

    public NetItem Item;
    public int Quantity;

    #region Constructors

    public NetItemValue() { }

    public NetItemValue(ItemBase item, int quantity = 1)
    {
        this.Item = new NetItem(item);
        this.Quantity = quantity;
    }

    public NetItemValue(ItemValue itemValue)
    {
        this.Item = new NetItem(itemValue.Item);
        this.Quantity = itemValue.Quantity;
    }

    #endregion

    public ItemValue ToItemValue()
    {
        return new ItemValue
        {
            Item = Database.Instance?.GetItemBase(Item.ItemBaseGuid),
            Quantity = this.Quantity
        };
    }

}