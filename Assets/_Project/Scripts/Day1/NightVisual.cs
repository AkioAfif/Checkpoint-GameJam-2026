using UnityEngine;

// Dusk falls when the director reaches Night: sun, ambient light, fog and sky colour ease to their night values.
public class NightVisual : MonoBehaviour
{
    [SerializeField] private Day1Director director;
    [SerializeField] private Light sun;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameObject doorLamp;
    [SerializeField] private float duration = 2.5f;
    [SerializeField] private Color nightSunColor = new Color(.45f, .52f, .85f);
    [SerializeField] private float nightSunIntensity = .45f;
    [SerializeField] private Color nightAmbient = new Color(.22f, .26f, .42f);
    [SerializeField] private Color nightSky = new Color(.10f, .13f, .26f);

    private float timer = -1f;
    private bool hasNightBegun;
    private Color dayAmbientSky, dayAmbientEquator, dayAmbientGround, dayFog, daySunColor, daySky;
    private float daySunIntensity;

    private void Start()
    {
        director.OnStateChanged += Director_OnStateChanged;
        doorLamp.SetActive(false);
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / duration));
        sun.color = Color.Lerp(daySunColor, nightSunColor, t);
        sun.intensity = Mathf.Lerp(daySunIntensity, nightSunIntensity, t);
        RenderSettings.ambientSkyColor = Color.Lerp(dayAmbientSky, nightAmbient, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(dayAmbientEquator, nightAmbient * .9f, t);
        RenderSettings.ambientGroundColor = Color.Lerp(dayAmbientGround, nightAmbient * .6f, t);
        RenderSettings.fogColor = Color.Lerp(dayFog, nightSky, t);
        worldCamera.backgroundColor = Color.Lerp(daySky, nightSky, t);
        if (t >= 1f) timer = -1f;
    }

    // scene lighting is global state: put it back for the next Play session (domain reload is off, the scene reloads)
    private void OnDestroy()
    {
        if (!hasNightBegun) return;
        RenderSettings.ambientSkyColor = dayAmbientSky;
        RenderSettings.ambientEquatorColor = dayAmbientEquator;
        RenderSettings.ambientGroundColor = dayAmbientGround;
        RenderSettings.fogColor = dayFog;
    }

    private void Director_OnStateChanged(object sender, Day1Director.OnStateChangedEventArgs e)
    {
        if (e.state != Day1Director.State.Night) return;
        daySunColor = sun.color;
        daySunIntensity = sun.intensity;
        dayAmbientSky = RenderSettings.ambientSkyColor;
        dayAmbientEquator = RenderSettings.ambientEquatorColor;
        dayAmbientGround = RenderSettings.ambientGroundColor;
        dayFog = RenderSettings.fogColor;
        daySky = worldCamera.backgroundColor;
        doorLamp.SetActive(true);
        hasNightBegun = true;
        timer = 0f;
    }
}
