using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopAppManager : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI moneyText;
    public Transform contentContainer; // El lugar donde se instanciarán los items
    public GameObject shopItemPrefab; // El prefab de la UI de cada item

    private void OnEnable()
    {
        UpdateUI();
        PopulateShop();
    }

    private void UpdateUI()
    {
        if (GameManager.instance != null && moneyText != null)
        {
            moneyText.text = $"Dinero: ${GameManager.instance.currentMoney}";
        }
    }

    private void PopulateShop()
    {
        // Limpiar el contenedor primero
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameManager.instance == null || GameManager.instance.itemDatabase == null) return;

        foreach (ItemData item in GameManager.instance.itemDatabase.allItems)
        {
            GameObject newObj = Instantiate(shopItemPrefab, contentContainer, false);
            ShopItemUI uiScript = newObj.GetComponent<ShopItemUI>();
            if (uiScript != null)
            {
                uiScript.Setup(item, this);
            }
        }
    }

    public void OnPurchaseAttempt(ItemData item)
    {
        if (GameManager.instance != null)
        {
            if (GameManager.instance.BuyItem(item))
            {
                UpdateUI(); // Refrescar el dinero en pantalla
            }
        }
    }
}