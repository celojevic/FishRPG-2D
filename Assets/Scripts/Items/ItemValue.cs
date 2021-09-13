
[System.Serializable]
public class ItemValue
{

    public ItemBase Item;

    public int Quantity;

    #region Constructors

    public ItemValue() { }

    public ItemValue(ItemBase item, int quantity)
    {
        this.Item = item;
        this.Quantity = quantity;
    }

    public ItemValue(ItemReward reward)
    {
        this.Item = reward.Item;
        this.Quantity = reward.Quantity;
    }

    #endregion

}