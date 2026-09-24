using System;
using Unity.Netcode;
using UnityEngine;

public class StoveCounter : BaseCounter, IHasProgress {
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs {
        public State state;
    }

    public enum State {
        Idle,
        Frying,
        Fried,
        Burned
    }

    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);
    private NetworkVariable<float> fryingTimer = new NetworkVariable<float>(0f);
    private FryingRecipeSO fryingRecipeSO;
    private NetworkVariable<float> burningTimer = new NetworkVariable<float>(0f);
    private BurningRecipeSO burningRecipeSO;

    public override void OnNetworkSpawn() {
        fryingTimer.OnValueChanged += FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged += BurningTimer_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    }

    private void FryingTimer_OnValueChanged(float previousValue, float newValue) {
        float fryingTimerMax = fryingRecipeSO != null ? fryingRecipeSO.fryingTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = fryingTimer.Value / fryingTimerMax
        });
    }

    private void BurningTimer_OnValueChanged(float previousValue, float newValue) {
        float burningTimerMax = burningRecipeSO != null ? burningRecipeSO.burningTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = burningTimer.Value / burningTimerMax
        });
    }

    private void State_OnValueChanged(State previousValue, State newValue) {
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
            state = newValue
        });

        if (newValue == State.Burned || newValue == State.Idle) {
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
                progressNormalized = 1f
            });
        }
    }

    private void Update() {
        if (!IsServer) {
            return;
        }

        if (HasKitchenObject()) {
            switch (state.Value) {
                case State.Idle:
                    break;
                case State.Frying:
                    fryingTimer.Value += Time.deltaTime;

                    if (fryingTimer.Value > fryingRecipeSO.fryingTimerMax) {
                        //Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);

                        state.Value = State.Fried;
                        burningTimer.Value = 0f;
                        SetBurningRecipeSORpc(GameMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO()));
                    }
                    break;
                case State.Fried:
                    burningTimer.Value += Time.deltaTime;

                    if (burningTimer.Value > burningRecipeSO.burningTimerMax) {
                        //Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);

                        state.Value = State.Burned;
                    }
                    break;
                case State.Burned:
                    break;
            }
        }
    }

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // There is no KitchenObject here
            if (player.HasKitchenObject()) {
                // Player is carring something
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    // Payer is carring something that can be fried
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);

                    InteractLogicPlaceObjectOnCounterRpc(GameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO()));
                }
            } else {
                // Player is not carring anything
            }
        } else {
            // There is a KitchenObject here
            if (player.HasKitchenObject()) {
                // Player is carring something
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        GetKitchenObject().DestroySelf();

                        state.Value = State.Idle;
                    }
                }
            } else {
                // Player not carring anything
                GetKitchenObject().SetKitchenObjectParent(player);
                SetStateIdleRpc();
            }
        }

    }

    [Rpc(SendTo.Server)]
    private void InteractLogicPlaceObjectOnCounterRpc(int kitchenObjectSOIndex) {
        fryingTimer.Value = 0f;
        state.Value = State.Frying;

        SetFryingRecipeSORpc(kitchenObjectSOIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetFryingRecipeSORpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = GameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetBurningRecipeSORpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = GameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
    }

    [Rpc(SendTo.Server)]
    private void SetStateIdleRpc() {
        state.Value = State.Idle;
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);

        return fryingRecipeSO != null;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray) {
            if (fryingRecipeSO.input == inputKitchenObjectSO) {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray) {
            if (burningRecipeSO.input == inputKitchenObjectSO) {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public bool IsFried() {
        return state.Value == State.Fried;
    }
}
