using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class UpdateRadarPrefab
{
    [MenuItem("Tools/Update Radar Prefab")]
    public static void Execute()
    {
        string prefabPath = "Assets/Prefabs/Items/Radar.prefab";
        GameObject radarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (radarPrefab == null)
        {
            Debug.LogError("Radar prefab not found!");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(radarPrefab) as GameObject;

        // Create Canvas
        GameObject canvasObj = new GameObject("RadarCanvas");
        canvasObj.transform.SetParent(instance.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(100, 100);
        
        // El cilindro del radar en su transform tiene scale 0.33, 0.012, 0.33
        // La altura (Y) es muy pequeña, por lo que con un Y un poco mayor debería estar por encima.
        // También depende del pivote del mesh, probamos con Y=1.5
        canvasRT.localPosition = new Vector3(0, 1.5f, 0);
        
        // Giramos el canvas para que apunte hacia arriba (el eje Y positivo del radar)
        canvasRT.localRotation = Quaternion.Euler(90, 0, 0); 
        
        // Como el padre (Radar) está escalado raro, contrarrestamos un poco, 
        // pero principalmente hacemos el canvas pequeño.
        canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0.2f, 0, 0.8f); // Verde oscuro
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        
        // Círculo del radar (opcional, para que parezca más un radar)
        // Podríamos usar un sprite de máscara si lo hubiera, pero lo dejamos cuadrado o dependiente del material.

        // Blips Container
        GameObject blipsContainer = new GameObject("Blips");
        blipsContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform blipsRT = blipsContainer.AddComponent<RectTransform>();
        blipsRT.anchorMin = Vector2.zero;
        blipsRT.anchorMax = Vector2.one;
        blipsRT.sizeDelta = Vector2.zero;

        // Blip Prefab inside the instance (we'll save it as a separate prefab later)
        GameObject blipTemplate = new GameObject("RadarBlip");
        Image blipImg = blipTemplate.AddComponent<Image>();
        blipImg.color = Color.red;
        RectTransform blipRT = blipTemplate.GetComponent<RectTransform>();
        blipRT.sizeDelta = new Vector2(4, 4);
        
        // Add RadarBlip component
        RadarBlip blipComp = blipTemplate.AddComponent<RadarBlip>();
        blipComp.rectTransform = blipRT;
        blipComp.image = blipImg;

        // Save blip as its own prefab
        string blipPrefabPath = "Assets/Prefabs/Items/RadarBlipPrefab.prefab";
        GameObject savedBlipPrefab = PrefabUtility.SaveAsPrefabAsset(blipTemplate, blipPrefabPath);
        Object.DestroyImmediate(blipTemplate);

        // Add RadarSystem
        RadarSystem radarSys = instance.AddComponent<RadarSystem>();
        radarSys.radarCanvasRect = canvasRT;
        radarSys.blipPrefab = savedBlipPrefab;
        radarSys.blipsContainer = blipsContainer.transform;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);

        Debug.Log("Radar prefab updated successfully!");
    }
}
