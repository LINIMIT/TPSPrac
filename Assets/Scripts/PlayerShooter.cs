using UnityEngine;


public class PlayerShooter : MonoBehaviour
{
    public enum AimState
    {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }

    public Gun gun;
    public LayerMask excludeTarget;//조준에서 제외할레이어

    private PlayerInput playerInput;
    private Animator playerAnimator;
    private Camera playerCamera;

    private float waitingTimeForReleasingAim = 2.5f;//발사 후에 2.5초동안 아무 입력이 없다면 다시 idle로 돌아오게 하기위함
    private float lastFireInputTime;//마지막 사격시간 저장


    private Vector3 aimPoint;//실제로 조준할 대상
    private bool linedUp => !(Mathf.Abs(playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);

    //총이 다른 오브젝트에 의해 파묻혀있다면
    private bool hasEnoughDistance => !Physics.Linecast
        (transform.position + Vector3.up * gun.fireTransform.position.y,
        gun.fireTransform.position, ~excludeTarget/*조준 대상이 아닌 레이어 처리*/);

    void Awake()
    {
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))//플레이어가 자기 자신을 공격하는 상황을 미리 예외처리
        {
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();

    }

    private void OnEnable()
    {
        aimState = AimState.Idle;

        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (playerInput.fire)
        {
            lastFireInputTime = Time.time;//현재시간으로 갱신
            Shoot();
        }
        else if (playerInput.reload)
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget();

        var angle = playerCamera.transform.eulerAngles.x;
        if (angle > 270f)
        {
            angle -= 360f;
        }
        angle = angle / 180f + 0.5f;
        playerAnimator.SetFloat("Angle", angle);

        if(!playerInput.fire && Time.time >= lastFireInputTime+waitingTimeForReleasingAim)
            //마지막으로 발사했던 시점에서 + 가만히 2.5초동안 있는 시간이-> 발사버튼에서 손을 땐 뒤에
        {
            aimState = AimState.Idle;
        }
        UpdateUI();
    }

    public void Shoot()
    {
        if (aimState == AimState.Idle)
        {
            if (linedUp)
            {
                aimState = AimState.HipFire;
            }

        }
        else if (aimState == AimState.HipFire)
        {
            if (hasEnoughDistance)//정면에 충분한 공간이 확보되어있다면 
            {
                if (gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                }
            }
            else
            {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if (gun.Reload())
        {
            playerAnimator.SetTrigger("Reload");
        }
    }


    /// <summary>
    /// 카메라에서 정면으로 바라본 선이 도착한 지점이 실제 플레이어의 정면에서 바라본 선과 만나는지 아닌지 체크
    /// </summary>
    private void UpdateAimTarget()
    {
        RaycastHit hit;

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));//화면상에 정중앙으로 뻗어가는 레이생성

        if (Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget))
        {
            aimPoint = hit.point;


            if (Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget))
            //캐릭터의 위치에서 에임 타켓까지에 막히게 하는 오브젝트가 있다면
            {
                aimPoint = hit.point;//플레이어 총이쏘는 위치는 막고있는 오브젝트로 가게함
            }
        }
        else//처음부터 레이캐스트에 걸린게 없다면
        {
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }

    }

    private void UpdateUI()
    {
        if (gun == null || UIManager.Instance == null) return;

        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);

        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (gun == null || gun.state == Gun.State.Reloading)
        {
            return;
        }

        //항상 캐릭터의 왼손이 총의 손잡이 위치에 고정되어있음

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);

    }
}