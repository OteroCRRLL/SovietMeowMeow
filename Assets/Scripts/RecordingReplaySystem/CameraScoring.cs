using System.Collections.Generic;
using UnityEngine;

public class CameraScoring : MonoBehaviour
{
    public static CameraScoring instance;

    [Header("Configuración de Grabación")]
    public float rayDistance = 50f;
    public int pointsPerTarget = 100;
    public List<string> targetTags = new List<string>();

    private int currentScore = 0;
    private HashSet<GameObject> alreadyRecordedObjects = new HashSet<GameObject>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (ReplayManager.instance != null && ReplayManager.instance.IsRecordingGlobal)
        {
            ScanForContent();
        }
    }

    private void ScanForContent()
    {
        // Dibuja una línea verde en la ventana Scene para visualizar el rayo
        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.green);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance))
        {
            GameObject hitObj = hit.collider.gameObject;

            if (targetTags.Contains(hitObj.tag) && !alreadyRecordedObjects.Contains(hitObj))
            {
                alreadyRecordedObjects.Add(hitObj);
                currentScore += pointsPerTarget;
                Debug.Log($"Grabado {hitObj.name} Puntos actuales: {currentScore}");
            }
        }
    }

    public void ShowFinalScore()
    {
        // Ahora el resultado final sale limpio por la consola
        Debug.Log($"--- EXTRACCIÓN COMPLETADA --- \n VISITAS DEL VÍDEO TOTALES: {currentScore}");
    }

    public void ResetScore()
    {
        currentScore = 0;
        alreadyRecordedObjects.Clear();
        Debug.Log("Puntuación reiniciada para un nuevo despliegue.");
    }
}