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


    private float _gravity = -9.8f;
    private CharacterController _characterController;
    private float turnSmoothVelocity;
    private Vector2 _moveInput;
    private Vector2 _mouseInput;
    PlayerInput playInput;
    Animator animator;
    float cameraYaw;
    float cameraPitch;
    private List<MoveInfo> UnProcessedMoves = new List<MoveInfo>();

    struct MoveInfo
    {
        public MoveInfo(Vector2 moveInput, Vector2 lookInput,float DeltaTime,float TimeStamp)
        {
            move = moveInput;
            look = lookInput;
            deltaTime = DeltaTime;
            timeStamp = TimeStamp;
        }

        public Vector2 move;
        public Vector2 look;
        public float deltaTime;
        public float timeStamp;
    }
    struct PlayerMoveState
    {
        public PlayerMoveState(Vector3 Position, Quaternion Rotation,float TimeStamp)
        {
            position = Position;
            rotation = Rotation;
            timeStamp = TimeStamp;
        }
        public Vector3 position;
        public Quaternion rotation;
        public float timeStamp;
    }

    private NetworkVariable<PlayerMoveState> netPlayerMoveState = new NetworkVariable<PlayerMoveState>();

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
        _characterController = GetComponent<CharacterController>();
        if (IsServer)
        {
            
            PlayerStart playerStart = FindObjectOfType<PlayerStart>();
            transform.position = playerStart.GetRandomSpawnPos();
        }
    }
    private void SetupPlayerInput()
    {
        if(IsOwner && playInput != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            playInput.Gameplay.Move.performed += OnMove;
            playInput.Gameplay.Move.canceled += OnMove;
            playInput.Gameplay.MouseMove.performed += OnMouseMove;
            playInput.Gameplay.MouseMove.canceled += OnMouseMove;
            playInput.Gameplay.Jump.performed += OnJumpBtnPressed;
        }
    }

    private void OnJumpBtnPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        //Add later
        //JumpServerRpc();
    }

    private void OnMouseMove(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _mouseInput = obj.ReadValue<Vector2>();
    }

    private void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _moveInput = obj.ReadValue<Vector2>();
    }

    [ServerRpc]
    void UpdatedMoveAndRotationServerRpc(MoveInfo moveInfo)
    {
        UpdateLocalCameraRotationAndMove(moveInfo);
        netPlayerMoveState.Value = new PlayerMoveState(transform.position, transform.rotation,moveInfo.timeStamp);
    }


    private void UpdateLocalCameraRotationAndMove(MoveInfo moveInfo)
    {
        UpdateMove(moveInfo.move, moveInfo.deltaTime);
        UpdateCameraRotation(moveInfo.look, moveInfo.deltaTime);
    }


    void Start()
    {
        SetupPlayerInput();
        if(IsOwner && PlayerEye != null)
        {
            PlayerEye.enabled = true;
        }

        animator = GetComponent<Animator>();
        netPlayerMoveState.OnValueChanged += OnPlayerMoveStateReplicated;
    }

    private void OnPlayerMoveStateReplicated(PlayerMoveState previousValue, PlayerMoveState newValue)
    {
        transform.position = newValue.position;
        transform.rotation = newValue.rotation;

        List<MoveInfo> stillUnprocessedMoves = new List<MoveInfo>();
        foreach(MoveInfo move in UnProcessedMoves)
        {
            if(newValue.timeStamp < move.timeStamp)
            {
                stillUnprocessedMoves.Add(move);
            }
        }

        UnProcessedMoves = stillUnprocessedMoves;
        foreach(MoveInfo move in UnProcessedMoves)
        {
            UpdateLocalCameraRotationAndMove(move);
        }
    }

    void Update()
    {
        //if this character played locally, and not the server
        if (IsOwner && !IsServer)
        {
            MoveInfo currentMoveInfo = new MoveInfo(_moveInput, _mouseInput, Time.deltaTime, Time.timeSinceLevelLoad);
            UpdatedMoveAndRotationServerRpc(currentMoveInfo);
            UnProcessedMoves.Add(currentMoveInfo);

            UpdateLocalCameraRotationAndMove(currentMoveInfo);
        }

        //we are the host (the server with a player playing)
        if (IsOwnedByServer && IsServer)
        {
            MoveInfo currentMoveInfo = new MoveInfo(_moveInput, _mouseInput, Time.deltaTime, Time.timeSinceLevelLoad);
            UpdatedMoveAndRotationServerRpc(currentMoveInfo);
        }

        //we are not the server, and we not controlled locally either
        if(!IsOwner && !IsServer)
        {
            transform.position = netPlayerMoveState.Value.position;
            transform.rotation = netPlayerMoveState.Value.rotation;
        }

    }



    private void UpdateMove(Vector2 input, float deltaTime)
    {
        Vector3 playerDir = new Vector3(input.x, 0f, input.y).normalized;

        if (playerDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(playerDir.x, playerDir.z) * Mathf.Rad2Deg + PlayerEye.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            SpringArm.transform.rotation *= Quaternion.Inverse(transform.rotation);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            float velocityY = 0;
            if (!_characterController.isGrounded)
            {
                velocityY = _gravity * deltaTime;
            }
            _characterController.Move(moveDir.normalized * MoveSpeed * deltaTime + new Vector3(0, velocityY, 0));
        }

        float currentMoveSpeed = input.magnitude * MoveSpeed;
        if (animator != null)
        {
            animator.SetFloat("speed", currentMoveSpeed);
        }
        else
        {
            Debug.Log("Animator is not playing!");
        }
    }

    private void UpdateCameraRotation(Vector2 look, float deltaTime)
    {
        float deltaTimeMultiplier = 10 * deltaTime;
        cameraYaw += look.x * deltaTimeMultiplier;
        cameraPitch += -look.y * deltaTimeMultiplier;

        
      
        SpringArm.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0.0f);
    }
}
