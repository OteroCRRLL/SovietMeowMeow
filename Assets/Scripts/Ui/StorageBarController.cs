using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorageBarController : MonoBehaviour
{
    [SerializeField] private Image filledBar;

    void Update()
    {
        if (ReplayManager.instance != null && filledBar != null)
        {
            filledBar.fillAmount = (ReplayManager.instance.currentCapacity / ReplayManager.instance.maxCapacity);
        }
    }
}
