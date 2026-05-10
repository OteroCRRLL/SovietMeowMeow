using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Equipamiento Visual")]
    public Transform handTransform; // Dónde se instanciarán los objetos
    
    [Header("HUD del Jugador")]
    public Image[] hudSlotIcons; // Los 3 iconos en la pantalla al estilo Minecraft
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Inputs")]
    public InputAction scrollAction; // Rueda del ratón
    public InputAction[] numberActions = new InputAction[3]; // Teclas 1, 2, 3

    private GameObject[] instantiatedItems = new GameObject[3];
    private int currentSlotIndex = 0;

    private void OnEnable()
    {
        scrollAction.Enable();
        foreach (var action in numberActions) action.Enable();
    }

    private void OnDisable()
    {
        scrollAction.Disable();
        foreach (var action in numberActions) action.Disable();
    }

    private void Start()
    {
        InitializeEquipment();
    }

    private void InitializeEquipment()
    {
        if (GameManager.instance == null || GameManager.instance.itemDatabase == null) return;

        for (int i = 0; i < 3; i++)
        {
            string itemID = GameManager.instance.equippedItems[i];
            if (!string.IsNullOrEmpty(itemID))
            {
                ItemData data = GameManager.instance.itemDatabase.GetItemByID(itemID);
                if (data != null && data.itemPrefab != null)
                {
                    // Instanciar el prefab en la mano del jugador
                    instantiatedItems[i] = Instantiate(data.itemPrefab, handTransform);
                    instantiatedItems[i].SetActive(false); // Ocultar todos al principio

                    // Configurar el icono en el HUD
                    if (hudSlotIcons != null && i < hudSlotIcons.Length && hudSlotIcons[i] != null)
                    {
                        hudSlotIcons[i].sprite = data.uiIcon;
                        hudSlotIcons[i].gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                // Slot vacío
                if (hudSlotIcons != null && i < hudSlotIcons.Length && hudSlotIcons[i] != null)
                {
                    hudSlotIcons[i].gameObject.SetActive(false);
                }
            }
        }

        // Seleccionar el primer slot por defecto
        SelectSlot(0);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Teclas numéricas 1, 2, 3
        for (int i = 0; i < numberActions.Length; i++)
        {
            if (numberActions[i].triggered)
            {
                SelectSlot(i);
                return;
            }
        }

        // Rueda del ratón
        float scrollValue = scrollAction.ReadValue<Vector2>().y;
        if (scrollValue > 0)
        {
            SelectSlot((currentSlotIndex - 1 + 3) % 3);
        }
        else if (scrollValue < 0)
        {
            SelectSlot((currentSlotIndex + 1) % 3);
        }
    }

    private void SelectSlot(int index)
    {
        // Ocultar el objeto actual
        if (instantiatedItems[currentSlotIndex] != null)
        {
            instantiatedItems[currentSlotIndex].SetActive(false);
        }
        
        // Actualizar UI del HUD para deseleccionar
        if (hudSlotIcons != null && currentSlotIndex < hudSlotIcons.Length && hudSlotIcons[currentSlotIndex] != null)
        {
            hudSlotIcons[currentSlotIndex].color = unselectedColor;
            hudSlotIcons[currentSlotIndex].transform.localScale = Vector3.one; // Tamaño normal
        }

        currentSlotIndex = index;

        // Mostrar el nuevo objeto
        if (instantiatedItems[currentSlotIndex] != null)
        {
            instantiatedItems[currentSlotIndex].SetActive(true);
        }

        // Actualizar UI del HUD para seleccionar
        if (hudSlotIcons != null && currentSlotIndex < hudSlotIcons.Length && hudSlotIcons[currentSlotIndex] != null)
        {
            hudSlotIcons[currentSlotIndex].color = selectedColor;
            hudSlotIcons[currentSlotIndex].transform.localScale = Vector3.one * 1.2f; // Hacerlo un poco más grande
        }
    }
}