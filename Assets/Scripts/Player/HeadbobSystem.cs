using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HeadbobSystem : MonoBehaviour
{
    [Range(0.001f, 0.01f)]
    public float Amount = 0.002f;

    [Range(1f, 30f)]
    public float Frequency = 10.0f;

    [Range(10f, 100f)]
    public float Smooth = 10.0f;

    [Header("Run Settings")]
    public float runFrequencyMultiplier = 1.5f;
    public float runAmountMultiplier = 1.5f;

    public InputAction moveInput;
    public InputAction runInput;

    Vector3 StartPos;

    private void OnEnable()
    {
        moveInput.Enable();
        runInput.Enable();
    }

    private void OnDisable()
    {
        moveInput.Disable();
        runInput.Disable();
    }


    // Start is called before the first frame update
    void Start()
    {
        StartPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        CheckForHeadbobTrigger();
    }

    private void CheckForHeadbobTrigger()
    {
        float inputMagnitude = moveInput.ReadValue<Vector2>().magnitude;

        if (inputMagnitude > 0)
        {
            StartHeadBob();
        }
    }

    private Vector3 StartHeadBob()
    {
        Vector3 pos = Vector3.zero;

        // Calcula la frecuencia y cantidad en base a si el jugador corre
        float currentFreq = runInput.IsPressed() ? Frequency * runFrequencyMultiplier : Frequency;
        float currentAmount = runInput.IsPressed() ? Amount * runAmountMultiplier : Amount;

        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * currentFreq) * currentAmount * 1.4f, Smooth * Time.deltaTime);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * currentFreq / 2f) * currentAmount * 1.6f, Smooth * Time.deltaTime);
        transform.localPosition += pos;

        return pos;
    }

    private void StopHeadBob()
    {
        if (transform.localPosition == StartPos)
        {
            return;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, StartPos, 1 * Time.deltaTime);
    }
}