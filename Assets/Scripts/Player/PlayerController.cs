using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// This class handles the movement of the player with given input from the input manager
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The speed at which the player moves")]
    public float moveSpeed = 20f;
    [Tooltip("The speed at which the player runs")]
    public float runSpeed = 22f;
    [Tooltip("The speed at which the player rotates to look left and right (calculated in degrees)")]
    public float lookSpeed = 60f;
    [Tooltip("The power with which the player jumps")]
    public float jumpPower = 8f;
    [Tooltip("The strength of gravity")]
    public float gravity = 9.81f;

    [Tooltip("The falling gravity multiplier after apex of jump. This is used to reduce floatiness.")]
    public float fallingMultiplier = 1.75f;

    [Header("Jump Timing")]
    public float jumpTimeLeniency = 0.25f;

    [Header("Input Actions")]
    [Tooltip("The bindings for moving the player")]
    public InputAction moveInput;
    [Tooltip("The binding to make the player run")]
    public InputAction runInput;
    [Tooltip("The binding to make the player jump")]
    public InputAction jumpInput;
    [Tooltip("The binding for making the player look left and right")]
    public InputAction lookInput;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaDrain = 20f;
    public float staminaRegen = 15f;
    private bool isExhausted = false;
    private bool shiftMustBeReleased = false;
    private bool hasInfiniteStamina = false;

    [Header("Adrenaline Effects")]
    public Camera playerCamera;
    public UnityEngine.Rendering.Volume adrenalineVolume;
    public float normalFOV = 60f;
    public float adrenalineFOV = 80f;
    public float fovTransitionSpeed = 5f;

    [Header("Audio")]
    public AudioSource footstepsAudioSource;
    public AudioClip snowFootstepsClip;
    public AudioSource catAudioSource;
    public AudioClip catDamageClip;
    public AudioClip catHissingClip;
    public float catHissingRadius = 8f;
    public float catHissingCheckInterval = 0.25f;
    [Range(0f, 1f)] public float catHissingChance = 0.333f;
    public LayerMask catHissingDetectionLayers = ~0;

    // The character controller component on the player
    private CharacterController controller;
    private HealthSystem playerHealth;
    private FactionIdentity playerFaction;
    private float lastHealthPercent;
    private float nextHissingCheckTime;
    private bool enemyWasInHissingRadius;
    private float footstepGraceTimer;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled.
    /// Enables the input actions.
    /// </summary>
    private void OnEnable()
    {
        moveInput.Enable();
        runInput.Enable();
        jumpInput.Enable();
        lookInput.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled.
    /// Disables the input actions.
    /// </summary>
    private void OnDisable()
    {
        moveInput.Disable();
        runInput.Disable();
        jumpInput.Disable();
        lookInput.Disable();
    }

    void Start()
    {
        HideMouse();
        SetUpCharacterController();
        SetUpRigidbody();
        SetUpAudio();
    }

    private void SetUpAudio()
    {
        if (footstepsAudioSource == null) footstepsAudioSource = gameObject.AddComponent<AudioSource>();
        if (catAudioSource == null) catAudioSource = gameObject.AddComponent<AudioSource>();

        footstepsAudioSource.loop = true;
        footstepsAudioSource.clip = snowFootstepsClip;
        playerFaction = GetComponent<FactionIdentity>();
        playerHealth = GetComponent<HealthSystem>();

        if (playerHealth != null)
        {
            lastHealthPercent = playerHealth.maxHealth > 0f ? playerHealth.CurrentHealth / playerHealth.maxHealth : 1f;
            playerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
            playerHealth.onHealthChanged.AddListener(HandleHealthChanged);
        }
    }

    /// <summary>
    /// Set's up the character controller component for use in this script
    /// </summary>
    private void SetUpCharacterController()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("The player controller script does not have a character controller on the same game object!");
        }
    }

    /// <summary>
    /// Set's up the Rigidbody component for use in this script
    /// void (no return)
    /// </summary>
    private void SetUpRigidbody()
    {
        Rigidbody playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.useGravity = false;
    }

    /// <summary>
    /// Standard Unity function called once every frame
    /// Process the movement and horizontal rotation on the player
    /// void (no return)
    /// </summary>
    void Update()
    {
        ProcessMovement();
        ProcessHorizontalRotation();
        ProcessAdrenalineEffects();
        ProcessAudio();
    }

    private void ProcessAudio()
    {
        ProcessFootstepsAudio();
        ProcessCatHissingAudio();
    }

    private void ProcessFootstepsAudio()
    {
        if (footstepsAudioSource == null || snowFootstepsClip == null || controller == null) return;

        bool isMoving = moveInput.ReadValue<Vector2>().sqrMagnitude > 0.01f;

        // Tolerance for isGrounded flickering on terrain/slopes
        if (controller.isGrounded)
        {
            footstepGraceTimer = 0.2f;
        }
        else
        {
            footstepGraceTimer -= Time.deltaTime;
        }

        if (isMoving && footstepGraceTimer > 0f)
        {
            if (!footstepsAudioSource.isPlaying)
            {
                // Play starts from 0 if stopped, or resumes if paused. 
                footstepsAudioSource.Play();
            }
        }
        else
        {
            if (footstepsAudioSource.isPlaying)
            {
                footstepsAudioSource.Pause(); // Pause instead of Stop avoids restarting sound completely if toggling rapidly
            }
        }
    }

    private void ProcessCatHissingAudio()
    {
        if (catHissingClip == null || Time.time < nextHissingCheckTime) return;
        nextHissingCheckTime = Time.time + catHissingCheckInterval;

        bool enemyInRadius = HasEnemyInHissingRadius();
        if (enemyInRadius && !enemyWasInHissingRadius && Random.value <= catHissingChance)
        {
            PlayCatClip(catHissingClip, 1f);
        }

        enemyWasInHissingRadius = enemyInRadius;
    }

    private bool HasEnemyInHissingRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, catHissingRadius, catHissingDetectionLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform.root == transform.root) continue;

            HealthSystem health = hit.GetComponentInParent<HealthSystem>();
            if (health != null && health.IsDead) continue;

            FactionIdentity faction = hit.GetComponentInParent<FactionIdentity>();
            if (playerFaction != null && faction != null)
            {
                if (playerFaction.IsEnemy(faction.myFaction)) return true;
            }
            else if (hit.gameObject.tag == "Enemy" || hit.GetComponentInParent<SoldierBrain>() != null || hit.GetComponentInParent<DroneBrain>() != null || hit.GetComponentInParent<TankBrain>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleHealthChanged(float healthPercent)
    {
        if (healthPercent < lastHealthPercent)
        {
            PlayCatClip(catDamageClip, 0.1f);
        }

        lastHealthPercent = healthPercent;
    }

    private void PlayCatClip(AudioClip clip, float startTime)
    {
        if (catAudioSource == null || clip == null) return;

        catAudioSource.clip = clip;
        catAudioSource.loop = false;
        catAudioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
        catAudioSource.Play();
    }

    void ProcessAdrenalineEffects()
    {
        if (playerCamera != null)
        {
            float targetFOV = hasInfiniteStamina ? adrenalineFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
        }

        if (adrenalineVolume != null)
        {
            float targetWeight = hasInfiniteStamina ? 1f : 0f;
            adrenalineVolume.weight = Mathf.Lerp(adrenalineVolume.weight, targetWeight, Time.deltaTime * fovTransitionSpeed);
        }
    }

    /// <summary>
    /// This processes the movement on the player based on the player inputs as well as gravity.
    /// </summary>
    /// <returns></returns>
    Vector3 moveDirection;
    float timeToStopBeingLenient = 0f; // track when to stop being lenient based on the jumpTimeLienciency
    void ProcessMovement()
    {
        // Get the input from the player
        float leftRightInput = moveInput.ReadValue<Vector2>().x;
        float forwardBackwardInput = moveInput.ReadValue<Vector2>().y;
        bool jumpPressed = jumpInput.triggered;
        bool isMoving = leftRightInput != 0 || forwardBackwardInput != 0;
        bool isShiftPressed = runInput.IsPressed();
        
        float currentSpeed = runInput.IsPressed() ? runSpeed : moveSpeed;

        if(!isShiftPressed)
        {
            shiftMustBeReleased = false;
        }
        if (isShiftPressed && isMoving && currentStamina > 0 && !isExhausted && !shiftMustBeReleased)
        {
            currentSpeed = runSpeed;
            if (!hasInfiniteStamina)
            {
                currentStamina -= staminaDrain * Time.deltaTime;
            }

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
                shiftMustBeReleased = true;
            }
        }
        else
        {
            currentSpeed = moveSpeed;
            if (currentStamina < maxStamina)
            {
                if (!hasInfiniteStamina)
                {
                    currentStamina += staminaRegen * Time.deltaTime;
                }
                else
                {
                    currentStamina = maxStamina; // Recover instantly when infinite stamina is active
                }

                if (isExhausted && currentStamina > (maxStamina * 0.2f))
                {
                    isExhausted = false;
                }
            }
        }
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // Handle the control of the player while it is on the ground
        if (controller.isGrounded && moveDirection.y <= 0) // could also use RayCastGrounded instead of isGrounded
        {

            timeToStopBeingLenient = Time.time + jumpTimeLeniency;

            // y is 0 here since the player is on the ground
            moveDirection = new Vector3(leftRightInput, 0, forwardBackwardInput);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection = moveDirection * currentSpeed;

            if (jumpPressed)
            {
                moveDirection.y = jumpPower;
            }

        }
        else
        {
            moveDirection = new Vector3(leftRightInput * currentSpeed, moveDirection.y, forwardBackwardInput * currentSpeed);
            moveDirection = transform.TransformDirection(moveDirection);

            if (jumpPressed && Time.time < timeToStopBeingLenient)
            {
                moveDirection.y = jumpPower;
            }

        }

        if (controller.isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -0.3f;
        }

        // add effect of gravity
        if (isFalling())
        {
            // Mario style falling where gravity is more on fall than on jump, to make the player seem less floaty
            moveDirection.y -= gravity * fallingMultiplier * Time.deltaTime;
        }
        else
        {
            // Apply regular gravity
            moveDirection.y -= gravity * Time.deltaTime;
        }


        controller.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// Rotate the player based on the look inputs
    /// </summary>
    /// <returns></returns>
    void ProcessHorizontalRotation()
    {
        float horizontalSensitivityMultiplier = 1;
        if (PlayerPrefs.HasKey("HorizontalMouseSensitivity"))
        {
            horizontalSensitivityMultiplier = PlayerPrefs.GetFloat("HorizontalMouseSensitivity");
        }

        float horizontalLookInput = lookInput.ReadValue<Vector2>().x * horizontalSensitivityMultiplier;
        Vector3 playerRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(new Vector3(0, playerRotation.y + horizontalLookInput * lookSpeed * Time.deltaTime, 0));
    }

    /// <summary>
    /// Checks to see if player is falling (vs. going up from jump)
    /// </summary>
    /// <returns></returns>
    float previousHeight; // tracks the previous height to determine when the player is falling
    bool isFalling()
    {
        bool isFalling = false;

        if (previousHeight > transform.position.y)
        {
            isFalling = true;
        }

        previousHeight = transform.position.y;

        return isFalling;
    }

    void HideMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ApplyInfiniteStamina(float duration)
    {
        StartCoroutine(InfiniteStaminaCoroutine(duration));
    }

    private IEnumerator InfiniteStaminaCoroutine(float duration)
    {
        hasInfiniteStamina = true;
        currentStamina = maxStamina;
        isExhausted = false;
        shiftMustBeReleased = false;
        
        yield return new WaitForSeconds(duration);
        
        hasInfiniteStamina = false;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.RemoveListener(HandleHealthChanged);
        }
    }
}
