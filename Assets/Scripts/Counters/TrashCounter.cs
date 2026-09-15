using System;
using Unity.Netcode;

public class TrashCounter : BaseCounter
{
    public static event EventHandler OnAnyObjectTrashed;

    new public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            TriggerOnAnyObjectTrashedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnAnyObjectTrashedRpc() {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}
