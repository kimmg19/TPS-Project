using System;
using UnityEngine;

//캐릭터, 적 등 게임 속에서 생명체들이 가지는 클래스
public class LivingEntity : MonoBehaviour, IDamageable {
    public float startingHealth = 100f;                 //초기 체력
    public float health { get; protected set; }         //현재 체력-외부에선 읽을 수 있으난 덮어씌울수 없다.
    public bool dead { get; protected set; }

    public event Action OnDeath;

    private const float minTimeBetDamaged = 0.1f;       //공격과 공격 사이의 대기 시간.
    private float lastDamagedTime;                      //최근에 공격 받은 시간.

    //참이면 무적 상태.
    protected bool IsInvulnerabe {
        get {
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false;

            return true;
        }
    }

    protected virtual void OnEnable() {
        dead = false;
        health = startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage) {
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead) return false;

        lastDamagedTime = Time.time;
        health -= damageMessage.amount;

        if (health <= 0) Die();

        return true;
    }

    public virtual void RestoreHealth(float newHealth) {
        if (dead) return;

        health += newHealth;
    }

    public virtual void Die() {
        if (OnDeath != null) OnDeath();

        dead = true;
    }
}