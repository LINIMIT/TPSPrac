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
        if (dead)
        {
            return;
        }

        if (state == State.Tracking)
        {
            var distance = Vector3.Distance(targetEntity.transform.position, transform.position);

            if (distance <= attackDistance)//플레이어와의 거리가 실제공격거리보다 짧다면
            {
                BeginAttack(); //공격시작
            }
        }

        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);//실제 속도값을 파라미터에 적용

    }

    private void FixedUpdate()
    {
        if (dead) return;

        // 공격이 아직 안들어감            공격이 들어가는중(애니메이션에 의해 통제됨
        if (state == State.AttackBegin || state == State.Attacking)
        {

            //공격시작이거나 공격중이라면 적이 바라보는 방향은 플레이어가 되어야함
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            var targetAngleY = lookRotation.eulerAngles.y;


            //타켓을 바라보게 할때 부드럽게 턴하기 위함
            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity, turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;
        }

        if (state == State.Attacking)
        {
            var direction = transform.forward;
            var deltaDistance = agent.velocity.magnitude * Time.deltaTime;//Time.deltaTime동안 움직이게될 거리를 계산 


            //Sphere의 시작 지점부터 어떤 거리만큼 이동한다 했을때 그 사이에 있는 콜라이더를 반환함(SphereCast,SphereCastAll)
            //NonAlloc은 콜라이더의 개수가 몇개인지를 반환
            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius,
                                                  direction, hits, deltaDistance, whatIsTarget);

            for (var i = 0; i < size; i++)
            {
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();


                if (attackTargetEntity != null && !lastAttackedTargets.Contains(attackTargetEntity))//공격 중에 중복공격을 막기위함
                {
                    var message = new DamageMessage();
                    message.amount = damage;
                    message.damager = gameObject;
                    message.hitPoint = hits[i].point;

                    if (hits[i].distance <= 0f)
                    {
                        message.hitPoint = attackRoot.position;
                    }
                    else
                    {
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

    private IEnumerator UpdatePath()//주기적으로 플레이어를 추적함
    {
        while (!dead)
        {
            if (hasTarget)
            {
                if (state == State.Patrol)
                {
                    state = State.Tracking;
                    agent.speed = runSpeed;
                }
                agent.SetDestination(targetEntity.transform.position);
            }
            else//적을 찾지 못했을때
            {
                if (targetEntity != null) targetEntity = null;

                if (state != State.Patrol)
                {
                    state = State.Patrol;
                    agent.speed = patrolSpeed;
                }


                if (agent.remainingDistance <= 1f)//목표지점까지 1f보다 작을 시에만 새로운 포지션을 적용함
                {
                    //적 ai를 20f반경내에 랜덤한 점으로 이동시키기 위함
                    var patrolTargetPosition = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                    agent.SetDestination(patrolTargetPosition);
                }

                //특정 크기의 구체안에 있는 모든 콜라이더를 가져오는데 성능저하방지를 위해 whatIsTarget에 해당하는 레이어만 가져옴
                var colliders = Physics.OverlapSphere(eyeTransform.position, viewDistance, whatIsTarget);

                foreach (var collider in colliders)
                {
                    if (!IsTargetOnSight(collider.transform)) continue; //타겟이 시야내에 없다면

                    var livingEntity = collider.GetComponent<LivingEntity>();

                    if (livingEntity != null && !livingEntity.dead)
                    {
                        targetEntity = livingEntity;
                        break;
                    }
                }
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

        var direction = target.position - eyeTransform.position;
        direction.y = eyeTransform.forward.y;//높이는 생각 안하기위해서


        if (Vector3.Angle(direction, eyeTransform.forward) > fieldOfView * 0.5f)//시야에서 벗어났다면
        {
            return false;
        }


        direction = target.position - eyeTransform.position;

        RaycastHit hit;

        //시야각 내에는 있지만 시야 사이에 장애물이 있다면
        if (Physics.Raycast(eyeTransform.position, direction, out hit, viewDistance, whatIsTarget))
        {
            if (hit.transform == target)
            {
                return true;
            }
            return false;
        }


        return false;
    }

    public override void Die()
    {

    }
}