using UnityEngine;
using System.Collections.Generic;

public class BoosterUI : MonoBehaviour
{
    [Header("References")]
    public BoosterManager boosterManager;
    public BoosterSlotUI[] slots;

    [Header("Layout")]
    public float slotHeight = 110f;
    public float slotSpacing = 10f;
    public float reorderSpeed = 8f;
    public float panelHeightPadding = 16f;

    private readonly List<BoosterSlotUI> layoutSlots = new List<BoosterSlotUI>();
    private readonly List<BoosterSlotUI> visibleSlots = new List<BoosterSlotUI>();
    private readonly Dictionary<BoosterSlotUI, float> layoutTargets = new Dictionary<BoosterSlotUI, float>();

    private RectTransform panelRect;

    void Awake()
    {
        if (boosterManager == null) boosterManager = FindObjectOfType<BoosterManager>();

        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<BoosterSlotUI>(true);
        }

        panelRect = transform as RectTransform;

        if (slots == null) return;

        foreach (BoosterSlotUI slot in slots)
        {
            if (slot == null) continue;

            layoutSlots.Add(slot);
            slot.SetVisible(false);
        }
    }

    void OnEnable()
    {
        if (boosterManager == null) return;

        boosterManager.OnBoosterActivated += HandleBoosterActivated;
        boosterManager.OnBoosterExpired += HandleBoosterExpired;
    }

    void OnDisable()
    {
        if (boosterManager == null) return;

        boosterManager.OnBoosterActivated -= HandleBoosterActivated;
        boosterManager.OnBoosterExpired -= HandleBoosterExpired;
    }

    void Start()
    {
        RefreshAll();
    }

    void Update()
    {
        if (boosterManager == null) return;

        RefreshVisible();
        UpdateFill();
        UpdateLayout();
    }

    void RefreshVisible()
    {
        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot == null) continue;

            bool active = boosterManager.IsActive(slot.BoosterType);
            if (slot.IsVisible != active) slot.SetVisible(active);
        }
    }

    void UpdateFill()
    {
        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot == null || !slot.IsVisible) continue;

            float duration = boosterManager.GetDuration(slot.BoosterType);
            float remaining = boosterManager.GetRemainingTime(slot.BoosterType);

            slot.SetFillAmount(duration > 0f ? remaining / duration : 0f);
        }
    }

    void RebuildVisible()
    {
        visibleSlots.Clear();

        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot == null || !slot.IsVisible) continue;

            visibleSlots.Add(slot);
        }

        visibleSlots.Sort(CompareSlots);
    }

    int CompareSlots(BoosterSlotUI a, BoosterSlotUI b)
    {
        float remainingA = boosterManager.GetRemainingTime(a.BoosterType);
        float remainingB = boosterManager.GetRemainingTime(b.BoosterType);

        // Чем меньше осталось времени, тем выше слот.
        // Новый бустер (максимум времени) встаёт в конец списка, снизу.
        return remainingA.CompareTo(remainingB);
    }

    void UpdateLayout()
    {
        RebuildVisible();

        layoutTargets.Clear();

        float step = slotHeight + slotSpacing;

        for (int i = 0; i < visibleSlots.Count; i++)
        {
            layoutTargets[visibleSlots[i]] = -i * step;
        }

        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot == null || !slot.IsVisible) continue;

            RectTransform rect = slot.transform as RectTransform;
            if (rect == null) continue;

            float target;
            if (!layoutTargets.TryGetValue(slot, out target)) continue;

            Vector2 position = rect.anchoredPosition;
            position.y = Mathf.MoveTowards(position.y, target, Time.deltaTime * reorderSpeed * step);
            rect.anchoredPosition = position;
        }

        ResizePanel();
    }

    void ResizePanel()
    {
        if (panelRect == null) return;

        int count = visibleSlots.Count;
        float height = count > 0 ? count * slotHeight + (count - 1) * slotSpacing + panelHeightPadding : panelHeightPadding;

        Vector2 size = panelRect.sizeDelta;
        size.y = Mathf.MoveTowards(size.y, height, Time.deltaTime * reorderSpeed * Mathf.Max(slotHeight, 100f));
        panelRect.sizeDelta = size;
    }

    void HandleBoosterActivated(BoosterType type)
    {
        BoosterSlotUI slot = FindSlot(type);
        if (slot == null) return;

        float duration = boosterManager != null ? boosterManager.GetDuration(type) : 1f;
        slot.SetFillAmount(duration > 0f ? 1f : 0f);
        slot.SetVisible(true);
    }

    void HandleBoosterExpired(BoosterType type)
    {
        BoosterSlotUI slot = FindSlot(type);
        if (slot == null) return;

        slot.SetFillAmount(0f);
        slot.SetVisible(false);
    }

    void RefreshAll()
    {
        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot == null) continue;

            bool active = boosterManager != null && boosterManager.IsActive(slot.BoosterType);
            slot.SetVisible(active);
        }
    }

    BoosterSlotUI FindSlot(BoosterType type)
    {
        for (int i = 0; i < layoutSlots.Count; i++)
        {
            BoosterSlotUI slot = layoutSlots[i];
            if (slot != null && slot.BoosterType == type) return slot;
        }

        return null;
    }
}
