using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryAppManager : MonoBehaviour
{
    [Header("UI Slots de Equipamiento (Max 3)")]
    public InventorySlotUI[] equipmentSlots;

    [Header("Lista de Inventario")]
    public Transform inventoryContentContainer;
    public GameObject inventoryItemPrefab;

    private int selectedEquipmentSlot = -1; // -1 significa que no hay slot seleccionado

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshEquipmentSlots();
        PopulateInventoryList();
    }

    private void RefreshEquipmentSlots()
    {
        if (GameManager.instance == null || GameManager.instance.itemDatabase == null) return;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (i >= GameManager.instance.equippedItems.Length) break;

            string itemID = GameManager.instance.equippedItems[i];
            
            if (string.IsNullOrEmpty(itemID))
            {
                equipmentSlots[i].SetupEmpty(i, this);
            }
            else
            {
                ItemData data = GameManager.instance.itemDatabase.GetItemByID(itemID);
                equipmentSlots[i].Setup(data, i, this);
            }
        }
    }

    private void PopulateInventoryList()
    {
        foreach (Transform child in inventoryContentContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameManager.instance == null || GameManager.instance.itemDatabase == null) return;

        // Diccionario para contar cuántos tenemos de cada uno
        Dictionary<string, int> inventoryCounts = new Dictionary<string, int>();
        foreach(string id in GameManager.instance.hubInventory)
        {
            if (inventoryCounts.ContainsKey(id)) inventoryCounts[id]++;
            else inventoryCounts[id] = 1;
        }

        // Restar los que ya están equipados
        foreach(string id in GameManager.instance.equippedItems)
        {
            if (!string.IsNullOrEmpty(id) && inventoryCounts.ContainsKey(id))
            {
                inventoryCounts[id]--;
            }
        }

        // Crear botones para los items disponibles
        foreach(var kvp in inventoryCounts)
        {
            if (kvp.Value > 0)
            {
                ItemData item = GameManager.instance.itemDatabase.GetItemByID(kvp.Key);
                if (item != null)
                {
                    GameObject newObj = Instantiate(inventoryItemPrefab, inventoryContentContainer, false);
                    InventoryItemUI uiScript = newObj.GetComponent<InventoryItemUI>();
                    if (uiScript != null)
                    {
                        uiScript.Setup(item, kvp.Value, this);
                    }
                }
            }
        }
    }

    public void SelectEquipmentSlot(int index)
    {
        selectedEquipmentSlot = index;
        Debug.Log("Has hecho click en el slot que el Inspector considera que es el: " + (index + 1) + " (Índice interno: " + index + ").");
        // Aquí podrías añadir lógica visual para destacar el slot seleccionado
    }

    public void OnInventoryItemClicked(ItemData item)
    {
        if (selectedEquipmentSlot != -1 && GameManager.instance != null)
        {
            GameManager.instance.equippedItems[selectedEquipmentSlot] = item.itemID;
            GameManager.instance.SaveGame(); // Guardar el nuevo equipamiento
            RefreshUI();
            
            // Avisar al jugador para que actualice su HUD y modelo 3D en tiempo real
            PlayerEquipment playerEq = FindObjectOfType<PlayerEquipment>();
            if (playerEq != null) playerEq.RefreshEquipment();

            selectedEquipmentSlot = -1; // Deseleccionar
        }
        else
        {
            Debug.Log("Selecciona un slot de equipamiento (1, 2 o 3) arriba primero.");
        }
    }

    public void UnequipSlot(int slotIndex)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.equippedItems[slotIndex] = "";
            GameManager.instance.SaveGame();
            RefreshUI();
            
            // Avisar al jugador para que actualice su HUD y modelo 3D en tiempo real
            PlayerEquipment playerEq = FindObjectOfType<PlayerEquipment>();
            if (playerEq != null) playerEq.RefreshEquipment();
        }
    }
}