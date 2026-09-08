using UnityEngine;
using System.Collections;
using System;

public class PlayerMovement : MonoBehaviour, IDamageable
{
    [Header("Zıplama Collider Ayarları")]
    public Vector2 jumpColliderSize = new Vector2(1f, 1f);
    public Vector2 jumpColliderOffset = new Vector2(0f, 0.5f);
    [Header("Eğilme ve Collider Ayarları")]
    public Vector2 crouchColliderSize = new Vector2(1f, 1f);   // Eğilince Collider'ın yeni boyutu
    public Vector2 crouchColliderOffset = new Vector2(0f, 0.5f); // Eğilince Collider'ın yeni merkezi
    
    private CapsuleCollider2D _collider; // (Eğer CapsuleCollider2D kullanıyorsan adını değiştir)
    private Vector2 _standColliderSize;
    private Vector2 _standColliderOffset;
    [Header("Hareket Ayarları")]
    public float moveSpeed = 20f;
    public float attackSpeedMultiplier = 0.25f; 

    [Header("Zıplama Ayarları")]
    public float jumpForce = 15f;
    public int maxJumps = 2; 
    private int _jumpCount;  

    [Header("Bileşenler")]
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;
    private PlayerCombat _combat;
    
    private EntityState _currentState = EntityState.Normal;
    private float _horizontalInput;
    private bool _isGrounded;

    // Zemin Kontrolü
    public Transform groundCheck;
    public float checkRadius = 0.5f;
    public LayerMask groundLayer;

    public event Action<int> OnHealthChanged;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
        _combat = GetComponent<PlayerCombat>();
        // YENİ: Collider'ı bul ve normal boyunu kaydet
        _collider = GetComponent<CapsuleCollider2D>();
        if (_collider != null)
        {
            _standColliderSize = _collider.size;
            _standColliderOffset = _collider.offset;
        }

        _combat.OnAttackStarted += () => _currentState = EntityState.Attacking;
        _combat.OnAttackEnded += () => _currentState = EntityState.Normal;
    }

    void Start()
    {
        _currentState = EntityState.Normal;
    }

    void Update()
    {
        if (_currentState == EntityState.Dead) return;

        GetInputs(); 
        HandleFlip();
        UpdateAnimations();
    }

    private void GetInputs()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical"); // S tuşu veya Aşağı ok
        
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (_isGrounded && _rb.linearVelocity.y <= 0.1f)
        {
            _jumpCount = 0;
        }

        // YENİ: EĞİLME (CROUCH) MANTIĞI
        // YENİ: EĞİLME (CROUCH) MANTIĞI
        if (_isGrounded && _currentState != EntityState.Attacking)
        {
            if (verticalInput < -0.1f || Input.GetKey(KeyCode.DownArrow))
            {
                if (_currentState != EntityState.Crouching)
                {
                    _currentState = EntityState.Crouching;
                    SetColliderHeight(true); // Collider'ı KÜÇÜLT
                }
            }
            else if (_currentState == EntityState.Crouching) 
            {
                _currentState = EntityState.Normal;
                SetColliderHeight(false); // Collider'ı BÜYÜT
            }
        }

        // ZIPLAMA KOMUTU
        // Eğilirken zıplamayı kilitliyoruz ki mantık hatası olmasın
        if (Input.GetButtonDown("Jump") && _currentState != EntityState.Crouching)
        {
            if (_isGrounded || _jumpCount < maxJumps)
            {
                PerformJump();
            }
        }
        // Zıplama sayacını SIFIRLAMA MANTIĞI
        if (_isGrounded && _rb.linearVelocity.y <= 0.1f)
        {
            _jumpCount = 0;
            
            // SİGORTA: Yere değdiğimizde eğilmiyorsak collider'ı KESİN olarak normale döndür
            if (_currentState != EntityState.Crouching)
            {
                DisableJumpCollider();
            }
        }
    }

    private void PerformJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        _jumpCount++; 
    }

    void FixedUpdate()
    {
        if (_currentState == EntityState.Dead) return;

        float currentSpeed = _horizontalInput * moveSpeed;

        // YENİ: HIZ KONTROLLERİ
        if (_currentState == EntityState.Attacking) 
        {
            currentSpeed *= attackSpeedMultiplier;
        }
        else if (_currentState == EntityState.Crouching)
        {
            currentSpeed = 0f; // Eğilirken hız KESİNLİKLE sıfır olur
        }

        _rb.linearVelocity = new Vector2(currentSpeed, _rb.linearVelocity.y);
    }

    private void HandleFlip()
    {
        // Eğilirken sağa sola dönmesini istemiyorsan buraya if (_currentState == EntityState.Crouching) return; ekleyebilirsin
        if (_horizontalInput > 0.1f) transform.localScale = new Vector3(5, 5, 5);
        else if (_horizontalInput < -0.1f) transform.localScale = new Vector3(-5, 5, 5);
    }

    private void UpdateAnimations()
    {
        _anim.SetBool("isRunning", Mathf.Abs(_horizontalInput) > 0.1f && _isGrounded);
        _anim.SetBool("isJumping", !_isGrounded);
        
        // YENİ: Animator'a eğilme durumunu gönder
        _anim.SetBool("isCrouching", _currentState == EntityState.Crouching);
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        _rb.AddForce(knockback, ForceMode2D.Impulse);
        StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        _sr.color = Color.white;
    }
    private void SetColliderHeight(bool isCrouching)
    {
        if (_collider == null) return;

        if (isCrouching)
        {
            _collider.size = crouchColliderSize;
            _collider.offset = crouchColliderOffset;
        }
        else
        {
            _collider.size = _standColliderSize;
            _collider.offset = _standColliderOffset;
        }
        
    }
    // Animasyonun 2. yarısında bacakları çektiğinde çağrılacak
    public void EnableJumpCollider()
    {
        if (_collider != null && _currentState != EntityState.Crouching)
        {
            _collider.size = jumpColliderSize;
            _collider.offset = jumpColliderOffset;
        }
    }

    // Yere inmeye yaklaştığında veya ayaklarını uzattığında çağrılacak
    public void DisableJumpCollider()
    {
        // Eğer o sırada havada eğilme tuşuna basılı tutmuyorsa normale döndür
        if (_collider != null && _currentState != EntityState.Crouching)
        {
            _collider.size = _standColliderSize;
            _collider.offset = _standColliderOffset;
        }
    }
}