using UnityEngine;

public class BeatNode : MonoBehaviour
{
    public float moveSpeed;
    private RectTransform _rect;

    void Awake() => _rect = GetComponent<RectTransform>();

    void Update()
    {
        _rect.anchoredPosition += Vector2.left * moveSpeed * Time.deltaTime;

        if (_rect.anchoredPosition.x < -600f)
        {
            Destroy(gameObject);
    }
    }
}