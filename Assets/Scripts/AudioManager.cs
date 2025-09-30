using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Ambient Sounds")]
    public AudioClip dogs;
    public AudioClip crickets;

    [Header("Player Sounds")]
    public AudioClip breathingFast;
    public AudioClip footsteps;

    private AudioSource musicSource;
    private AudioSource ambientSource;
    private AudioSource sfxSource;

    private static AudioManager instance;

    void Awake()
    {
        // Singleton para que solo exista un AudioManager
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear fuentes de audio
        musicSource = gameObject.AddComponent<AudioSource>();
        ambientSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Configuración de loop
        musicSource.loop = true;
        ambientSource.loop = true;
    }

    void Start()
    {
        // Música de fondo
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = 0.5f;
            musicSource.Play();
        }

        // Sonido ambiental (ej: grillos)
        if (crickets != null)
        {
            ambientSource.clip = crickets;
            ambientSource.volume = 0.3f;
            ambientSource.Play();
        }

        // Perros ladrando cada cierto tiempo
        if (dogs != null)
        {
            InvokeRepeating(nameof(PlayDogs), 10f, Random.Range(15f, 30f));
        }
    }

    // --- Métodos públicos para reproducir sonidos del jugador ---
    public void PlayBreathing()
    {
        if (!sfxSource.isPlaying || sfxSource.clip != breathingFast)
        {
            sfxSource.clip = breathingFast;
            sfxSource.loop = true;
            sfxSource.Play();
        }
    }

    public void StopBreathing()
    {
        if (sfxSource.isPlaying && sfxSource.clip == breathingFast)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
        }
    }

    public void PlayFootstep()
    {
        if (footsteps != null)
        {
            sfxSource.PlayOneShot(footsteps, 1f);
        }
    }

    // --- Sonidos ambientales ---
    private void PlayDogs()
    {
        if (dogs != null)
            ambientSource.PlayOneShot(dogs, 0.7f);
    }

    // --- Acceso global ---
    public static AudioManager GetInstance()
    {
        return instance;
    }
}
