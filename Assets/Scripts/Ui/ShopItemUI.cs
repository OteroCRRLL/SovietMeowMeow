using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ItemData currentItem;
    private ShopAppManager shopManager;

    public void Setup(ItemData item, ShopAppManager manager)
    {
        currentItem = item;
        shopManager = manager;

        if (iconImage != null) iconImage.sprite = item.uiIcon;
        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = $"${item.price}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        if (shopManager != null && currentItem != null)
        {
            shopManager.OnPurchaseAttempt(currentItem);
        }
    }
}