using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Audio files")]
    public AudioClip backgroundMusic;
    public AudioClip chaseMusic;

    [Header("Volume (0 = silent, 1 = full)")]
    [Range(0f, 1f)] public float backgroundVolume = 0.25f;
    [Range(0f, 1f)] public float chaseVolume = 0.35f;

    [Header("Crossfade")]
    public float fadeTime = 1.5f;   // seconds to fade between the two tracks

    AudioSource backgroundSource;
    AudioSource chaseSource;
    bool wasChasing;

    void Awake()
    {
        backgroundSource = CreateSource(backgroundMusic);
        chaseSource = CreateSource(chaseMusic);
    }

    void Start()
    {
        if (backgroundMusic == null || chaseMusic == null)
        {
            Debug.LogWarning("MusicManager is missing an audio file");
        }

        backgroundSource.Play();
    }

    AudioSource CreateSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // plain 2D sound, same volume everywhere
        source.volume = 0f;
        return source;
    }

    void Update()
    {
        bool isChased = IsAnyEnemyChasing();

        // Start the chase music from the beginning each time a chase starts
        if (isChased && !wasChasing)
        {
            chaseSource.Stop();
            chaseSource.Play();
        }

        wasChasing = isChased;

        // Fade each track toward its target volume (unscaled time, so it still works when the game is paused)
        float step = Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f);

        float backgroundTarget = isChased ? 0f : backgroundVolume;
        float chaseTarget = isChased ? chaseVolume : 0f;

        backgroundSource.volume = Mathf.MoveTowards(backgroundSource.volume, backgroundTarget, step);
        chaseSource.volume = Mathf.MoveTowards(chaseSource.volume, chaseTarget, step);

        // Chase music has fully faded out: stop it so it restarts fresh next time
        if (!isChased && chaseSource.volume <= 0f && chaseSource.isPlaying)
        {
            chaseSource.Stop();
        }
    }

    bool IsAnyEnemyChasing()
    {
        foreach (EnemyMovement enemy in EnemyMovement.allEnemies)
        {
            if (enemy.IsChasing())
            {
                return true;
            }
        }

        return false;
    }
}