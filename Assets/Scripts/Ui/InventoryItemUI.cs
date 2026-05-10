using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public Button selectButton;

    private ItemData currentItem;
    private InventoryAppManager appManager;

    public void Setup(ItemData item, int count, InventoryAppManager manager)
    {
        currentItem = item;
        appManager = manager;

        if (iconImage != null) iconImage.sprite = item.uiIcon;
        if (nameText != null) nameText.text = item.itemName;
        if (countText != null) countText.text = $"x{count}";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);
    }

    private void OnSelectClicked()
    {
        if (appManager != null && currentItem != null)
        {
            appManager.OnInventoryItemClicked(currentItem);
        }
    }
}