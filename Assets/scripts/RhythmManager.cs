using UnityEngine;
using System;

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; } // Singleton Pattern

    [Header("Müzik Ayarları")]
    public AudioSource musicSource;
    public float bpm = 120f;
    
    // Gelişmiş C# Delegate/Event kullanımı
    public event Action OnBeatEvent; 

    public float LastBeatTime { get; private set; }
    private float _secPerBeat;
    private float _dspSongTime;
    private int _lastCompletedBeat = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _secPerBeat = 60f / bpm;
        _dspSongTime = (float)AudioSettings.dspTime;
        
        if (musicSource != null) musicSource.Play();
    }

    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        float songPosition = (float)(AudioSettings.dspTime - _dspSongTime);
        float songPositionInBeats = songPosition / _secPerBeat;

        if ((int)songPositionInBeats > _lastCompletedBeat)
        {
            _lastCompletedBeat = (int)songPositionInBeats;
            LastBeatTime = (float)AudioSettings.dspTime;
            
            OnBeatEvent?.Invoke(); // Event'e abone olan herkese haber ver
        }
    }

    public bool IsPerfectHit(float window)
    {
        float timeSinceLastBeat = (float)AudioSettings.dspTime - LastBeatTime;
        float beatInterval = 60f / bpm;
        return timeSinceLastBeat <= window || timeSinceLastBeat >= beatInterval - window;
    }
}