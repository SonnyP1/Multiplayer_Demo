using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Player : NetworkBehaviour
{
    [SerializeField] float MoveSpeed = 5f;
    [SerializeField] Camera PlayerEye;
    [SerializeField] GameObject SpringArm;
    [SerializeField] float rotationSmoothTime = 0.01f;



    private CharacterController _characterController;
    private float turnSmoothVelocity;
    PlayerInput playInput;
    Animator animator;
    Vector2 mouseInput;
    float cameraYaw;
    float cameraPitch;

    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>();
    private NetworkVariable<Vector2> netMouseInput = new NetworkVariable<Vector2>();
    private void Awake()
    {
        if(playInput == null)
        {
            playInput = new PlayerInput();
        }
    }
    private void OnEnable()
    {
        if(playInput != null)
        {
            playInput.Enable();
        }
    }
    private void OnDisable()
    {
        if(playInput != null)
        {
            playInput.Disable();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer)
        {
            _characterController = GetComponent<CharacterController>();
            PlayerStart playerStart = FindObjectOfType<PlayerStart>();
            transform.position = playerStart.GetRandomSpawnPos();
        }
    }
    private void SetupPlayerInput()
    {
        if(IsOwner && playInput != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            playInput.Gameplay.Move.performed += Move;
            playInput.Gameplay.Move.canceled += Move;
            playInput.Gameplay.MouseMove.performed += OnMouseMove;
            playInput.Gameplay.MouseMove.canceled += OnMouseMove;
        }
    }

    private void OnMouseMove(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnMouseUpdatedServerRPC(obj.ReadValue<Vector2>());
    }

    private void Move(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInputUpdatedServerRpc(obj.ReadValue<Vector2>());
    }

    [ServerRpc]
    private void OnInputUpdatedServerRpc(Vector2 newInputValue)
    {
        netMoveInput.Value = newInputValue;
    }

    [ServerRpc]
    private void OnMouseUpdatedServerRPC(Vector2 newInputValue)
    {
        netMouseInput.Value = newInputValue;
    }

    void Start()
    {
        SetupPlayerInput();
        if(IsOwner && PlayerEye != null)
        {
            PlayerEye.enabled = true;
        }

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        //on the server and the client this is both called
        //however the moveinput is something on the client but ZERO on the server
        UpdateCameraRotation();


        float currentMoveSpeed = netMoveInput.Value.magnitude * MoveSpeed;
        if (IsServer)
        {
            Vector3 playerDir = new Vector3(netMoveInput.Value.x, 0f, netMoveInput.Value.y).normalized;

            if(playerDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(playerDir.x, playerDir.z) * Mathf.Rad2Deg + PlayerEye.transform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                float velocityY = 0;
                if(!_characterController.isGrounded)
                {
                    velocityY = -9.8f * Time.deltaTime;
                }
                _characterController.Move(moveDir.normalized * MoveSpeed* Time.deltaTime + new Vector3(0,velocityY,0));
            }
        }

        if (animator != null)
        {
            animator.SetFloat("speed", currentMoveSpeed);
        }

    }

    private void UpdateCameraRotation()
    {
        float deltaTimeMultiplier = 10 * Time.deltaTime;
        cameraYaw += netMouseInput.Value.x * deltaTimeMultiplier;
        cameraPitch += -netMouseInput.Value.y * deltaTimeMultiplier;

        
        if (IsServer)
        {
            SpringArm.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0.0f);
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
