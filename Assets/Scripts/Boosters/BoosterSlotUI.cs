using UnityEngine;
using UnityEngine.UI;

public class BoosterSlotUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private BoosterType boosterType = BoosterType.Magnet;

    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Image fillImage;
    [SerializeField] private Slider fillSlider;

    public BoosterType BoosterType => boosterType;

    public bool IsVisible => gameObject.activeSelf;

    void Awake()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        if (iconImage != null && iconSprite != null) iconImage.sprite = iconSprite;

        if (fillSlider == null) fillSlider = GetComponentInChildren<Slider>(true);
        if (fillImage == null && fillSlider != null && fillSlider.fillRect != null)
        {
            fillImage = fillSlider.fillRect.GetComponent<Image>();
        }
    }

    public void Setup(BoosterType type)
    {
        boosterType = type;
    }

    public void Setup(BoosterType type, Sprite sprite)
    {
        boosterType = type;
        iconSprite = sprite;

        if (iconImage != null && iconSprite != null) iconImage.sprite = iconSprite;
    }

    public void SetFillAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);

        if (fillImage != null) fillImage.fillAmount = amount;
        if (fillSlider != null) fillSlider.value = amount;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
