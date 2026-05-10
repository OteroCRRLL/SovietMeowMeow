using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public Button slotButton;
    public GameObject emptyTextObj; // Un texto que diga "Vacio"

    private int slotIndex;
    private InventoryAppManager manager;
    private bool isEmpty = true;

    public void SetupEmpty(int index, InventoryAppManager appManager)
    {
        slotIndex = index;
        manager = appManager;
        isEmpty = true;

        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (emptyTextObj != null) emptyTextObj.SetActive(true);

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    public void Setup(ItemData item, int index, InventoryAppManager appManager)
    {
        slotIndex = index;
        manager = appManager;
        isEmpty = false;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = item.uiIcon;
        }
        if (emptyTextObj != null) emptyTextObj.SetActive(false);

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (manager != null)
        {
            if (isEmpty)
            {
                manager.SelectEquipmentSlot(slotIndex);
            }
            else
            {
                manager.UnequipSlot(slotIndex);
            }
        }
    }
}