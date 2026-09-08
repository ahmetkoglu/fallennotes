using UnityEngine;
using System;

// Karakterlerin o anki durumunu takip eden Enum (State Machine altyapısı)
public enum EntityState 
{
    Normal,
    Idle,
    Moving,
    Crouching,
    Attacking,
    Dead,
    
}

// Oyuncu ve düşmanların ortak hasar alma arayüzü[cite: 11]
public interface IDamageable 
{
    void TakeDamage(int damage, Vector2 knockback);
    event Action<int> OnHealthChanged; // Sağlık değiştiğinde tetiklenen Delegate
}