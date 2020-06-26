using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }

    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;

    private AudioSource gunAudioPlayer;
    public AudioClip shotClip;
    public AudioClip reloadClip;

    public ParticleSystem muzzleFlashEffect;
    public ParticleSystem shellEjectEffect;

    public Transform fireTransform;
    public Transform leftHandMount;

    public float damage = 25;
    public float fireDistance = 100f;

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;//연사속도
    public float reloadTime = 1.8f;

    [Range(0f, 10f)] public float maxSpread = 3f;//탄퍼짐
    [Range(1f, 10f)] public float stability = 1f;//반동 값이 올라갈수록 반동이 심해짐
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;//총이 흔들리고나서 돌아오는 속도 
    private float currentSpread;// 현재 탄퍼짐 정도
    private float currentSpreadVelocity;//현재 탄퍼짐의 반경이 실시간으로 변하는 변화량을 저장함

    private float lastFireTime;

    private LayerMask excludeTarget;

    private void Awake()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;//1번째는 총알이 시작한위치 2번째는 총알이 도착한 위치
        bulletLineRenderer.enabled = false;

    }

    public void Setup(PlayerShooter gunHolder)
    {
        this.gunHolder = gunHolder;
        excludeTarget = gunHolder.excludeTarget;
    }

    private void OnEnable()
    {
        magAmmo = magCapacity;
        currentSpread = 0;
        lastFireTime = 0;
        state = State.Ready;

    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 aimTarget)//조준대상을 받은 다음 해당 방향으로 발사가 가능한 상태에서 shot 메서드를 실행시킴 
                                       //그리고 발사가 성공했는지 아닌지를 bool값으로 리턴함 
    {
        if (state == State.Ready && Time.time >= lastFireTime + timeBetFire)
        {
            var fireDirection = aimTarget - fireTransform.position;

            var xError = Utility.GetRandomNormalDistribution(0f, currentSpread);//총의 반동을 주기 위한 작업
            var yError = Utility.GetRandomNormalDistribution(0f, currentSpread);

            //원래 총이 가야할 방향에 yError를 대입하여 위나 아래로 움직이게함
            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;


            currentSpread += 1f / stability;

            lastFireTime = Time.time;
            Shot(fireTransform.position, fireDirection);
            return true;//발사를 할수있는 상황이면 true반환
        }

        return false;
    }

    private void Shot(Vector3 startPoint, Vector3 direction)//발사가 실제로 실행됨 
    {
        RaycastHit hit;
        Vector3 hitposition;

        //excludeTarget에 해당하는 마스크를 제외하는 마스크를 표현함 ~연산자
        if (Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget))
        {
            //상대방의 오브젝트로부터 IDamageable로 이루어져있는지 검사함
            var target = hit.collider.GetComponent<IDamageable>();

            if (target != null)
            {
                DamageMessage damageMessage;//초기화작업
                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            else
            {
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitposition = hit.point;
        }

        else//총알이 아무것도 안맞았다면 총의 최대사거리까지 감
        {
            hitposition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitposition));//hitposition까지 이펙트가 생김
        magAmmo--;
        if (magAmmo <= 0)
        {
            state = State.Empty;
        }
    }

    private IEnumerator ShotEffect(Vector3 hitPosition)//총알이 도착한 위치를 입력받아서 총알 이펙트 구현함
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);//총알소리 중첩되게하기 위함

        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        yield return new WaitForSeconds(0.03f);
        bulletLineRenderer.enabled = false;
    }

    public bool Reload()//재장전 시도
    {
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine()//실제로 재장전 처리를 함
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        var ammotoFit = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        magAmmo += ammotoFit;
        ammoRemain -= ammotoFit;
        state = State.Ready;

    }

    private void Update()//현재 총알반동값을 상태에 따라 갱신함
    {
        //탄퍼짐이 계속되도 일정량이상은 퍼짐이 안되게 막음
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
        currentSpread
            = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1 / restoreFromRecoilSpeed);

    }
}