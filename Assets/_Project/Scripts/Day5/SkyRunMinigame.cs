using System;
using System.Collections.Generic;
using UnityEngine;

// The climb's minigame: while PlantationSequence flies the MC up, the player steers them left and right (Move x) across a
// few lanes. Each row up the climb holds floating sawit bunches to catch or birds to dodge; a catch is a bunch for the bag
// (given when the climb ends), a bird knocks one loose again.
public class SkyRunMinigame : MonoBehaviour
{
    public event EventHandler OnStarted;
    public event EventHandler OnEnded;
    public event EventHandler<OnCountChangedEventArgs> OnCountChanged;
    public class OnCountChangedEventArgs : EventArgs { public int count; public bool isHit; }

    [SerializeField] private ItemDataSO rewardItemDataSO;
    [Tooltip("A model of the floating bunch (no logic).")]
    [SerializeField] private Transform pickupPrefab;
    [SerializeField] private Transform birdPrefab;
    [Tooltip("The bunch model is item-sized: bigger, so it reads against the sky.")]
    [SerializeField] private float pickupScale = 2.2f;
    [Tooltip("Screen-right across the climb, on the ground plane (set from the launch camera).")]
    [SerializeField] private Vector3 steerAxis = Vector3.left;
    [SerializeField] private float steerSpeed = 7f;
    [Tooltip("How far (m) the MC can steer to either side; also the outer lanes.")]
    [SerializeField] private float laneWidth = 3.2f;
    [SerializeField] private float catchRadius = 1.4f;
    [Tooltip("How far (m) a bird sweeps across its lane and how fast.")]
    [SerializeField] private float birdSweep = 1.6f;
    [SerializeField] private float birdSweepSpeed = 1.7f;

    private class Hazard { public Transform transform; public Vector3 centre; public float phase; }

    private readonly List<Transform> pickupList = new List<Transform>();
    private readonly List<Hazard> birdList = new List<Hazard>();
    private bool isActive;
    private float offset;
    private int count;

    private void Update()
    {
        if (!isActive) return;
        offset = Mathf.Clamp(offset + GameInput.Instance.GetMovementVectorNormalized().x * steerSpeed * Time.deltaTime, -laneWidth, laneWidth);

        Vector3 player = Player.Instance.transform.position;
        for (int i = pickupList.Count - 1; i >= 0; i--)
        {
            if ((pickupList[i].position - player).sqrMagnitude > catchRadius * catchRadius) continue;
            Destroy(pickupList[i].gameObject);
            pickupList.RemoveAt(i);
            SetCount(count + 1, false);
        }
        for (int i = birdList.Count - 1; i >= 0; i--)
        {
            Hazard bird = birdList[i];
            bird.transform.position = bird.centre + steerAxis * Mathf.Sin(Time.time * birdSweepSpeed + bird.phase) * birdSweep;
            if ((bird.transform.position - player).sqrMagnitude > catchRadius * catchRadius) continue;
            // it flaps off: one bird, one bunch at most
            Destroy(bird.transform.gameObject);
            birdList.RemoveAt(i);
            SetCount(Mathf.Max(0, count - 1), true);
        }
    }

    // a row per point up the climb: every third one is a bird row (one bird, a bunch in another lane), the rest are bunches
    public void Begin(Vector3[] rowArray)
    {
        offset = 0f;
        count = 0;
        for (int row = 0; row < rowArray.Length; row++)
        {
            int lane = (row * 2 + 1) % 3 - 1;
            if (row % 3 == 2)
            {
                Vector3 centre = rowArray[row] + steerAxis * lane * laneWidth;
                birdList.Add(new Hazard { transform = Instantiate(birdPrefab, centre, Quaternion.LookRotation(steerAxis)), centre = centre, phase = row });
                lane = lane == 0 ? 1 : -lane;
            }
            Transform pickup = Instantiate(pickupPrefab, rowArray[row] + steerAxis * lane * laneWidth, Quaternion.identity);
            pickup.localScale *= pickupScale;
            pickupList.Add(pickup);
        }
        isActive = true;
        OnStarted?.Invoke(this, EventArgs.Empty);
        SetCount(0, false);
    }

    // the catches go in the bag; whatever is left in the sky goes away
    public void End()
    {
        isActive = false;
        foreach (Transform pickup in pickupList) Destroy(pickup.gameObject);
        foreach (Hazard bird in birdList) Destroy(bird.transform.gameObject);
        pickupList.Clear();
        birdList.Clear();
        if (count > 0) Inventory.Instance.AddItem(rewardItemDataSO, count);
        OnEnded?.Invoke(this, EventArgs.Empty);
    }

    public Vector3 GetSteerOffset() => steerAxis * offset;
    public int GetCount() => count;

    private void SetCount(int count, bool isHit)
    {
        this.count = count;
        OnCountChanged?.Invoke(this, new OnCountChangedEventArgs { count = count, isHit = isHit });
    }
}
