using UnityEngine;
using UnityEngine.Rendering.Universal; 

public class BeatLight : MonoBehaviour
{
    public float maxIntensity = 1.5f;
    public float minIntensity = 0.2f;
    public float fadeSpeed = 8f;

    private Light2D _light;

    void Awake() => _light = GetComponent<Light2D>();

    void Start()
    {
        // RhythmManager eventine kod üzerinden abone oluyoruz
        if (RhythmManager.Instance != null)
            RhythmManager.Instance.OnBeatEvent += Pulse;
    }

    void OnDestroy()
    {
        if (RhythmManager.Instance != null)
            RhythmManager.Instance.OnBeatEvent -= Pulse;
    }

    void Update()
    {
        if (_light != null)
            _light.intensity = Mathf.Lerp(_light.intensity, minIntensity, Time.deltaTime * fadeSpeed);
    }

    private void Pulse() // Private yaptık çünkü Event üzerinden tetikleniyor
    {
        if (_light != null) _light.intensity = maxIntensity;
    }
}