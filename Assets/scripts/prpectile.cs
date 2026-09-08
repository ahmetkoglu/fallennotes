using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 10;
    public float lifeTime = 3f;
    
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifeTime);}
    

    public void SetDirection(float dir)
    {
        _rb.linearVelocity = new Vector2(dir * speed, 0);
        float visualScale = (dir > 0) ? 1f : -1f;
        transform.localScale = new Vector3(visualScale * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // TryGetComponent (C# 7 Pattern Matching): Hem performansı artırır hem kod kirliliğini önler
        if (hitInfo.TryGetComponent(out IDamageable damageableTarget))
        {
            Vector2 knockbackDir = _rb.linearVelocity.normalized * 2f;
            damageableTarget.TakeDamage(damage, knockbackDir);
            Destroy(gameObject);
        }
        else if (hitInfo.CompareTag("Ground")) 
        {
            Destroy(gameObject);
        }
    }
}