using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "New item!" card the first time an item type is collected. Freezes the world until dismissed.
[RequireComponent(typeof(CanvasGroup))]
public class DiscoveryPopupUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private UIPop cardPop;
    [SerializeField] private Transform iconSpin;
    [SerializeField] private string continueFormat = "Press [{0}] to continue";

    private CanvasGroup canvasGroup;
    private readonly Queue<ItemDataSO> pendingQueue = new Queue<ItemDataSO>();
    private bool isShowing;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        continueButton.onClick.AddListener(Close);
        Hide();
    }

    private void Start()
    {
        Inventory.Instance.OnItemDiscovered += Inventory_OnItemDiscovered;
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    private void Update()
    {
        if (isShowing) iconSpin.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.5f) * 8f);
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnItemDiscovered -= Inventory_OnItemDiscovered;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
        if (GameInput.Instance != null) GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    // discoveries can happen mid-dialogue or inside the Fabricator: wait until the player is free again
    private void Inventory_OnItemDiscovered(object sender, Inventory.OnItemDiscoveredEventArgs e)
    {
        pendingQueue.Enqueue(e.itemDataSO);
        TryShowNext();
    }

    private void GameManager_OnStateChanged(object sender, GameManager.OnStateChangedEventArgs e) => TryShowNext();

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (isShowing && !GameManager.Instance.IsStateChangedThisFrame()) Close();
    }

    private void TryShowNext()
    {
        if (isShowing || pendingQueue.Count == 0 || GameManager.Instance.GetState() != GameManager.State.Playing) return;
        ItemDataSO itemDataSO = pendingQueue.Dequeue();
        iconImage.sprite = itemDataSO.icon;
        nameText.text = itemDataSO.itemName;
        descriptionText.text = itemDataSO.description;
        continueText.text = string.Format(continueFormat, GameInput.Instance.GetInteractBindingDisplay());
        isShowing = true;
        GameManager.Instance.SetState(GameManager.State.Popup);
        canvasGroup.SetVisible(true);
        cardPop.Play(0f);
        continueButton.Select();
    }

    private void Close()
    {
        if (!isShowing) return;
        isShowing = false;
        Hide();
        GameManager.Instance.SetState(GameManager.State.Playing); // re-enters TryShowNext for queued items
    }

    private void Hide() => canvasGroup.SetVisible(false);
}
