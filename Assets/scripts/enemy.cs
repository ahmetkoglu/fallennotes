using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Sağlık Ayarları")]
    public int health = 100;
    
    [Header("Hareket (Devriye) Ayarları")]
    public float walkSpeed = 2f;
    public float patrolDistance = 5f;

    [Header("Saldırı ve Görüş Ayarları")]
    public float detectRange = 8f;        // Oyuncuyu görme mesafesi
    public float shootCooldown = 2f;      // İki ateş arasındaki bekleme süresi
    public LayerMask playerLayer;         // Sadece oyuncuyu görmesi için
    public GameObject projectilePrefab;   // Fırlatılacak mermi
    public Transform firePoint;           // Merminin çıkış noktası

    public event Action<int> OnHealthChanged;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _anim;
    private EntityState _currentState = EntityState.Moving;
    private Color _originalColor;
    
    private Vector2 _startPos;
    private bool _movingRight = true;
    private float _lastShootTime;
    private Transform _playerTarget;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
        _originalColor = _sr.color;
    }

    void Start()
    {
        _startPos = transform.position;
        _currentState = EntityState.Moving; // Oyuna devriye atarak başlar
    }

    void Update()
    {
        if (_currentState == EntityState.Dead) return;

        CheckLineOfSight();

        if (_currentState == EntityState.Moving)
        {
            HandlePatrol();
        }
        else if (_currentState == EntityState.Attacking)
        {
            HandleShooting();
        }
    }

    // GÖRÜŞ SİSTEMİ (Raycast)
    private void CheckLineOfSight()
    {
        // Karakterin baktığı yöne doğru bir lazer çizer
        Vector2 sightDirection = _movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, sightDirection, detectRange, playerLayer);

        if (hit.collider != null)
        {
            // Oyuncuyu gördü! Duruma geç.
            _playerTarget = hit.collider.transform;
            _currentState = EntityState.Attacking;
        }
        else if (_playerTarget == null) // Menzilden çıktıysa devriyeye dön
        {
            _currentState = EntityState.Moving;
        }
    }

    // DEVRİYE SİSTEMİ
    private void HandlePatrol()
    {
        _anim.SetBool("isWalking", true);

        if (_movingRight)
        {
            _rb.linearVelocity = new Vector2(walkSpeed, _rb.linearVelocity.y);
            SetFacingDirection(true); // Görseli ve mantığı kilitle
            
            // Sınıra ulaştıysa ANINDA dön
            if (transform.position.x >= _startPos.x + patrolDistance) 
                SetFacingDirection(false); 
        }
        else
        {
            _rb.linearVelocity = new Vector2(-walkSpeed, _rb.linearVelocity.y);
            SetFacingDirection(false); // Görseli ve mantığı kilitle
            
            // Sınıra ulaştıysa ANINDA dön
            if (transform.position.x <= _startPos.x - patrolDistance) 
                SetFacingDirection(true);
        }
    }

    // ATEŞ ETME SİSTEMİ
    // ATEŞ ETME SİSTEMİ
    private void HandleShooting()
    {
        // Ateş ederken dur
        _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        _anim.SetBool("isWalking", false);

        if (_playerTarget != null)
        {
            // Menzilden çıkma kontrolü
            if (Vector2.Distance(transform.position, _playerTarget.position) > detectRange)
            {
                _playerTarget = null;
                _currentState = EntityState.Moving;
                return;
            }

            // ÇÖZÜM: Yüzünü her zaman kesin olarak oyuncuya dön
            bool shouldFaceRight = _playerTarget.position.x > transform.position.x;
            if (_movingRight != shouldFaceRight)
            {
                SetFacingDirection(shouldFaceRight);
            }
        }

        // Ateş Etme Cooldown
        if (Time.time >= _lastShootTime + shootCooldown)
        {
            _lastShootTime = Time.time;
            _anim.SetTrigger("shootTrigger");
        }
    }

    // BU FONKSİYONU ANIMATOR EVENT İLE ÇAĞIR!
    public void FireProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            if (proj.TryGetComponent(out Projectile projScript))
            {
                float dir = _movingRight ? 1f : -1f;
                projScript.SetDirection(dir); // Projectile.cs içindeki fonksiyonu çalıştır
            }
        }
    }

    // HASAR ALMA VE ÖLÜM (IDamageable)
    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (_currentState == EntityState.Dead) return;
        
        health -= damage;
        OnHealthChanged?.Invoke(health);
        
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(knockback, ForceMode2D.Impulse);
        
        StartCoroutine(DamageFlashRoutine());
        
        if (health <= 0) Die();
    }

    private IEnumerator DamageFlashRoutine()
    {
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _sr.color = _originalColor;
    }

    private void Die()
    {
        _currentState = EntityState.Dead;
        _anim.SetTrigger("dieTrigger"); // Ölüm animasyonu varsa tetikle
        GetComponent<Collider2D>().enabled = false;
        _rb.simulated = false;
        Destroy(gameObject, 2f);
    }

    // Geliştirici için Editör Çizgileri (Görüş mesafesini gösterir)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 sightDirection = _movingRight ? Vector2.right : Vector2.left;
        Gizmos.DrawRay(transform.position, sightDirection * detectRange);
    }
    // YENİ: Görsel ve mantığı aynı anda çeviren kilit fonksiyonu
    private void SetFacingDirection(bool lookRight)
    {
        _movingRight = lookRight;
        float xPath = lookRight ? 1f : -1f;
        transform.localScale = new Vector3(xPath * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }
}