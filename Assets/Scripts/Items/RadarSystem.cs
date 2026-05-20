using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarSystem : MonoBehaviour
{
    [Header("Radar Settings")]
    public float detectionRadius = 50f; // Radio máximo de detección real
    public float displayRadius = 30f; // Distancia a la que los blips empiezan a ser puntos dentro del radar
    public float updateInterval = 0.5f; // Frecuencia de escaneo
    
    [Header("UI References")]
    public RectTransform radarCanvasRect;
    public GameObject blipPrefab; // Prefab con un Image y un RectTransform
    public Transform blipsContainer;
    
    [Header("Styling")]
    public Color innerBlipColor = Color.red; // Color cuando está dentro del display
    public Color edgeBlipColor = new Color(1f, 0.5f, 0f, 0.8f); // Color cuando está en el borde (lejos)
    public float innerBlipSize = 4f;
    public float edgeBlipSize = 8f;

    private List<RadarBlip> activeBlips = new List<RadarBlip>();
    private float timer = 0f;
    private FactionIdentity playerFaction;

    private void Start()
    {
        // El radar se instancia en la mano del Player (o deberíamos buscar al Player localmente)
        // Buscamos el Player en la raíz para tener su facción, aunque usualmente es FactionType.Player
        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            playerFaction = player.GetComponent<FactionIdentity>();
        }
        else
        {
            // Fallback: buscar un objeto con etiqueta Player
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
                playerFaction = pObj.GetComponent<FactionIdentity>();
        }

        // Crear una reserva inicial de Blips por si acaso
        for (int i = 0; i < 20; i++)
        {
            CreateBlip();
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            ScanEnvironment();
            timer = 0f;
        }

        UpdateBlipsPosition();
    }

    private void ScanEnvironment()
    {
        // Ocultar todos los blips actuales temporalmente
        foreach (var blip in activeBlips)
        {
            blip.gameObject.SetActive(false);
            blip.target = null;
        }

        // Si no tenemos al jugador definido, no hacemos nada
        if (playerFaction == null) return;
        
        Transform playerTransform = playerFaction.transform;

        // Buscar facciones en el radio
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, detectionRadius);
        
        int blipIndex = 0;
        
        foreach (Collider col in colliders)
        {
            FactionIdentity entityFaction = col.GetComponentInParent<FactionIdentity>();
            
            // Si tiene facción y es enemigo del jugador
            if (entityFaction != null && entityFaction.gameObject != playerFaction.gameObject && playerFaction.IsEnemy(entityFaction.myFaction))
            {
                // Evitar múltiples detecciones del mismo root (un soldado tiene varios colliders en los huesos)
                if (IsTargetAlreadyTracked(entityFaction.transform))
                    continue;

                RadarBlip blip = GetAvailableBlip(blipIndex);
                blip.target = entityFaction.transform;
                blip.gameObject.SetActive(true);
                blipIndex++;
            }
        }
    }

    private bool IsTargetAlreadyTracked(Transform t)
    {
        foreach (var blip in activeBlips)
        {
            if (blip.gameObject.activeSelf && blip.target == t)
                return true;
        }
        return false;
    }

    private RadarBlip GetAvailableBlip(int index)
    {
        if (index < activeBlips.Count)
        {
            return activeBlips[index];
        }
        else
        {
            return CreateBlip();
        }
    }

    private RadarBlip CreateBlip()
    {
        GameObject newBlipObj = Instantiate(blipPrefab, blipsContainer);
        newBlipObj.SetActive(false);
        RadarBlip blip = newBlipObj.GetComponent<RadarBlip>();
        
        // Si no tiene el script, se lo añadimos
        if (blip == null)
        {
            blip = newBlipObj.AddComponent<RadarBlip>();
            blip.rectTransform = newBlipObj.GetComponent<RectTransform>();
            blip.image = newBlipObj.GetComponent<Image>();
        }
        
        // Forzar resets para evitar problemas de escala/posición por el Instantiate
        blip.rectTransform.localScale = Vector3.one;
        blip.rectTransform.localRotation = Quaternion.identity;
        Vector3 localPos = blip.rectTransform.localPosition;
        localPos.z = 0;
        blip.rectTransform.localPosition = localPos;
        
        // Forzar el anclaje al centro por si el prefab no lo tiene
        blip.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        blip.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        blip.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        activeBlips.Add(blip);
        return blip;
    }

    private void UpdateBlipsPosition()
    {
        if (playerFaction == null) return;
        
        Transform playerTransform = playerFaction.transform;
        
        // Radio del Canvas (mitad del ancho) usando la escala real del canvas para evitar cálculos minúsculos
        // Como el Canvas está muy escalado (0.006 etc) y el rect transform es 100x100,
        // la posición anclada del RectTransform (anchoredPosition) opera en el espacio sin escalar (0 a 50).
        // Por tanto, maxUIRadius DEBE basarse puramente en rect.width/height, ignorando la escala transform.
        float radarUIHalfWidth = radarCanvasRect.rect.width / 2f;
        float radarUIHalfHeight = radarCanvasRect.rect.height / 2f;
        
        // Si rect.width es 0, intentar con sizeDelta
        if (radarUIHalfWidth == 0) radarUIHalfWidth = radarCanvasRect.sizeDelta.x / 2f;
        if (radarUIHalfHeight == 0) radarUIHalfHeight = radarCanvasRect.sizeDelta.y / 2f;
        
        // Si por algún motivo el canvas no tiene tamaño, usamos 50 por defecto (mitad de 100)
        if (radarUIHalfWidth <= 0) radarUIHalfWidth = 50f;
        if (radarUIHalfHeight <= 0) radarUIHalfHeight = 50f;
        
        float maxUIRadius = Mathf.Min(radarUIHalfWidth, radarUIHalfHeight) - edgeBlipSize;
        if (maxUIRadius <= 0) maxUIRadius = 42f; // Fallback de seguridad

        foreach (var blip in activeBlips)
        {
            if (!blip.gameObject.activeSelf || blip.target == null) continue;

            // Diferencia en el mundo
            Vector3 worldDelta = blip.target.position - playerTransform.position;
            
            // Ignorar la altura para el radar 2D
            worldDelta.y = 0; 
            
            float distanceToTarget = worldDelta.magnitude;

            // Obtener los ejes FÍSICOS del Canvas en el mundo y aplanarlos al plano XZ (suelo)
            // Esto asegura que sin importar cómo esté rotado ("en diagonal") el radar físicamente, 
            // el punto rojo siempre apuntará en la dirección real del enemigo en el mundo.
            Vector3 canvasRight = radarCanvasRect.right;
            canvasRight.y = 0;
            if (canvasRight.sqrMagnitude < 0.001f) canvasRight = playerTransform.right;
            canvasRight.Normalize();
            
            Vector3 canvasUp = radarCanvasRect.up; // El "Arriba" de la interfaz 2D
            canvasUp.y = 0;
            if (canvasUp.sqrMagnitude < 0.001f) canvasUp = playerTransform.forward;
            canvasUp.Normalize();

            // Vector direccional hacia el objetivo
            Vector3 dirToTarget = worldDelta.normalized;

            // Calcular x e y proyectando la dirección sobre los ejes físicos del Canvas
            float radarX = Vector3.Dot(dirToTarget, canvasRight);
            float radarY = Vector3.Dot(dirToTarget, canvasUp);
            
            Vector2 relativePos = new Vector2(radarX, radarY);

            // Determinar si es un punto interior o de borde
            if (distanceToTarget <= displayRadius)
            {
                // Es un punto interior
                float normalizedDistance = distanceToTarget / displayRadius;
                
                blip.rectTransform.anchoredPosition = relativePos * (maxUIRadius * normalizedDistance);
                blip.rectTransform.localPosition = new Vector3(blip.rectTransform.anchoredPosition.x, blip.rectTransform.anchoredPosition.y, -0.1f); // Forzar Z para que se vea sobre el fondo
                blip.image.color = innerBlipColor;
                blip.rectTransform.sizeDelta = new Vector2(innerBlipSize, innerBlipSize);
                // DEBUG
                Debug.Log($"[Radar] {blip.target.name} (Dist: {distanceToTarget:F1}) - PosRelativa: {relativePos} - AnchoredPos: {blip.rectTransform.anchoredPosition}");
            }
            else
            {
                // Está lejos, lo ponemos en el borde
                blip.rectTransform.anchoredPosition = relativePos.normalized * maxUIRadius;
                blip.rectTransform.localPosition = new Vector3(blip.rectTransform.anchoredPosition.x, blip.rectTransform.anchoredPosition.y, -0.1f);
                blip.image.color = edgeBlipColor;
                blip.rectTransform.sizeDelta = new Vector2(edgeBlipSize, edgeBlipSize);
            }
        }
    }
}
