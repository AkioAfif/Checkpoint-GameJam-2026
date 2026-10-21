using System;
using UnityEngine;

// The tube machine in front of the house: zoom in → solve the puzzle → seeds pop out. Resources are only used on success.
public class FabricatorController : MonoBehaviour, IInteractable
{
    public static event EventHandler OnAnySeedFabricated;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }

    public enum State { Idle, Entering, Puzzle, Solved, Exiting }

    // one press: what goes in, what comes out, and the puzzle that gates it. The last entry repeats.
    [Serializable]
    public class Recipe
    {
        public ItemAmount[] inputArray;
        public ItemAmount output;
        // given back after a successful run (the bottle the water came in)
        public ItemAmount returned;
        public FabricatorPuzzleSO puzzleSO;
        public string blockedReason;
    }

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private Recipe[] recipeArray;
    [SerializeField] private float enterDuration = .9f;
    [SerializeField] private float solvedDuration = 1.6f;
    [SerializeField] private float exitDuration = .5f;
    [Tooltip("Extra seeds for a combo (all liquids into the mould at once).")]
    [SerializeField] private int comboBonus = 1;
    [SerializeField] private string promptVerb = "use the Fabricator";
    [SerializeField] private Transform promptAnchor;

    private State state;
    private float timer;
    private int completedRuns;
    private bool isSuccess;
    private bool isPlaced = true;

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
                miniGame.Begin(GetRecipe().puzzleSO);
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

    public bool IsSelectable() => isPlaced && state == State.Idle;
    public bool CanInteract(Player player) => Inventory.Instance.HasItems(GetRecipe().inputArray);

    public void Interact(Player player)
    {
        isSuccess = false;
        GameManager.Instance.SetState(GameManager.State.Fabricating);
        SetState(State.Entering, enterDuration);
    }

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => GetRecipe().blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    // the seed type the next press makes (its prefab pops out of the tube)
    public ItemDataSO GetOutputItemDataSO() => GetRecipe().output.itemDataSO;
    // false while the Distributor still carries it
    public void SetPlaced(bool isPlaced) => this.isPlaced = isPlaced;

    // a later day brings its own recipes; the press count starts over
    public void SetRecipeArray(Recipe[] recipeArray)
    {
        this.recipeArray = recipeArray;
        completedRuns = 0;
    }

    private Recipe GetRecipe() => recipeArray[Mathf.Min(completedRuns, recipeArray.Length - 1)];

    private void Leave(bool isSuccess)
    {
        this.isSuccess = isSuccess;
        if (isSuccess)
        {
            Recipe recipe = GetRecipe();
            Inventory.Instance.RemoveItems(recipe.inputArray);
            if (recipe.returned.amount > 0) Inventory.Instance.AddItem(recipe.returned.itemDataSO, recipe.returned.amount);
            Inventory.Instance.AddItem(recipe.output.itemDataSO, recipe.output.amount + (miniGame.IsCombo() ? comboBonus : 0));
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
