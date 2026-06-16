using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Footsteps")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip[] snowFootsteps;
    [SerializeField] private float footstepMovementThreshold = 0.1f;
    [SerializeField] private float footstepStopGraceTime = 0.08f;

    [Header("Actions")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip checkpointClip;
    [SerializeField] private AudioClip landingClip;

    private Collision coll;
    private Rigidbody2D rb;
    private Movement movement;
    private float footstepStopGraceTimer;
    private bool footstepsActive;

    private void Awake()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        if (footstepAudioSource == null)
        {
            AudioSource[] audioSources = GetComponents<AudioSource>();
            foreach (AudioSource source in audioSources)
            {
                if (source != null && source != audioSource)
                {
                    footstepAudioSource = source;
                    break;
                }
            }
        }

        if (footstepAudioSource == null || footstepAudioSource == audioSource)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = true;
            footstepAudioSource.clip = GetFootstepClip();
            footstepAudioSource.spatialBlend = 0f;
            footstepAudioSource.volume = audioSource != null ? audioSource.volume : 1f;
            footstepAudioSource.pitch = 1f;
        }
    }

    private void Update()
    {
        bool shouldPlayFootsteps = ShouldPlayFootsteps();
        if (shouldPlayFootsteps)
        {
            footstepStopGraceTimer = footstepStopGraceTime;

            if (!footstepsActive)
            {
                footstepsActive = true;
                StartFootstepLoop();
            }
        }
        else
        {
            UpdateFootstepStopGrace();
        }
    }

    public void PlayJump()
    {
        PlayClip(jumpClip);
    }

    public void PlayDash()
    {
        PlayClip(dashClip);
    }

    public void PlayDeath()
    {
        PlayClip(deathClip);
    }

    public void PlayCheckpoint()
    {
        PlayClip(checkpointClip);
    }

    public void PlayLanding()
    {
        PlayClip(landingClip);
    }

    public void ResetFootstepState()
    {
        footstepStopGraceTimer = 0f;
        footstepsActive = false;
        StopFootstepLoop();
    }

    private bool ShouldPlayFootsteps()
    {
        if (audioSource == null || coll == null || rb == null)
        {
            return false;
        }

        if (!coll.onGround || Mathf.Abs(rb.velocity.x) < footstepMovementThreshold)
        {
            return false;
        }

        if (Mathf.Abs(rb.velocity.y) > 0.05f)
        {
            return false;
        }

        if (movement != null && (movement.isDashing || movement.wallGrab || movement.wallSlide))
        {
            return false;
        }

        return true;
    }

    private void StartFootstepLoop()
    {
        if (footstepAudioSource == null)
        {
            return;
        }

        AudioClip clip = GetFootstepClip();
        if (clip == null)
        {
            return;
        }

        if (footstepAudioSource.clip != clip)
        {
            footstepAudioSource.clip = clip;
        }

        if (!footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Play();
        }
    }

    private void StopFootstepLoop()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private AudioClip GetFootstepClip()
    {
        if (footstepClip != null)
        {
            return footstepClip;
        }

        if (snowFootsteps != null && snowFootsteps.Length > 0)
        {
            return snowFootsteps[0];
        }

        return null;
    }

    private void UpdateFootstepStopGrace()
    {
        if (!footstepsActive)
        {
            return;
        }

        footstepStopGraceTimer -= Time.deltaTime;
        if (footstepStopGraceTimer <= 0f)
        {
            ResetFootstepState();
        }
    }

    private void OnDisable()
    {
        ResetFootstepState();
    }
}
