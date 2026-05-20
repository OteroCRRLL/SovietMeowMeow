using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarBlip : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image image;
    
    // Componentes para el modo borde
    [HideInInspector]
    public Transform target;
}
