using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBarController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] Image staminaFilledBar;

    void Update()
    {
        staminaFilledBar.fillAmount = playerController.currentStamina / playerController.maxStamina;
    }
}
