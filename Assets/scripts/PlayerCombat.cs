using UnityEngine;
using System;
using System.Collections;
using DG.Tweening; // YENİ: DOTween kütüphanesi eklendi

public class PlayerCombat : MonoBehaviour
{
    [Header("Saldırı Değerleri")]
    public float attackDistance = 3f;
    public int damageAmount = 20;
    public LayerMask enemyLayer;
    public float attackHeightOffset = 1.5f;
    
    [Header("Kombo & Ritim")]
    public float perfectWindow = 0.15f;
    public float comboResetTime = 0.8f; 
    public int comboStep = 0;

    [Header("Görsel Efektler")]
    public SpriteRenderer perfectHitEffect; 
    public float effectDuration = 0.15f;    

    public event Action OnAttackStarted;
    public event Action OnAttackEnded;

    private Animator _anim;
    private bool _wasPerfect;
    private float _lastAttackTime;
    
    // YENİ: Efektin orijinal boyutunu hafızada tutacağız
    private Vector3 _effectOriginalScale;

    void Awake() 
    {
        _anim = GetComponent<Animator>();
        
        if (perfectHitEffect != null) 
        {
            // Başlangıçta orijinal boyutu kaydet, görünmez yap ve boyutunu sıfırla
            _effectOriginalScale = perfectHitEffect.transform.localScale;
            perfectHitEffect.transform.localScale = Vector3.zero;
            perfectHitEffect.enabled = false;
        }
    }

    void Update()
    {
        if (Time.time - _lastAttackTime > comboResetTime && comboStep != 0)
        {
            comboStep = 0;
            _anim.SetInteger("comboStep", 0);
        }

        if (Input.GetButtonDown("Fire1")) AttemptAttack();
    }

    private void AttemptAttack()
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag("Attack") && stateInfo.normalizedTime < 0.8f) return;

        // Ritim Kontrolü
        _wasPerfect = RhythmManager.Instance.IsPerfectHit(perfectWindow);
        
        // YENİ: Perfect ise DOTween animasyonunu tetikle
        if (_wasPerfect)
        {
            PlayPerfectEffect();
        }
        
        comboStep++;
        if (comboStep > 3) comboStep = 1;

        _lastAttackTime = Time.time;
        _anim.SetInteger("comboStep", comboStep); 

        _anim.Play("Attack" + comboStep, -1, 0f); 

        OnAttackStarted?.Invoke(); 

        StopAllCoroutines();
        StartCoroutine(ForceResetAttack(stateInfo.length)); 
    }

    // YENİ FONKSİYON: DOTween ile tatlı (juicy) efekt animasyonu
    private void PlayPerfectEffect()
    {
        if (perfectHitEffect == null) return;

        // Eğer oyuncu art arda çok hızlı perfect atarsa, önceki animasyonu iptal et ki çakışmasın
        perfectHitEffect.transform.DOKill();
        
        // Efekti aç ve boyutunu 0'a çek ki büyüme animasyonu temiz başlasın
        perfectHitEffect.enabled = true;
        perfectHitEffect.transform.localScale = Vector3.zero; 

        // DOTween Sequence: Animasyonları sıraya diziyoruz
        Sequence effectSeq = DOTween.Sequence();

        // 1. "Pıt" diye büyüme (OutBack ile hedeften biraz daha fazla büyüyüp geri esner)
        effectSeq.Append(perfectHitEffect.transform.DOScale(_effectOriginalScale, 0.1f).SetEase(Ease.OutBack));
        
        // 2. Ekranda bekleme süresi
        effectSeq.AppendInterval(effectDuration);
        
        // 3. Hızla küçülerek kaybolma (InBack ile içine çökerek kaybolur)
        effectSeq.Append(perfectHitEffect.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack));
        
        // 4. Bütün animasyon bitince objeyi tekrar kapat (optimizasyon için)
        effectSeq.OnComplete(() => perfectHitEffect.enabled = false);
    }

    private IEnumerator ForceResetAttack(float animLength)
    {
        float waitTime = animLength > 0 ? animLength : 0.5f;
        yield return new WaitForSeconds(waitTime);
        EndAttackSequence();
    }

    public void EndAttackSequence()
    {
        OnAttackEnded?.Invoke(); 
    }

    public void CheckHit()
    {
        Vector2 dir = Vector2.right * (transform.localScale.x > 0 ? 1 : -1);
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + attackHeightOffset);

        Debug.DrawRay(rayOrigin, dir * attackDistance, Color.red, 1f);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, attackDistance, enemyLayer);

        if (hit.collider != null && hit.collider.TryGetComponent(out IDamageable target))
        {
            int damage = _wasPerfect ? damageAmount * 2 : damageAmount;
            target.TakeDamage(damage, dir * 5f);
        }
    }
}