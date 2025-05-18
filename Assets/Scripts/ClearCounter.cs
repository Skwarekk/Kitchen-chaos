using UnityEngine;

public class ClearCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public override void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            // There is no KitchenObject here
            if (player.HasKitchenObject())
            {
                // Player is carring something
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
            else
            {
                // Player is not carring anything
            }
        }
        else
        {
            // There is a KitchenObject here
            if(player.HasKitchenObject())
            {
                // Player is carring something
            }
            else
            {
                // Player not carring anything
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }
}
