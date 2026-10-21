using System;
using TMPro;
using UnityEngine;

// "+1.500.000 Koin" pops up at the top of the screen whenever the ICAN crate pays out.
[RequireComponent(typeof(CanvasGroup))]
public class MoneyPopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private UIPop amountPop;
    [SerializeField] private string amountFormat = "+{0} Koin";
    [SerializeField] private float showDuration = 2.2f;

    private CanvasGroup canvasGroup;
    private float timer = -1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.SetVisible(false);
    }

    private void Start()
    {
        SellCrate.OnAnySold += SellCrate_OnAnySold;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer -= Time.deltaTime;
        if (timer < 0f) canvasGroup.SetVisible(false);
    }

    private void OnDestroy()
    {
        SellCrate.OnAnySold -= SellCrate_OnAnySold;
    }

    private void SellCrate_OnAnySold(object sender, SellCrate.OnAnySoldEventArgs e)
    {
        amountText.text = string.Format(amountFormat, Koin.Format(e.amount));
        canvasGroup.SetVisible(true);
        amountPop.Play(0f);
        timer = showDuration;
    }
}
