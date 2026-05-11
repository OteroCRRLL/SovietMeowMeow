using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    // Variables privadas para forzar que busque a los hijos y evitar errores del Inspector
    private Image iconImage;
    private GameObject emptyTextObj;
    private Button slotButton;

    private int slotIndex;
    private InventoryAppManager manager;
    private bool isEmpty = true;

    private void Awake()
    {
        // Obtenemos el botón de este mismo objeto (el marco)
        slotButton = GetComponent<Button>();

        // Buscamos obligatoriamente a los hijos por su nombre
        Transform iconTransform = transform.Find("Icono");
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("No se encontró un hijo llamado 'Icono' en " + gameObject.name);
        }

        Transform textTransform = transform.Find("Text (TMP)");
        if (textTransform != null)
        {
            emptyTextObj = textTransform.gameObject;
        }
        else
        {
            Debug.LogError("No se encontró un hijo llamado 'Text (TMP)' en " + gameObject.name);
        }
    }

    public void SetupEmpty(int index, InventoryAppManager appManager)
    {
        slotIndex = index;
        manager = appManager;
        isEmpty = true;

        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (emptyTextObj != null) emptyTextObj.SetActive(true);

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
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

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
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