using UnityEngine;

/// <summary>
/// Componente para reproducir sonidos que deben quedar grabados en el replay.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ReplayAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private ReplayObject replayObject;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        replayObject = GetComponent<ReplayObject>();
    }

    public void PlayRecordedSound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null) return;

        float volume = audioSource.volume * volumeScale;
        audioSource.PlayOneShot(clip, volumeScale);

        if (replayObject != null)
        {
            replayObject.RegisterAudioEvent(clip.name, volume, audioSource.pitch);
        }
    }

    public void PlayRecordedSoundAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, volumeScale);

        if (replayObject != null)
        {
            replayObject.RegisterAudioEvent(clip.name, volumeScale, 1f);
        }
    }
}
