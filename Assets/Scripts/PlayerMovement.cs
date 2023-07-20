using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    private CharacterController characterController;
    private PlayerInput playerInput;
    private PlayerShooter playerShooter;
    private Animator animator;

    private Camera followCam;

    public float speed = 6f;
    public float jumpVelocity = 20f;
    [Range(0.01f, 1f)] public float airControlPercent;

    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;

    private float speedSmoothVelocity;
    private float turnSmoothVelocity;

    private float currentVelocityY;
    //characterController.velocity-> characterController의 속도 벡터
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;

    private void Start() {

        playerInput = GetComponent<PlayerInput>();
        playerShooter = GetComponent<PlayerShooter>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        followCam = Camera.main;                //Camera.main-메인 태그가 붙어있는 카메라 컴포넌트.
    }

    private void FixedUpdate() {
        if (currentSpeed > 0.2f || playerInput.fire || playerShooter.aimState == PlayerShooter.AimState.HipFire) Rotate();

        Move(playerInput.moveInput);

        if (playerInput.jump) Jump();
    }

    private void Update() {
        UpdateAnimation(playerInput.moveInput);
    }

    public void Move(Vector2 moveInput) {
        var targetSpeed = speed * moveInput.magnitude;
        var moveDirectoin = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x); //x,z 
        currentVelocityY += Time.deltaTime * Physics.gravity.y;     //CharactorController는 중력이 없기에 추가해줌.  //y

        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;
        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        var velocity = moveDirectoin * targetSpeed + Vector3.up * currentVelocityY;     //x,z 와 y를 따로 계산해 여기서 더해줌.

        characterController.Move(velocity * Time.deltaTime);

        if (characterController.isGrounded) {
            currentVelocityY = 0f;
        }
    }
    //캐릭터를 카메라가 바라보는 방향으로 정렬.
    public void Rotate() {
        var targetRotation = followCam.transform.eulerAngles.y;

        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRotation;
    }

    public void Jump() {
        if (!characterController.isGrounded) {
            return;
        }
        currentVelocityY = jumpVelocity; ;
    }

    private void UpdateAnimation(Vector2 moveInput) {
        var animationSpeedPercent = currentSpeed / speed;
        animator.SetFloat("Horizontal Move", moveInput.x * animationSpeedPercent, 0.05f, Time.deltaTime);
        animator.SetFloat("Vertical Move", moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);
    }
}