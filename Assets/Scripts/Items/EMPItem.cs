using UnityEngine;

public class EMPItem : UsableItem
{
    [Header("EMP Config")]
    public GameObject empGrenadePrefab;
    public float throwForce = 15f;
    public float upForce = 3f;

    protected override void UseItem()
    {
        if (empGrenadePrefab != null)
        {
            Camera mainCam = null;
            
            // Buscar la cámara dentro del jugador (el objeto es hijo de su mano)
            PlayerController player = GetComponentInParent<PlayerController>();
            if (player != null)
            {
                // Buscar específicamente una cámara en los hijos, asumiendo que es la ForwardCamera
                mainCam = player.GetComponentInChildren<Camera>(true); 
            }
            
            // Fallback final por nombre si todo lo demás falla
            if (mainCam == null)
            {
                GameObject camObj = GameObject.Find("ForwardCamera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogWarning("No se encontró la cámara (ForwardCamera) para lanzar la granada.");
                return;
            }

            // Instanciar la granada un poco por delante del jugador/cámara
            Vector3 spawnPosition = mainCam.transform.position + mainCam.transform.forward * 1.5f;
            GameObject grenade = Instantiate(empGrenadePrefab, spawnPosition, mainCam.transform.rotation);
            
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceToAdd = mainCam.transform.forward * throwForce + transform.up * upForce;
                rb.AddForce(forceToAdd, ForceMode.Impulse);
            }

            Debug.Log("Granada EMP lanzada.");
            ConsumeItem(); // Desaparece del inventario
        }
        else
        {
            Debug.LogWarning("Falta el prefab de la Granada EMP en el inspector del EMPItem.");
        }
    }
}
