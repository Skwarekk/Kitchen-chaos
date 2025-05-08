using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO KitchenObjectSO;

    public KitchenObjectSO GetKitchenObjectSO()
    {
        return KitchenObjectSO;
    }
}
