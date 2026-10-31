using UnityEngine;

// The MC's childhood TV show, remembered through the glitch: in a little studio far under the map (filmed into a
// RenderTexture that ChildhoodShowUI shows on an old TV), a cartoon superhero drops out of the sky fist first, lands in a
// crouch with a crater and a burst of dust, and flowers bloom outward from the crater; then he stands, arms raised.
// Played over the plantation sequence's Hallucination step.
public class ChildhoodShowVisual : MonoBehaviour
{
    [SerializeField] private PlantationSequence plantationSequence;
    [SerializeField] private Camera showCamera;
    [SerializeField] private Transform hero;
    [SerializeField] private Transform heroArmL;
    [SerializeField] private Transform heroArmR;
    [SerializeField] private GameObject crater;
    [Tooltip("The flowers round the crater, nearest first: they bloom outward in this order.")]
    [SerializeField] private Transform[] flowerArray;
    [SerializeField] private Transform dustPrefab;
    [SerializeField] private float dropHeight = 9f;
    [SerializeField] private float fallStartTime = .4f;
    [SerializeField] private float landTime = 1.3f;
    [SerializeField] private float bloomStartTime = 1.5f;
    [SerializeField] private float bloomEndTime = 3.2f;
    [SerializeField] private float standTime = 3.5f;
    [SerializeField] private float flowerPopDuration = .35f;
    [SerializeField] private float shakeAmount = .25f;
    [SerializeField] private float shakeDuration = .5f;

    private Vector3 heroRestPosition;
    private Vector3[] flowerScaleArray;
    private Vector3 cameraRestPosition;
    private float timer = -1f;
    private bool hasLanded;

    private void Start()
    {
        plantationSequence.OnStepChanged += PlantationSequence_OnStepChanged;
        heroRestPosition = hero.localPosition;
        cameraRestPosition = showCamera.transform.localPosition;
        flowerScaleArray = new Vector3[flowerArray.Length];
        for (int i = 0; i < flowerArray.Length; i++) flowerScaleArray[i] = flowerArray[i].localScale;
        showCamera.enabled = false;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;

        if (timer < landTime)
        {
            // fist first out of the sky, speeding up
            float fall = Mathf.Clamp01((timer - fallStartTime) / (landTime - fallStartTime));
            hero.localPosition = heroRestPosition + Vector3.up * dropHeight * (1f - fall * fall);
            hero.localRotation = Quaternion.Euler(180f, 0f, 0f);
            SetArms(180f, 180f);
        }
        else
        {
            if (!hasLanded) Land();
            bool isStanding = timer >= standTime;
            hero.localPosition = heroRestPosition;
            hero.localRotation = Quaternion.identity;
            // crouched over the fist in the crater, then up with both arms raised
            hero.localScale = new Vector3(1f, isStanding ? 1f : .8f, 1f);
            if (isStanding) SetArms(170f, 170f);
            else SetArms(30f, 150f);
        }

        for (int i = 0; i < flowerArray.Length; i++)
        {
            float bloomTime = Mathf.Lerp(bloomStartTime, bloomEndTime, (float)i / Mathf.Max(1, flowerArray.Length - 1));
            float pop = Mathf.Clamp01((timer - bloomTime) / flowerPopDuration);
            flowerArray[i].localScale = flowerScaleArray[i] * (pop + Mathf.Sin(pop * Mathf.PI) * .3f);
        }

        float shake = Mathf.Clamp01(1f - (timer - landTime) / shakeDuration);
        showCamera.transform.localPosition = cameraRestPosition + (hasLanded ? Random.insideUnitSphere * shakeAmount * shake : Vector3.zero);
    }

    private void PlantationSequence_OnStepChanged(object sender, PlantationSequence.OnStepChangedEventArgs e)
    {
        bool isPlaying = e.step == PlantationSequence.Step.Hallucination;
        showCamera.enabled = isPlaying;
        if (!isPlaying)
        {
            timer = -1f;
            return;
        }
        timer = 0f;
        hasLanded = false;
        crater.SetActive(false);
        hero.localScale = Vector3.one;
        foreach (Transform flower in flowerArray) flower.localScale = Vector3.zero;
    }

    private void Land()
    {
        hasLanded = true;
        crater.SetActive(true);
        Instantiate(dustPrefab, hero.position, Quaternion.identity).localScale = Vector3.one * 1.6f;
    }

    // the arms hang from the shoulders: 0 straight down, 180 straight up past the head
    private void SetArms(float leftAngle, float rightAngle)
    {
        heroArmL.localRotation = Quaternion.Euler(-leftAngle, 0f, 0f);
        heroArmR.localRotation = Quaternion.Euler(-rightAngle, 0f, 0f);
    }
}
