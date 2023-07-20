using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Transactions;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy : LivingEntity {
    private enum State {
        Patrol,
        Tracking,
        AttackBegin,
        Attacking
    }

    private State state;

    private NavMeshAgent agent;
    private Animator animator;

    public Transform attackRoot;            //이 점을 중심으로 해당 반지름 내에 있는 어떤 물체가 공격당함.
    public Transform eyeTransform;          //좀비의 시야 기준점. 범위 내 플레이어 감지.ㄴ

    private AudioSource audioPlayer;
    public AudioClip hitClip;
    public AudioClip deathClip;

    private Renderer skinRenderer;          //좀비의 피부색을 공격력에 따라 변경

    public float runSpeed = 10f;            //좀비 이속.
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f;      //좀비 회전 속도
    private float turnSmoothVelocity;

    public float damage = 30f;
    public float attackRadius = 2f;
    private float attackDistance;

    public float fieldOfView = 50f;         //좀비 시야각
    public float viewDistance = 10f;        //감지 거리
    public float patrolSpeed = 3f;          //

    [HideInInspector]
    public LivingEntity targetEntity;     //공격받는 대상.
    public LayerMask whatIsTarget;


    private RaycastHit[] hits = new RaycastHit[10];
    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();

    private bool hasTarget => targetEntity != null && !targetEntity.dead;


#if UNITY_EDITOR
    //OnDrawGizmosSelected현재 스크립트를 가지고 있는 오브젝트가 선택되었을 때 매 프레임 실행
    //여기선 좀비 게임 오브젝트를 선택하였을 때 매 프레임 실행
    private void OnDrawGizmosSelected() {
        if (attackRoot != null) {
            //공격범위를 그림
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(attackRoot.position, attackRadius);
        }
        if (eyeTransform != null) {
            //왼쪽으로 25만큼움직이고 오른쪽으로 50 적용시켜 시야각 표현.
            var leftEyeRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
            var leftRayDirection = leftEyeRotation * transform.forward;
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayDirection, fieldOfView, viewDistance);
        }

    }

#endif

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponentInChildren<Renderer>();

        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;
        attackDistance = Vector3.Distance(transform.position, attackPivot) + attackRadius;

        agent.stoppingDistance = attackDistance;
        agent.speed = patrolSpeed;
    }
    //적 생성 초기값.
    public void Setup(float health, float damage,
        float runSpeed, float patrolSpeed, Color skinColor) {
        this.startingHealth = health;
        this.health = health;
        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;
        skinRenderer.material.color = skinColor;
        agent.speed = patrolSpeed;
    }

    private void Start() {
        StartCoroutine(UpdatePath());
    }

    private void Update() {
        if (dead)
            return;
        if (state == State.Tracking) {
            var distance = Vector3.Distance(targetEntity.transform.position, transform.position);
            if (distance <= attackDistance) {

                BeginAttack();
            }
        }
        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);
    }


    private void FixedUpdate() {
        if (dead) return;
        if (state == State.AttackBegin || state == State.Attacking) {
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            var targetAngleY = lookRotation.eulerAngles.y;        //y축을 기준으로 회전.
            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity,
                turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;      //y축이 기준이었으니 0,1,0을 곱함.
        }
        if (state == State.Attacking) {
            var direction = transform.forward;
            //agent.velocity.magnitude- NavMeshAgent가 현재 이동하고 있는 방향과 속도를 나타내는 3D 벡터의 크기
            var deltaDistance = agent.velocity.magnitude * Time.deltaTime;
            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius, direction,
                hits, deltaDistance, whatIsTarget);
            for (var i = 0; i < size; i++) {
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();

                if (attackTargetEntity != null && !lastAttackedTargets.Contains(attackTargetEntity)) {
                    var message = new DamageMessage();
                    message.amount = damage;
                    message.damager = gameObject;
                    if (hits[i].distance <= 0f) {
                        message.hitPoint = attackRoot.position;
                    } else {
                        message.hitPoint = hits[i].point;
                    }
                    message.hitNormal = hits[i].normal;
                    attackTargetEntity.ApplyDamage(message);
                    lastAttackedTargets.Add(attackTargetEntity);
                    break;
                }
            }
        }
    }
    //주기적으로 적을 찾음.
    private IEnumerator UpdatePath() {
        while (!dead) {
            if (hasTarget) {
                if (state == State.Patrol) {
                    state = State.Tracking;
                    agent.speed = runSpeed;
                }
                agent.SetDestination(targetEntity.transform.position);                      //목적지 지정

            } else {
                if (targetEntity != null) {
                    targetEntity = null;
                }

                if (state != State.Patrol) {
                    state = State.Patrol;
                    agent.speed = patrolSpeed;
                }
                if (agent.remainingDistance <= 1f) {
                    var patrolTargetPosition
                        = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                    agent.SetDestination(patrolTargetPosition);
                }
                var colliders = Physics.OverlapSphere(eyeTransform.position, viewDistance, whatIsTarget);
                //시야 내에 존재하면서 livingEntity 유무, target의 생사 여부 확인
                foreach (var collider in colliders) {
                    if (!IsTargetOnSight(collider.transform)) {
                        continue;
                    }
                    //livingEntity  유무와 생사 여부를 통해 걸린 콜라이더가 target이 될지 안될지 정함.
                    var livingEntity = collider.GetComponent<LivingEntity>();
                    if (livingEntity != null || !livingEntity.dead) {
                        targetEntity = livingEntity;
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    public override bool ApplyDamage(DamageMessage damageMessage) {
        if (!base.ApplyDamage(damageMessage)) return false;
        if (targetEntity == null) {
            targetEntity = damageMessage.damager.GetComponent<LivingEntity>();
        }
        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform,
            EffectManager.EffectType.Flesh);
        audioPlayer.PlayOneShot(hitClip);
        return true;
    }

    public void BeginAttack() {
        state = State.AttackBegin;

        agent.isStopped = true;
        animator.SetTrigger("Attack");
    }

    public void EnableAttack() {
        state = State.Attacking;

        lastAttackedTargets.Clear();
    }

    public void DisableAttack() {
        if (hasTarget) {
            state = State.Tracking;
        } else {
            state = State.Patrol;
        }
        agent.isStopped = false;
    }
    //target이 시야 내에 존재하는지-여기서 target은 foreach (var collider in colliders)에서 나온 값.
    private bool IsTargetOnSight(Transform target) {
        var direction = target.position - eyeTransform.position;       //target.position 타겟의 위치,eyeTransform.position-좀비의 눈 위치
        direction.y = eyeTransform.position.y;                        //수직방향은 고려하지 않음.
        if (Vector3.Angle(direction, eyeTransform.forward) < fieldOfView * 0.5f) {
            return false;
        }
        direction.y = target.position.y;
        RaycastHit hit;
        if (Physics.Raycast(eyeTransform.position, direction, out hit, viewDistance, whatIsTarget)) {
            if (hit.transform == target) {
                return true;
            }
        }
        return false;
    }

    public override void Die() {
        base.Die();
        GetComponent<Collider>().enabled = false;

        agent.enabled = false;
        animator.applyRootMotion = true;
        animator.SetTrigger("Die");
        audioPlayer.PlayOneShot(deathClip);
    }
}