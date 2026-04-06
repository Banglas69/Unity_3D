using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundId
{
    Walk,
    Sprint,
    Jump,
    WallRun,
    Dash,
    WallJump,
    Shoot,
    Melee,
    EnemyAttack,
    EnemyChase,
    EnemyDeath,
    DoorOpen,
    DoorClose
}

[Serializable]
public class SoundDefinition
{
    public SoundId id;
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 pitchRange = new Vector2(1f, 1f);
    public bool spatial3D = false;
    public float minDistance = 1f;
    public float maxDistance = 20f;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Sound Library")]
    public SoundDefinition[] sounds;

    [Header("Player Sources")]
    public AudioSource playerOneShotSource;
    public AudioSource playerLoopSource;

    private readonly Dictionary<SoundId, SoundDefinition> soundMap = new Dictionary<SoundId, SoundDefinition>();
    private readonly Dictionary<string, AudioSource> worldLoopSources = new Dictionary<string, AudioSource>();

    private bool hasPlayerLoop;
    private SoundId currentPlayerLoopId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSoundMap();
        EnsurePlayerSources();
    }

    private void BuildSoundMap()
    {
        soundMap.Clear();

        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i] == null)
                continue;

            soundMap[sounds[i].id] = sounds[i];
        }
    }

    private void EnsurePlayerSources()
    {
        if (playerOneShotSource == null)
        {
            GameObject go = new GameObject("PlayerOneShotAudio");
            go.transform.SetParent(transform);
            playerOneShotSource = go.AddComponent<AudioSource>();
        }

        if (playerLoopSource == null)
        {
            GameObject go = new GameObject("PlayerLoopAudio");
            go.transform.SetParent(transform);
            playerLoopSource = go.AddComponent<AudioSource>();
        }

        ConfigureAs2D(playerOneShotSource);
        ConfigureAs2D(playerLoopSource);
    }

    private void ConfigureAs2D(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }

    private void ConfigureAs3D(AudioSource source, SoundDefinition def)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = def.spatial3D ? 1f : 0f;
        source.dopplerLevel = 0f;
        source.minDistance = def.minDistance;
        source.maxDistance = def.maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
    }

    private SoundDefinition GetDefinition(SoundId id)
    {
        soundMap.TryGetValue(id, out SoundDefinition def);
        return def;
    }

    private AudioClip PickClip(SoundDefinition def)
    {
        if (def == null || def.clips == null || def.clips.Length == 0)
            return null;

        if (def.clips.Length == 1)
            return def.clips[0];

        return def.clips[UnityEngine.Random.Range(0, def.clips.Length)];
    }

    private float PickPitch(SoundDefinition def)
    {
        if (def == null)
            return 1f;

        return UnityEngine.Random.Range(def.pitchRange.x, def.pitchRange.y);
    }

    public void PlayOneShot2D(SoundId id, float volumeScale = 1f)
    {
        SoundDefinition def = GetDefinition(id);
        AudioClip clip = PickClip(def);

        if (def == null || clip == null || playerOneShotSource == null)
            return;

        playerOneShotSource.pitch = PickPitch(def);
        playerOneShotSource.PlayOneShot(clip, def.volume * volumeScale);
    }

    public void StartLoop2D(SoundId id)
    {
        SoundDefinition def = GetDefinition(id);
        AudioClip clip = PickClip(def);

        if (def == null || clip == null || playerLoopSource == null)
            return;

        if (hasPlayerLoop && currentPlayerLoopId == id && playerLoopSource.isPlaying)
            return;

        playerLoopSource.Stop();
        playerLoopSource.clip = clip;
        playerLoopSource.volume = def.volume;
        playerLoopSource.pitch = PickPitch(def);
        playerLoopSource.loop = true;
        playerLoopSource.spatialBlend = 0f;
        playerLoopSource.Play();

        currentPlayerLoopId = id;
        hasPlayerLoop = true;
    }

    public void StopLoop2D(SoundId id)
    {
        if (!hasPlayerLoop || currentPlayerLoopId != id || playerLoopSource == null)
            return;

        playerLoopSource.Stop();
        playerLoopSource.clip = null;
        hasPlayerLoop = false;
    }

    public void StopAllPlayerLoops()
    {
        if (playerLoopSource == null)
            return;

        playerLoopSource.Stop();
        playerLoopSource.clip = null;
        hasPlayerLoop = false;
    }

    public void PlayOneShot3D(SoundId id, Vector3 position, Transform follow = null, float volumeScale = 1f)
    {
        SoundDefinition def = GetDefinition(id);
        AudioClip clip = PickClip(def);

        if (def == null || clip == null)
            return;

        GameObject go = new GameObject($"OneShot3D_{id}");
        go.transform.position = position;

        if (follow != null)
            go.transform.SetParent(follow);

        AudioSource source = go.AddComponent<AudioSource>();
        ConfigureAs3D(source, def);

        source.clip = clip;
        source.volume = def.volume * volumeScale;
        source.pitch = PickPitch(def);
        source.loop = false;
        source.Play();

        float destroyDelay = clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        Destroy(go, destroyDelay + 0.1f);
    }

    public void StartLoop3D(SoundId id, Transform target)
    {
        if (target == null)
            return;

        SoundDefinition def = GetDefinition(id);
        AudioClip clip = PickClip(def);

        if (def == null || clip == null)
            return;

        string key = GetLoopKey(id, target);

        if (worldLoopSources.TryGetValue(key, out AudioSource existing) && existing != null)
        {
            if (!existing.isPlaying)
                existing.Play();
            return;
        }

        GameObject go = new GameObject($"Loop3D_{id}_{target.name}");
        go.transform.SetParent(target);
        go.transform.localPosition = Vector3.zero;

        AudioSource source = go.AddComponent<AudioSource>();
        ConfigureAs3D(source, def);

        source.clip = clip;
        source.volume = def.volume;
        source.pitch = PickPitch(def);
        source.loop = true;
        source.Play();

        worldLoopSources[key] = source;
    }

    public void StopLoop3D(SoundId id, Transform target)
    {
        if (target == null)
            return;

        string key = GetLoopKey(id, target);

        if (!worldLoopSources.TryGetValue(key, out AudioSource source))
            return;

        worldLoopSources.Remove(key);

        if (source != null)
            Destroy(source.gameObject);
    }

    private string GetLoopKey(SoundId id, Transform target)
    {
        return $"{id}_{target.GetInstanceID()}";
    }
}