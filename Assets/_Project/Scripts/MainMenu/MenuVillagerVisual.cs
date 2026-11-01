using UnityEngine;

// Plays the menu villager's walk and emotes at time-lapse speed.
public class MenuVillagerVisual : MonoBehaviour
{
    [SerializeField] private MenuVillager menuVillager;
    [SerializeField] private Animator animator;

    private void Start()
    {
        animator.speed = MenuTimeLapse.Instance.GetTimeScale();
        menuVillager.OnActivityStarted += MenuVillager_OnActivityStarted;
    }

    private void Update()
    {
        animator.SetFloat(ChibiAnimation.SPEED, menuVillager.GetSpeedNormalized());
    }

    private void MenuVillager_OnActivityStarted(object sender, MenuVillager.OnActivityEventArgs e) => ChibiAnimation.PlayEmote(animator, e.emote);
}
