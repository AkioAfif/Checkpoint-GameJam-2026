using System;
using UnityEngine;

// The tube machine in front of the house: zoom in → solve the puzzle → seed pops out. Resources are only used on success.
public class FabricatorController : MonoBehaviour, IInteractable
{
    public static event EventHandler OnAnySeedFabricated;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Idle, Entering, Puzzle, Solved, Exiting }

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private ItemAmount[] recipeArray;
    [SerializeField] private ItemDataSO outputItemDataSO;
    [Tooltip("Given back after a successful run (the bottle the water came in).")]
    [SerializeField] private ItemAmount returnedItem;
    [SerializeField] private FabricatorPuzzleSO[] puzzleSOArray;
    [SerializeField] private float enterDuration = .9f;
    [SerializeField] private float solvedDuration = 1.6f;
    [SerializeField] private float exitDuration = .5f;
    [SerializeField] private string promptVerb = "use the Fabricator";
    [SerializeField] private string blockedReason = "Needs water, a flower and a crop";
    [SerializeField] private Transform promptAnchor;

    private State state;
    private float timer;
    private int completedRuns;
    private bool isSuccess;

    private void Start()
    {
        miniGame.OnSolved += MiniGame_OnSolved;
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnRestartAction += GameInput_OnRestartAction;
    }

    private void Update()
    {
        if (state == State.Idle || state == State.Puzzle) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case State.Entering:
                miniGame.Begin(puzzleSOArray[Mathf.Min(completedRuns, puzzleSOArray.Length - 1)]);
                SetState(State.Puzzle, 0f);
                break;
            case State.Solved:
                Leave(true);
                break;
            case State.Exiting:
                SetState(State.Idle, 0f);
                GameManager.Instance.SetState(GameManager.State.Playing);
                if (isSuccess) OnAnySeedFabricated?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnPauseAction -= GameInput_OnPauseAction;
        GameInput.Instance.OnRestartAction -= GameInput_OnRestartAction;
    }

    private void MiniGame_OnSolved(object sender, EventArgs e) => SetState(State.Solved, solvedDuration);

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        if (state == State.Puzzle) Leave(false);
    }

    private void GameInput_OnRestartAction(object sender, EventArgs e)
    {
        if (state == State.Puzzle) miniGame.Restart();
    }

    public bool IsSelectable() => state == State.Idle;
    public bool CanInteract(Player player) => Inventory.Instance.HasItems(recipeArray);

    public void Interact(Player player)
    {
        isSuccess = false;
        GameManager.Instance.SetState(GameManager.State.Fabricating);
        SetState(State.Entering, enterDuration);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    public ItemDataSO GetOutputItemDataSO() => outputItemDataSO;

    private void Leave(bool isSuccess)
    {
        this.isSuccess = isSuccess;
        if (isSuccess)
        {
            Inventory.Instance.RemoveItems(recipeArray);
            if (returnedItem.amount > 0) Inventory.Instance.AddItem(returnedItem.itemDataSO, returnedItem.amount);
            Inventory.Instance.AddItem(outputItemDataSO);
            completedRuns++;
        }
        miniGame.End();
        SetState(State.Exiting, exitDuration);
    }

    private void SetState(State state, float duration)
    {
        this.state = state;
        timer = duration;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData() => OnAnySeedFabricated = null;
}
