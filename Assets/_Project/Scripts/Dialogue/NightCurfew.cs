using System;
using UnityEngine;

// The villagers' day/night switch: it is night once the day's director reaches Night (and stays night through Done).
// NpcWander listens and sends its villager home for the night. Each day's root has its own copy with only its own
// director set, like NightVisual; a scene without one never has a curfew.
public class NightCurfew : MonoBehaviour
{
    public static NightCurfew Instance { get; private set; }

    public event EventHandler OnNightChanged;

    [SerializeField] private Day1Director director;
    [SerializeField] private Day2Director day2Director;
    [SerializeField] private Day3Director day3Director;
    [SerializeField] private YardDayDirector yardDayDirector;

    private bool isNight;

    private void Awake()
    {
        if (Instance != null) Debug.LogError("More than one NightCurfew", this);
        Instance = this;
    }

    private void Start()
    {
        if (director != null) director.OnStateChanged += Director_OnStateChanged;
        if (day2Director != null) day2Director.OnStateChanged += Day2Director_OnStateChanged;
        if (day3Director != null) day3Director.OnStateChanged += Day3Director_OnStateChanged;
        if (yardDayDirector != null) yardDayDirector.OnStateChanged += YardDayDirector_OnStateChanged;
        // a director that started first may already be at night
        SetNight(IsDirectorAtNight());
    }

    private void Director_OnStateChanged(object sender, Day1Director.OnStateChangedEventArgs e) =>
        SetNight(e.state == Day1Director.State.Night || e.state == Day1Director.State.Done);

    private void Day2Director_OnStateChanged(object sender, Day2Director.OnStateChangedEventArgs e) =>
        SetNight(e.state == Day2Director.State.Night || e.state == Day2Director.State.Done);

    private void Day3Director_OnStateChanged(object sender, Day3Director.OnStateChangedEventArgs e) =>
        SetNight(e.state == Day3Director.State.Night || e.state == Day3Director.State.Done);

    private void YardDayDirector_OnStateChanged(object sender, YardDayDirector.OnStateChangedEventArgs e) =>
        SetNight(e.state == YardDayDirector.State.Night || e.state == YardDayDirector.State.Done);

    public bool IsNight() => isNight;

    private bool IsDirectorAtNight()
    {
        if (director != null) return director.GetState() == Day1Director.State.Night || director.GetState() == Day1Director.State.Done;
        if (day2Director != null) return day2Director.GetState() == Day2Director.State.Night || day2Director.GetState() == Day2Director.State.Done;
        if (day3Director != null) return day3Director.GetState() == Day3Director.State.Night || day3Director.GetState() == Day3Director.State.Done;
        if (yardDayDirector != null) return yardDayDirector.GetState() == YardDayDirector.State.Night || yardDayDirector.GetState() == YardDayDirector.State.Done;
        return false;
    }

    private void SetNight(bool isNight)
    {
        if (this.isNight == isNight) return;
        this.isNight = isNight;
        OnNightChanged?.Invoke(this, EventArgs.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
