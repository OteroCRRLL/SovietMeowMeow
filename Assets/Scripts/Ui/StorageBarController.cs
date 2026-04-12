using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorageBarController : MonoBehaviour
{
    [SerializeField] private Image filledBar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        filledBar.fillAmount = (ReplayManager.instance.currentCapacity / ReplayManager.instance.maxCapacity);

    }
}
