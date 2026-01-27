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

    public InputAction moveInput;

    Vector3 StartPos;

    private void OnEnable()
    {
        moveInput.Enable();
    }

    private void OnDisable()
    {
        moveInput.Disable();
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
        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * Frequency) * Amount * 1.4f, Smooth * Time.deltaTime);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * Frequency / 2f) * Amount * 1.6f, Smooth * Time.deltaTime);
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
