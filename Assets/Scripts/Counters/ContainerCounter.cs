using System;
using Unity.Netcode;
using UnityEngine;

public class ContainerCounter : BaseCounter {
    public event EventHandler OnPlayerGrabbedObject;

    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is not carring anything
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);

            TriggerOnPlayerGrabbedObjectRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlayerGrabbedObjectRpc() {
        OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
    }
}
