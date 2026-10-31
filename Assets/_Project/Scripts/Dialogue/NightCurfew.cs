using System;
using System.Collections.Generic;
using UnityEngine;

// The villagers' day/night switch: it is night once the day's director reaches Night (and stays night through Done).
// Every director turns to Night behind the "That evening..." fade, so at that moment every villager outdoors goes indoors
// (switched off, whatever their schedule or wander), and comes back out when it is day again. Each day's root has its own
// copy with only its own director set, like NightVisual; a scene without one never has a curfew.
public class NightCurfew : MonoBehaviour
{
    public static NightCurfew Instance { get; private set; }

    public event EventHandler OnNightChanged;

    [SerializeField] private Day1Director director;
    [SerializeField] private Day2Director day2Director;
    [SerializeField] private Day3Director day3Director;
    [SerializeField] private YardDayDirector yardDayDirector;
    [SerializeField] private Day5Director day5Director;
    [SerializeField] private Day6Director day6Director;

    private bool isNight;
    // the villagers sent indoors tonight, to bring back out in the morning
    private readonly List<Npc> indoorNpcList = new List<Npc>();

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
        if (day5Director != null) day5Director.OnStateChanged += Day5Director_OnStateChanged;
        if (day6Director != null) day6Director.OnStateChanged += Day6Director_OnStateChanged;
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

    private void Day5Director_OnStateChanged(object sender, Day5Director.OnStateChangedEventArgs e) =>
        SetNight(e.state == Day5Director.State.Night || e.state == Day5Director.State.Done);

    private void Day6Director_OnStateChanged(object sender, Day6Director.OnStateChangedEventArgs e) =>
        SetNight(e.state == Day6Director.State.Night || e.state == Day6Director.State.Done);

    public bool IsNight() => isNight;

    private bool IsDirectorAtNight()
    {
        if (director != null) return director.GetState() == Day1Director.State.Night || director.GetState() == Day1Director.State.Done;
        if (day2Director != null) return day2Director.GetState() == Day2Director.State.Night || day2Director.GetState() == Day2Director.State.Done;
        if (day3Director != null) return day3Director.GetState() == Day3Director.State.Night || day3Director.GetState() == Day3Director.State.Done;
        if (yardDayDirector != null) return yardDayDirector.GetState() == YardDayDirector.State.Night || yardDayDirector.GetState() == YardDayDirector.State.Done;
        if (day5Director != null) return day5Director.GetState() == Day5Director.State.Night || day5Director.GetState() == Day5Director.State.Done;
        if (day6Director != null) return day6Director.GetState() == Day6Director.State.Night || day6Director.GetState() == Day6Director.State.Done;
        return false;
    }

    private void SetNight(bool isNight)
    {
        if (this.isNight == isNight) return;
        this.isNight = isNight;
        if (isNight) SendVillagersIndoors();
        else BringVillagersOut();
        OnNightChanged?.Invoke(this, EventArgs.Empty);
    }

    // villagers a director already switched off (Sari, Rigel) aren't outdoors and stay as the story left them
    private void SendVillagersIndoors()
    {
        foreach (Npc npc in FindObjectsByType<Npc>())
        {
            indoorNpcList.Add(npc);
            npc.gameObject.SetActive(false);
        }
    }

    private void BringVillagersOut()
    {
        foreach (Npc npc in indoorNpcList)
            if (npc != null) npc.gameObject.SetActive(true);
        indoorNpcList.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData() => Instance = null;
}
