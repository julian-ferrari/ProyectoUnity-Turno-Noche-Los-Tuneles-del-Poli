using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ==================== AUDIO ZONE MANAGER ====================
// Este script va en un GameObject vacío en la escena (solo uno)
public class AudioZoneManager : MonoBehaviour
{
    public static AudioZoneManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource ambienceSource1;
    public AudioSource ambienceSource2;

    [Header("Configuración")]
    public float fadeDuration = 2f;
    public bool debugMode = false;

    private AudioZone currentZone;
    private Coroutine musicFadeCoroutine;
    private Coroutine ambience1FadeCoroutine;
    private Coroutine ambience2FadeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        // Crear AudioSources si no existen
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f; // 2D
            musicSource.playOnAwake = false;
        }

        if (ambienceSource1 == null)
        {
            GameObject amb1Obj = new GameObject("AmbienceSource1");
            amb1Obj.transform.SetParent(transform);
            ambienceSource1 = amb1Obj.AddComponent<AudioSource>();
            ambienceSource1.loop = true;
            ambienceSource1.spatialBlend = 0f;
            ambienceSource1.playOnAwake = false;
        }

        if (ambienceSource2 == null)
        {
            GameObject amb2Obj = new GameObject("AmbienceSource2");
            amb2Obj.transform.SetParent(transform);
            ambienceSource2 = amb2Obj.AddComponent<AudioSource>();
            ambienceSource2.loop = true;
            ambienceSource2.spatialBlend = 0f;
            ambienceSource2.playOnAwake = false;
        }

        Debug.Log("AudioZoneManager inicializado con 3 AudioSources");
    }

    public void EnterZone(AudioZone zone)
    {
        if (currentZone == zone) return;

        if (debugMode)
            Debug.Log($"<color=cyan>Entrando a zona: {zone.zoneName}</color>");

        currentZone = zone;
        TransitionToZone(zone);
    }

    public void ExitZone(AudioZone zone)
    {
        if (currentZone == zone)
        {
            if (debugMode)
                Debug.Log($"<color=yellow>Saliendo de zona: {zone.zoneName}</color>");

            currentZone = null;
            StopAllAudio();
        }
    }

    void TransitionToZone(AudioZone zone)
    {
        // Música
        if (zone.music != null)
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(CrossfadeAudio(musicSource, zone.music, zone.musicVolume));
        }
        else
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutAudio(musicSource));
        }

        // Ambiente 1
        if (zone.ambience1 != null)
        {
            if (ambience1FadeCoroutine != null) StopCoroutine(ambience1FadeCoroutine);
            ambience1FadeCoroutine = StartCoroutine(CrossfadeAudio(ambienceSource1, zone.ambience1, zone.ambience1Volume));
        }
        else
        {
            if (ambience1FadeCoroutine != null) StopCoroutine(ambience1FadeCoroutine);
            ambience1FadeCoroutine = StartCoroutine(FadeOutAudio(ambienceSource1));
        }

        // Ambiente 2
        if (zone.ambience2 != null)
        {
            if (ambience2FadeCoroutine != null) StopCoroutine(ambience2FadeCoroutine);
            ambience2FadeCoroutine = StartCoroutine(CrossfadeAudio(ambienceSource2, zone.ambience2, zone.ambience2Volume));
        }
        else
        {
            if (ambience2FadeCoroutine != null) StopCoroutine(ambience2FadeCoroutine);
            ambience2FadeCoroutine = StartCoroutine(FadeOutAudio(ambienceSource2));
        }
    }

    IEnumerator CrossfadeAudio(AudioSource source, AudioClip newClip, float targetVolume)
    {
        // Si ya está reproduciendo el mismo clip, solo ajustar volumen
        if (source.clip == newClip && source.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(source, targetVolume));
            yield break;
        }

        // Fade out del audio actual
        if (source.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(source, 0f));
            source.Stop();
        }

        // Cambiar clip y fade in
        source.clip = newClip;
        source.volume = 0f;
        source.Play();
        yield return StartCoroutine(FadeVolume(source, targetVolume));
    }

    IEnumerator FadeOutAudio(AudioSource source)
    {
        if (!source.isPlaying) yield break;

        yield return StartCoroutine(FadeVolume(source, 0f));
        source.Stop();
    }

    IEnumerator FadeVolume(AudioSource source, float targetVolume)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    void StopAllAudio()
    {
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        if (ambience1FadeCoroutine != null) StopCoroutine(ambience1FadeCoroutine);
        if (ambience2FadeCoroutine != null) StopCoroutine(ambience2FadeCoroutine);

        StartCoroutine(FadeOutAudio(musicSource));
        StartCoroutine(FadeOutAudio(ambienceSource1));
        StartCoroutine(FadeOutAudio(ambienceSource2));
    }
}