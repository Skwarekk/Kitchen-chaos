using Unity.Netcode;
using UnityEngine;

public class GameMultiplayer : NetworkBehaviour {
    public static GameMultiplayer Instance { get; private set; }

    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

    private void Awake() {
        if(Instance != null) {
            Debug.LogError("There is more than one Instance of GameMultiplayer");
        }

        Instance = this;
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {
        SpawnKitchenObjectRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    [Rpc(SendTo.Server)]
    private void SpawnKitchenObjectRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObjectTransform.GetComponent<NetworkObject>().Spawn(true);

        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    public void DestroyKitchenObject(KitchenObject kitchenObject) {
        DestroyKitchenObjectRpc(kitchenObject.NetworkObject);
    }

    [Rpc(SendTo.Server)]
    private void DestroyKitchenObjectRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        ClearKitchenObjectOnParentRpc(kitchenObjectNetworkObjectReference);

        kitchenObject.DestroySelf();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClearKitchenObjectOnParentRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }
}
