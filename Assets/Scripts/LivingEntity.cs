using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamageable
{
    public float startingHealth = 100f;
    public float health { get; protected set; }
    public bool dead { get; protected set; }

    public event Action OnDeath; //LivingEntitiy가 사망할시에 외부에서 접근해서 할당할 수 있음

    private const float minTimeBetDamaged = 0.1f;//공격과 공격사이의 최소 시간
    private float lastDamagedTime;//최근에 공격을 당한 시점

    protected bool IsInvulnerabe
    {
        get
        {
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false;//LivingEntitiy

            return true;
        }
    }

    protected virtual void OnEnable()
    {
        dead = false;
        health = startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)
    {                       // 공격받았을 때 공격한 대상이 나 자신이라면
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead) return false;

        lastDamagedTime = Time.time;
        health -= damageMessage.amount;

        if (health <= 0) Die();

        return true;
    }

    public virtual void RestoreHealth(float newHealth)
    {
        if (dead) return;

        health += newHealth;
    }


    public virtual void Die()
    {
        if (OnDeath != null) OnDeath();

        dead = true;
    }
}