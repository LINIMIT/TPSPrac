using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy : LivingEntity
{
    private enum State
    {
        Patrol,
        Tracking,
        AttackBegin,
        Attacking
    }

    private State state;

    private NavMeshAgent agent;
    private Animator animator;

    public Transform attackRoot;
    public Transform eyeTransform;

    private AudioSource audioPlayer;
    public AudioClip hitClip;
    public AudioClip deathClip;

    private Renderer skinRenderer; //피부색 변경

    public float runSpeed = 10f;
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    public float damage = 30f;
    public float attackRadius = 2f;
    private float attackDistance;

    public float fieldOfView = 50f;
    public float viewDistance = 10f;
    public float patrolSpeed = 3f;

    public LivingEntity targetEntity;
    public LayerMask whatIsTarget; //누가 플레이어인지 확인


    private RaycastHit[] hits = new RaycastHit[10];

    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();//직전 공격까지 공격한 대상을 저장할 리스트
                                                                              //똑같은 대상에게 중복 공격을 안하게 하기 위함

    private bool hasTarget => targetEntity != null && !targetEntity.dead;


#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (attackRoot != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(attackRoot.position, attackRadius);
        }

        if (eyeTransform != null)
        {
            var leftEyeRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
            var leftRayDirectioon = leftEyeRotation * transform.forward;
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayDirectioon, fieldOfView, viewDistance);
        }
    }

#endif

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponentInChildren<Renderer>();//스킨 렌더러는 좀비오브젝트가 아닌 그밑에 LowMan에 있기 때문

        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;

        attackDistance = Vector3.Distance(transform.position, attackRoot.position) + attackRadius;

        agent.stoppingDistance = attackDistance;
        agent.speed = patrolSpeed;

    }

    public void Setup(float health, float damage,
        float runSpeed, float patrolSpeed, Color skinColor)//ai의 처음시작 스펙을 결정함
    {
        this.startingHealth = health;
        this.health = health;
        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;

        skinRenderer.material.color = skinColor;

        agent.speed = patrolSpeed;


    }

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        if (dead) return;
    }

    private IEnumerator UpdatePath()
    {
        while (!dead)
        {
            if (hasTarget)
            {
                agent.SetDestination(targetEntity.transform.position);
            }
            else
            {
                if (targetEntity != null) targetEntity = null;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;

        return true;
    }

    public void BeginAttack()
    {
        state = State.AttackBegin;

        agent.isStopped = true;
        animator.SetTrigger("Attack");
    }

    public void EnableAttack()
    {
        state = State.Attacking;

        lastAttackedTargets.Clear();
    }

    public void DisableAttack()
    {
        state = State.Tracking;

        agent.isStopped = false;
    }

    private bool IsTargetOnSight(Transform target)
    {
        return false;
    }

    public override void Die()
    {

    }
}