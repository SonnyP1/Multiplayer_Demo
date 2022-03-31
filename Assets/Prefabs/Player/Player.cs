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
    private CharacterController _characterController;


    private float RotationSmoothTime = 0.12f;
    private float _rotationVelocity;
    private float _targetRotation = 0.0f;
    private float _verticalVelocity;
    PlayerInput playInput;
    Animator animator;
    Vector2 mouseInput;
    float cameraYaw;
    float cameraPitch;

    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>();
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
        mouseInput = obj.ReadValue<Vector2>();
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
            Vector3 inputDirection = new Vector3(netMoveInput.Value.x, 0.0f, netMoveInput.Value.y).normalized;
            if (!_characterController.isGrounded)
            {
                _verticalVelocity += -9.8f * Time.deltaTime;
            }

            if(netMoveInput.Value != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + SpringArm.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            _characterController.Move(targetDirection.normalized * (currentMoveSpeed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("speed", currentMoveSpeed);
        }

    }

    private void UpdateCameraRotation()
    {
        float deltaTimeMultiplier = 10 * Time.deltaTime;
        cameraYaw += mouseInput.x * deltaTimeMultiplier;
        cameraPitch += -mouseInput.y * deltaTimeMultiplier;

        
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
