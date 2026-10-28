using System;
using System.Collections.Generic;
using UnityEngine;

// The tube machine in front of the house: pick a recipe and how many batches → zoom in → solve the puzzle once → seeds pop out.
// Resources are only used on success, one recipe's worth per batch. The recipe panel always opens; Make needs the materials.
// The panel offers today's recipe first, then every other seed's recipe made on an earlier press (WorldState).
// A guided recipe (the tutorial's first press) skips the panel: one batch, straight into the puzzle, only with the materials.
public class FabricatorController : MonoBehaviour, IInteractable
{
    public static event EventHandler<OnAnySeedFabricatedEventArgs> OnAnySeedFabricated;
    public class OnAnySeedFabricatedEventArgs : EventArgs { public int seedCount; public ItemDataSO seedItemDataSO; }

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs { public State state; }
    // the chosen recipe or batch count changed
    public event EventHandler OnChoiceChanged;

    public enum State { Idle, Choosing, Entering, Puzzle, Solved, Exiting }

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
        [Tooltip("Tutorial press: no recipe panel, always one batch, and [Interact] is blocked (showing blockedReason) until the materials are there.")]
        public bool isGuided;
    }

    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private Recipe[] recipeArray;
    [SerializeField] private float enterDuration = .9f;
    [SerializeField] private float solvedDuration = 1.6f;
    [SerializeField] private float exitDuration = .5f;
    [Tooltip("Extra seeds for a combo (all liquids into the mould at once).")]
    [SerializeField] private int comboBonus = 1;
    [Tooltip("Most batches one run can make, even with a full inventory.")]
    [SerializeField] private int maxBatchCount = 9;
    [SerializeField] private string promptVerb = "use the Fabricator";
    [SerializeField] private Transform promptAnchor;

    private State state;
    private float timer;
    private int completedRuns;
    private bool isSuccess;
    // what the last successful run put in the bag (batches × output + combo bonus), announced once the machine is idle again
    private int madeSeedCount;
    private bool isPlaced = true;
    private int batchCount = 1;
    // today's recipe, then the unlocked recipes for other seeds
    private readonly List<Recipe> choiceList = new List<Recipe>();
    private int choiceIndex;
    // the Interact press that opened the recipe panel must not also confirm it
    private int choosingStartFrame;
    // A/D change the recipe and W/S the batch count once per press, not every frame they are held
    private int heldBatchStep;
    private int heldRecipeStep;

    private void Start()
    {
        miniGame.OnSolved += MiniGame_OnSolved;
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnRestartAction += GameInput_OnRestartAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        if (state == State.Choosing)
        {
            Vector2 movement = GameInput.Instance.GetMovementVectorNormalized();
            int recipeStep = movement.x > .5f ? 1 : movement.x < -.5f ? -1 : 0;
            if (recipeStep != 0 && recipeStep != heldRecipeStep) ChangeRecipe(recipeStep);
            heldRecipeStep = recipeStep;
            int step = movement.y > .5f ? 1 : movement.y < -.5f ? -1 : 0;
            if (step != 0 && step != heldBatchStep) ChangeBatchCount(step);
            heldBatchStep = step;
            return;
        }
        if (state == State.Idle || state == State.Puzzle) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case State.Entering:
                miniGame.Begin(GetChosenRecipe().puzzleSO);
                SetState(State.Puzzle, 0f);
                break;
            case State.Solved:
                Leave(true);
                break;
            case State.Exiting:
                SetState(State.Idle, 0f);
                GameManager.Instance.SetState(GameManager.State.Playing);
                if (isSuccess) OnAnySeedFabricated?.Invoke(this, new OnAnySeedFabricatedEventArgs { seedCount = madeSeedCount, seedItemDataSO = GetOutputItemDataSO() });
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance == null) return;
        GameInput.Instance.OnPauseAction -= GameInput_OnPauseAction;
        GameInput.Instance.OnRestartAction -= GameInput_OnRestartAction;
        GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    private void MiniGame_OnSolved(object sender, EventArgs e) => SetState(State.Solved, solvedDuration);

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        if (state == State.Choosing) CancelBatch();
        else if (state == State.Puzzle) Leave(false);
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state == State.Choosing && Time.frameCount != choosingStartFrame) ConfirmBatch();
    }

    private void GameInput_OnRestartAction(object sender, EventArgs e)
    {
        if (state == State.Puzzle) miniGame.Restart();
    }

    public bool IsSelectable() => isPlaced && state == State.Idle;
    // the panel opens with any bag (it shows what is missing); only a guided press waits for its materials
    public bool CanInteract(Player player) => !GetRecipe().isGuided || Inventory.Instance.HasItems(GetRecipe().inputArray);

    public void Interact(Player player)
    {
        isSuccess = false;
        batchCount = 1;
        heldBatchStep = 0;
        heldRecipeStep = 0;
        choosingStartFrame = Time.frameCount;
        FillChoiceList();
        GameManager.Instance.SetState(GameManager.State.Fabricating);
        if (GetRecipe().isGuided)
        {
            SetState(State.Entering, enterDuration);
            return;
        }
        SetState(State.Choosing, 0f);
        OnChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    // wraps around; the batch count shrinks if the bag can't cover it for the new recipe
    public void ChangeRecipe(int delta)
    {
        if (choiceList.Count < 2) return;
        choiceIndex = (choiceIndex + delta + choiceList.Count) % choiceList.Count;
        batchCount = Mathf.Clamp(batchCount, 1, GetMaxBatchCount());
        OnChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ChangeBatchCount(int delta)
    {
        int newCount = Mathf.Clamp(batchCount + delta, 1, GetMaxBatchCount());
        if (newCount == batchCount) return;
        batchCount = newCount;
        OnChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    // Make: refused while the bag can't cover the chosen batches (Leave would otherwise pay out with inputs missing)
    public void ConfirmBatch()
    {
        if (state != State.Choosing || !CanAffordBatches()) return;
        SetState(State.Entering, enterDuration);
    }

    public void CancelBatch()
    {
        if (state != State.Choosing) return;
        SetState(State.Exiting, 0f);
    }

    // the bag covers every input for the chosen number of batches
    public bool CanAffordBatches()
    {
        foreach (ItemAmount input in GetChosenRecipe().inputArray)
            if (Inventory.Instance.GetCount(input.itemDataSO) < input.amount * batchCount) return false;
        return true;
    }

    public int GetBatchCount() => batchCount;
    // as many batches as the inventory covers for every input, capped at maxBatchCount
    public int GetMaxBatchCount()
    {
        int affordable = maxBatchCount;
        foreach (ItemAmount input in GetChosenRecipe().inputArray)
            if (input.amount > 0) affordable = Mathf.Min(affordable, Inventory.Instance.GetCount(input.itemDataSO) / input.amount);
        return Mathf.Max(1, affordable);
    }
    public ItemAmount[] GetInputArray() => GetChosenRecipe().inputArray;
    public ItemAmount GetOutput() => GetChosenRecipe().output;
    public int GetChoiceCount() => choiceList.Count;
    public int GetChoiceIndex() => choiceIndex;

    public string GetPromptVerb() => promptVerb;
    public string GetBlockedReason() => GetRecipe().blockedReason;
    public Transform GetPromptAnchor() => promptAnchor;
    // the seed type the chosen recipe makes (its prefab pops out of the tube)
    public ItemDataSO GetOutputItemDataSO() => GetChosenRecipe().output.itemDataSO;
    // false while the Distributor still carries it
    public void SetPlaced(bool isPlaced) => this.isPlaced = isPlaced;

    // a later day brings its own recipes; the press count starts over
    public void SetRecipeArray(Recipe[] recipeArray)
    {
        this.recipeArray = recipeArray;
        completedRuns = 0;
    }

    // today's recipe for the next press
    private Recipe GetRecipe() => recipeArray[Mathf.Min(completedRuns, recipeArray.Length - 1)];
    // a guided press never fills the list, so it runs today's recipe
    private Recipe GetChosenRecipe() => choiceIndex < choiceList.Count ? choiceList[choiceIndex] : GetRecipe();

    private void FillChoiceList()
    {
        choiceList.Clear();
        choiceIndex = 0;
        Recipe todayRecipe = GetRecipe();
        choiceList.Add(todayRecipe);
        if (todayRecipe.isGuided) return;
        foreach (Recipe unlocked in WorldState.GetUnlockedRecipeList())
            if (unlocked.output.itemDataSO != todayRecipe.output.itemDataSO) choiceList.Add(unlocked);
    }

    private void Leave(bool isSuccess)
    {
        this.isSuccess = isSuccess;
        if (isSuccess)
        {
            Recipe recipe = GetChosenRecipe();
            for (int i = 0; i < batchCount; i++) Inventory.Instance.RemoveItems(recipe.inputArray);
            if (recipe.returned.amount > 0) Inventory.Instance.AddItem(recipe.returned.itemDataSO, recipe.returned.amount * batchCount);
            // the combo is one reward per run, however many batches it made
            madeSeedCount = recipe.output.amount * batchCount + (miniGame.IsCombo() ? comboBonus : 0);
            Inventory.Instance.AddItem(recipe.output.itemDataSO, madeSeedCount);
            if (!recipe.isGuided) WorldState.UnlockRecipe(recipe);
            // only today's recipe moves the day's press order on (Day 1's puzzles get harder)
            if (recipe == GetRecipe()) completedRuns++;
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
