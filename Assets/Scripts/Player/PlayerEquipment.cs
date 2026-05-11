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
    private Vector3[] originalIconScales = new Vector3[3];

    private void Awake()
    {
        // Guardar las escalas originales de los padres (los slots) para respetarlas al hacer zoom
        if (hudSlotIcons != null)
        {
            originalIconScales = new Vector3[hudSlotIcons.Length];
            for (int i = 0; i < hudSlotIcons.Length; i++)
            {
                if (hudSlotIcons[i] != null && hudSlotIcons[i].transform.parent != null)
                {
                    originalIconScales[i] = hudSlotIcons[i].transform.parent.localScale;
                }
                else
                {
                    originalIconScales[i] = Vector3.one;
                }
            }
        }
    }

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

    public void RefreshEquipment()
    {
        // 1. Limpiar los objetos instanciados actualmente
        for (int i = 0; i < instantiatedItems.Length; i++)
        {
            if (instantiatedItems[i] != null)
            {
                Destroy(instantiatedItems[i]);
                instantiatedItems[i] = null;
            }
        }

        // 2. Restaurar las escalas de los iconos a su tamaño original antes de recargar
        if (hudSlotIcons != null)
        {
            for (int i = 0; i < hudSlotIcons.Length; i++)
            {
                if (hudSlotIcons[i] != null && hudSlotIcons[i].transform.parent != null)
                {
                    hudSlotIcons[i].transform.parent.localScale = originalIconScales[i];
                }
            }
        }

        // 3. Volver a cargar todo desde el GameManager
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
                if (data != null)
                {
                    // 1. Configurar el icono en el HUD SIEMPRE, aunque no tenga modelo 3D
                    if (hudSlotIcons != null && i < hudSlotIcons.Length && hudSlotIcons[i] != null)
                    {
                        hudSlotIcons[i].sprite = data.uiIcon;
                        hudSlotIcons[i].gameObject.SetActive(true);
                    }

                    // 2. Instanciar el prefab 3D en la mano solo si existe
                    if (data.itemPrefab != null)
                    {
                        instantiatedItems[i] = Instantiate(data.itemPrefab, handTransform);
                        instantiatedItems[i].SetActive(false); // Ocultar todos al principio
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
        float scrollValue = 0f;
        
        try 
        {
            // Intentamos leerlo como un Eje 1D (float) que es como lo tienes configurado
            scrollValue = scrollAction.ReadValue<float>();
        }
        catch 
        {
            try 
            {
                // Si falla, intentamos leerlo como un Vector2 (X, Y)
                scrollValue = scrollAction.ReadValue<Vector2>().y;
            }
            catch {}
        }

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
            if (hudSlotIcons[currentSlotIndex].transform.parent != null)
            {
                hudSlotIcons[currentSlotIndex].transform.parent.localScale = originalIconScales[currentSlotIndex]; // Tamaño original
            }
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
            if (hudSlotIcons[currentSlotIndex].transform.parent != null)
            {
                hudSlotIcons[currentSlotIndex].transform.parent.localScale = originalIconScales[currentSlotIndex] * 1.15f; // Hacerlo un poquiiiito más grande
            }
        }
    }
}
