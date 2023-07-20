using System;
using UnityEngine;


public class PlayerShooter : MonoBehaviour {
    public enum AimState {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }

    public Gun gun;
    public LayerMask excludeTarget;

    private PlayerInput playerInput;
    private Animator playerAnimator;
    private Camera playerCamera;

    private float waitingTImeForReleasingAim = 2.5f;        //자동으로 HipFire 상태에서 Idle 상태로 돌아오는데 걸리느 시간
    private float lastFireInputTIme;

    private Vector3 aimPoint;       //조준하고 있는 위치, fps 의 경우는 화면 정중앙이기 때문에 필요 없음.

    //카메라와 캐릭터가 정렬이 되었는가..?
    private bool linedUp => !(Mathf.Abs(playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
    //총이 발사될 만큼 충분한 공간이 있는가.
    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * gun.fireTransform.position.y,
        gun.fireTransform.position, ~excludeTarget);
    //여기 이해 안됨..ㅜㅜ
    void Awake() {
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer))) {
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start() {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();

    }

    private void OnEnable() {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable() {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate() {
        if (playerInput.fire) {
            lastFireInputTIme = Time.time;
            Shoot();
        } else if (playerInput.reload) {
            Reload();
        }
    }

    private void Update() {
        UpdateAimTarget();

        var angle = playerCamera.transform.eulerAngles.x;
        if (angle > 270f) {
            angle -= 360f;
        }
        angle = angle / -180f + 0.5f;
        playerAnimator.SetFloat("Angle", angle);
        if (!playerInput.fire && Time.time >= lastFireInputTIme + waitingTImeForReleasingAim) {
            aimState = AimState.Idle;
        }
        UpdateUI();
    }

    public void Shoot() {
        if (aimState == AimState.Idle) {
            if (linedUp) {
                aimState = AimState.HipFire;
            }
        } else if (aimState == AimState.HipFire) {
            if (hasEnoughDistance) {
                if (gun.Fire(aimPoint)) {
                    playerAnimator.SetTrigger("Shoot");
                }
            } else {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload() {
        if (gun.Reload()) {
            playerAnimator.SetTrigger("Reload");
        }
    }

    private void UpdateAimTarget() {
        RaycastHit hit;
        //ViewportPointToRay-한점을 찍어주면 그 점을 향해 나아가는 Ray 생성
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget)) {
            aimPoint = hit.point;

            if (Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget)) {
                aimPoint = hit.point;
            }
        } else {
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }
    }

    private void UpdateUI() {
        if (gun == null || UIManager.Instance == null) return;

        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);

        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }
    //IK가 갱신될 떄마다 자동으로 실행-왼손이 항상 손잡이로 고정되게 하는 코드래요.....
    private void OnAnimatorIK(int layerIndex) {
        if (gun == null || gun.state == Gun.State.Reloading) return;

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);

    }
}