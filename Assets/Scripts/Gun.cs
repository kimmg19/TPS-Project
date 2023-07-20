using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour {
    public enum State {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }

    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;        //총알 궤적

    private AudioSource gunAudioPlayer;             //총 발사 소리, 재장전 소리
    public AudioClip shotClip;
    public AudioClip reloadClip;

    public ParticleSystem muzzleFlashEffect;        //총구화염
    public ParticleSystem shellEjectEffect;         //탄피효과

    public Transform fireTransform;                 //총알이 나가는 위치
    public Transform leftHandMount;                 //왼손의 위치

    public float damage = 25;                       //총의 데미지
    public float fireDistance = 100f;               //사정거리

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;               //연사력
    public float reloadTime = 1.8f;                 //재장전 시간

    [Range(0f, 10f)] public float maxSpread = 3f;   //탄퍼짐 정도
    [Range(1f, 10f)] public float stability = 1f;   //반동-높을수록 안정
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;    //반동 회복 속도
    private float currentSpread;                    //현재 탄퍼짐 정도
    private float currentSpreadVelocity;

    private float lastFireTime;                     //가장최근발사시간

    private LayerMask excludeTarget;                //총을 맞으면 안되는 애들

    private void Awake() {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();
        bulletLineRenderer.positionCount = 2;       // 총을 발사하는 지점과 총이 맞은 지점.
        bulletLineRenderer.enabled = false;

    }
    //총을 누가 들고 있는지
    public void Setup(PlayerShooter gunHolder) {
        this.gunHolder = gunHolder;
        excludeTarget = gunHolder.excludeTarget;
    }
    //총이 활성화 될 때마다 초기화
    private void OnEnable() {
        magAmmo = magCapacity;
        currentSpread = 0f;
        lastFireTime = 0f;
        state = State.Ready;
    }
    //
    private void OnDisable() {
        StopAllCoroutines();
    }
    //Shot을 감싸는 역할
    public bool Fire(Vector3 aimTarget) {
        if (state == State.Ready && Time.time >= lastFireTime + timeBetFire) {
            var fireDirection = aimTarget - fireTransform.position;     //목표지점에서 발사지점을 빼서 총알이 날아가는 궤적 설정
            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;       //탄퍼짐 구현 부분
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;       //탄퍼짐 구현 부분

            currentSpread += 1f / stability;
            Shot(fireTransform.position, fireDirection);
            lastFireTime = Time.time;

            return true;
        }
        return false;
    }

    private void Shot(Vector3 startPoint, Vector3 direction) {
        RaycastHit hit;
        Vector3 hitPosition;

        if (Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget)) {
            var target = hit.collider.GetComponent<IDamageable>();
            if (target != null) {
                DamageMessage damageMessage;
                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            } else {
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitPosition = hit.point;                                //총이 적에게 맞은 지점.
        } else {                                                    //hitPosition- ShotEffct 사용 위해.
            hitPosition = startPoint + direction * fireDistance;    //총이 최대 사정거리 만큼 나아간 지점
        }
        StartCoroutine(ShotEffect(hitPosition));
        magAmmo--;
        if (magAmmo <= 0) {
            state = State.Empty;
        }

    }
    //hitPosition-총알이 맞은 지점
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);
        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);
        yield return new WaitForSeconds(0.03f);
        bulletLineRenderer.enabled = false;

    }
    //ReloadRoutine 을 감싸는 역할
    public bool Reload() {
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity) {
            return false;
        }
        StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine() {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);
        yield return new WaitForSeconds(reloadTime);

        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0f, ammoRemain);
        magAmmo += (int)ammoToFill;
        ammoRemain -= (int)ammoToFill;
        state = State.Ready;
    }
    //총의 반동 값 체크
    private void Update() {
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
        currentSpread
               = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);
    }
}