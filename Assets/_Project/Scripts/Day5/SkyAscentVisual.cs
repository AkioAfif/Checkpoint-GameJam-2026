using System;
using UnityEngine;

// The climb leaves the world behind: while the MC is up in the air the sky's colour (camera background and fog) follows the
// altitude through bands (morning blue, deep blue, the white of the cloud layer, the dark stratosphere, black space); above
// the fog the map fades away, past the space line the fog goes and the stars and a huge curved Earth below show up. All of
// it is put back as the MC hits the ground. The Earth is a backdrop only: it hides again once the dive starts, so the MC
// never falls through it.
public class SkyAscentVisual : MonoBehaviour
{
    [Serializable]
    public struct SkyBand { public float altitude; public Color color; }

    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private Camera worldCamera;
    [Tooltip("By altitude above the impact point, lowest first.")]
    [SerializeField] private SkyBand[] skyBandArray;
    [Tooltip("Above this altitude there's no air: no fog, and the stars come out.")]
    [SerializeField] private float spaceAltitude = 380f;
    [Tooltip("The Earth shows from this altitude up: keep it above the Earth's top, so the MC is never inside it.")]
    [SerializeField] private float earthAltitude = 480f;
    [SerializeField] private GameObject starsRoot;
    [SerializeField] private GameObject earthRoot;

    private Color groundSky;
    private Color groundFog;
    private bool hasFog;
    private bool isFlying;

    private void Start()
    {
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
        starsRoot.SetActive(false);
        earthRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isFlying) return;
        float altitude = Player.Instance.transform.position.y - plantationSequence.GetImpactPosition().y;
        Color sky = GetSkyColor(altitude);
        worldCamera.backgroundColor = sky;
        RenderSettings.fogColor = sky;
        bool isInSpace = altitude >= spaceAltitude;
        RenderSettings.fog = hasFog && !isInSpace;
        starsRoot.SetActive(isInSpace);
        earthRoot.SetActive(altitude >= earthAltitude && plantationSequence.GetStep() != PlantationSequence.Step.Dive);
    }

    // scene lighting is global state: put it back for the next Play session (domain reload is off, the scene reloads)
    private void OnDestroy()
    {
        if (isFlying) Restore();
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        if (e.step == PlantationSequence.Step.Launch)
        {
            groundSky = worldCamera.backgroundColor;
            groundFog = RenderSettings.fogColor;
            hasFog = RenderSettings.fog;
            isFlying = true;
        }
        else if (e.step == PlantationSequence.Step.Impact && isFlying) Restore();
    }

    private void Restore()
    {
        isFlying = false;
        worldCamera.backgroundColor = groundSky;
        RenderSettings.fogColor = groundFog;
        RenderSettings.fog = hasFog;
        starsRoot.SetActive(false);
        earthRoot.SetActive(false);
    }

    private Color GetSkyColor(float altitude)
    {
        if (altitude <= skyBandArray[0].altitude) return groundSky;
        for (int i = 1; i < skyBandArray.Length; i++)
        {
            if (altitude > skyBandArray[i].altitude) continue;
            float t = Mathf.InverseLerp(skyBandArray[i - 1].altitude, skyBandArray[i].altitude, altitude);
            return Color.Lerp(skyBandArray[i - 1].color, skyBandArray[i].color, t);
        }
        return skyBandArray[skyBandArray.Length - 1].color;
    }
}
