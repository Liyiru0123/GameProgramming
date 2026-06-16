using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public float defaultVolume = 0.3f;

    private AudioClip currentClip;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void ChangeBGM(AudioClip newClip)
    {
        if (newClip == null) return;

        if (currentClip == newClip) return;

        currentClip = newClip;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeToNewBGM(newClip));
    }

    private IEnumerator FadeToNewBGM(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, defaultVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = defaultVolume;
    }
}