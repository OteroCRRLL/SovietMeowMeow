using UnityEngine;

public class SoldierController : MonoBehaviour
{
    [Header("Object References")]
    public Transform body;
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public Animator anim;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip ak47Clip;

    [Header("Voice Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip russianVoicelinesClip;

    // TIEMPO REDUCIDO PARA TESTING: De 2 a 5 segundos
    public Vector2 minMaxVoiceInterval = new Vector2(2f, 5f);

    private float lastFireTime;
    private SoldierBrain brain;
    private float nextVoiceTime;
    private float currentVoiceEndTime;

    // Segmentos: 0.0-1.0, 1.3-2.3, 3.0-3.6, 4.5-6.0, 6.6-8.0
    private readonly Vector2[] voiceSegments = new Vector2[]
    {
        new Vector2(0.0f, 1.0f),
        new Vector2(1.3f, 2.3f),
        new Vector2(3.0f, 3.6f),
        new Vector2(4.5f, 6.0f),
        new Vector2(6.6f, 8.0f)
    };

    [Header("Visual Effects")]
    public GameObject muzzleFlashPrefab;

    [Header("Settings")]
    public float rotationSpeed = 8f;
    public float bulletForce = 4000f;
    public float spreadAngle = 1.5f;

    private void Start()
    {
        
        if (audioSource != null && voiceAudioSource != null && audioSource == voiceAudioSource)
        {
            Debug.LogError("El arma y la voz están usando el MISMO AudioSource en el Inspector. El arma silenciará las voces. Asigna componentes diferentes.");
        }

        
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();

        // 2D forzado temporalmente para descartar problemas de distancia
        voiceAudioSource.spatialBlend = 0f;

        brain = GetComponent<SoldierBrain>();
        if (brain == null) brain = GetComponentInParent<SoldierBrain>();

        ScheduleNextVoiceLine();
        Debug.Log($"[Voz] Soldado inicializado. Siguiente línea programada en: {nextVoiceTime - Time.time} segundos.");
    }

    private void ScheduleNextVoiceLine()
    {
        nextVoiceTime = Time.time + Random.Range(minMaxVoiceInterval.x, minMaxVoiceInterval.y);
    }

    private void Update()
    {
        UpdateWeaponAudio();
        UpdateVoiceAudio();
    }

    private void UpdateVoiceAudio()
    {
        // Si el soldado está muerto, detenemos audios y salimos
        if (brain != null && brain.CurrentState == SoldierState.Dead)
        {
            if (voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            return;
        }

        // ¿Es hora de hablar?
        if (Time.time >= nextVoiceTime && !voiceAudioSource.isPlaying)
        {
            // COMPROBACIÓN CRÍTICA: ¿Falta el clip?
            if (russianVoicelinesClip == null)
            {
                Debug.LogWarning("[Voz] Intentando hablar, pero 'Russian Voicelines Clip' está vacío en el Inspector.");
                ScheduleNextVoiceLine(); // Lo reprogramamos para evitar bucles infinitos de errores
                return;
            }

            PlayRandomVoiceLine();
        }

        // ¿El segmento ha terminado?
        if (voiceAudioSource.isPlaying && voiceAudioSource.time >= currentVoiceEndTime)
        {
            Debug.Log($"[Voz] Segmento finalizado en el segundo {voiceAudioSource.time}. Deteniendo audio.");
            voiceAudioSource.Stop();
        }
    }

    private void PlayRandomVoiceLine()
    {
        int randomIndex = Random.Range(0, voiceSegments.Length);
        Vector2 segment = voiceSegments[randomIndex];

        // COMPROBACIÓN CRÍTICA: ¿El audio es más corto que el segmento?
        if (segment.x >= russianVoicelinesClip.length)
        {
            Debug.LogError($"[Voz] ERROR: El segmento intenta empezar en {segment.x}s, pero el audio solo dura {russianVoicelinesClip.length}s.");
            ScheduleNextVoiceLine();
            return;
        }

        Debug.Log($"[Voz] Hablando... Clip: {russianVoicelinesClip.name} | Segmento: {segment.x}s a {segment.y}s");

        voiceAudioSource.clip = russianVoicelinesClip;
        voiceAudioSource.time = segment.x;
        currentVoiceEndTime = segment.y;

        voiceAudioSource.Play();
        ScheduleNextVoiceLine();
    }

    private void UpdateWeaponAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            if (audioSource.time >= 3f)
            {
                audioSource.time = 0f;
            }

            bool shouldStop = Time.time > lastFireTime + 0.5f;
            if (brain != null && (brain.CurrentState == SoldierState.Reloading || brain.CurrentState == SoldierState.Dead))
            {
                shouldStop = true;
            }

            if (shouldStop)
            {
                audioSource.Stop();
            }
        }
    }

    public void RotateTowards(Transform target)
    {
        if (target == null || body == null) return;

        Vector3 direction = (target.position - body.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            body.rotation = Quaternion.Slerp(body.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private string currentAnimState = "";

    public void SetAnimation(string stateName)
    {
        // Si el estado es Walk, usamos la animación de Run
        if (stateName == "Walk")
        {
            stateName = "Run";
        }

        if (anim != null && currentAnimState != stateName)
        {
            anim.CrossFadeInFixedTime(stateName, 0.1f);
            currentAnimState = stateName;
        }
    }

    public void Fire(Transform target)
    {
        if (brain != null && brain.CurrentState == SoldierState.Reloading) return;

        lastFireTime = Time.time;
        if (audioSource != null && ak47Clip != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = ak47Clip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        if (bulletPrefab == null || shootPoint == null) return;

        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, shootPoint.position, shootPoint.rotation);
            muzzleFlash.transform.SetParent(shootPoint);
            Destroy(muzzleFlash, 0.05f);
        }

        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetShooterFaction(GetComponentInParent<FactionIdentity>());
        }

        if (bulletObj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 direction = shootPoint.forward;
            if (target != null)
            {
                Collider targetCollider = target.GetComponentInChildren<Collider>();
                Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 1f;
                direction = (targetCenter - shootPoint.position).normalized;
            }

            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            float randomZ = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, randomZ);
            Vector3 finalDirection = spreadRotation * direction;

            rb.AddForce(finalDirection * bulletForce);
        }
    }
}