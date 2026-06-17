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
    Debug.Log("ChangeBGM 被调用");

    if (newClip == null)
    {
        Debug.LogError("newClip 是空的，无法播放 BGM");
        return;
    }

    if (audioSource == null)
    {
        Debug.LogError("BGMManager 没有 AudioSource");
        return;
    }

    if (currentClip == newClip && audioSource.isPlaying)
    {
        Debug.Log("当前 BGM 已经在播放，不重复播放");
        return;
    }

    Debug.Log("开始播放 BGM：" + newClip.name);

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