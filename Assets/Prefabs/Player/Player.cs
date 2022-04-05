using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Camera playerEye;
    [SerializeField] Transform playerSpringArm;
    [SerializeField] float jumpSpeed = 10f;
    [SerializeField] float rotationVelocity = 1f;
    [SerializeField] GameObject MagicProjectile;
    [SerializeField] Transform MagicProjectileSpawnPoint;

    PlayerInput playerInput;
    Animator animator;
    Vector2 moveInput;

    Rigidbody rb;


    [Header("Camera Movement")]
    Vector2 mouseInput;

    struct Move
    {
        public Move(Vector2 moveInput, Vector2 lookInput, float DeltaTime, float TimeStamp)
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

    //this data type is devised to help pass data from server to client to synch up location and rotation
    struct PlayerMoveState
    {
        public PlayerMoveState(Vector3 Position, Quaternion Rotation, float TimeStamp)
        {
            position = Position;
            rotation = Rotation;
            timeStamp = TimeStamp;
        }
        public Vector3 position;
        public Quaternion rotation;
        public float timeStamp;
    }
    //variable hold location and rotation of the player that the server will update and inform the client
    private NetworkVariable<PlayerMoveState> netPlayerMoveState = new NetworkVariable<PlayerMoveState>();
    private List<Move> UnProcessedMoves = new List<Move>();

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = new PlayerInput();
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.Disable();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            PlayerStart playerStart = FindObjectOfType<PlayerStart>();
            transform.position = playerStart.GetRandomSpawnPos();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            rb = GetComponent<Rigidbody>();
            playerInput.Gameplay.Move.performed += OnMove;
            playerInput.Gameplay.Move.canceled += OnMove;
            playerInput.Gameplay.MouseMove.performed += MouseMove;
            playerInput.Gameplay.MouseMove.canceled += MouseMove;
            playerInput.Gameplay.Jump.performed += Jump;
            playerInput.Gameplay.MagicAttack.performed += MagicAttack;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
            if (playerEye != null)
            {
                playerEye.enabled = true;
            }
            netPlayerMoveState.OnValueChanged += OnPlayerMoveStateReplicated;
        }

        animator = GetComponent<Animator>();
    }

    private void MagicAttack(InputAction.CallbackContext obj)
    {
        MagicAttackAnimationTrigger();
    }

    private void MagicAttackAnimationTrigger()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    [ServerRpc]
    private void MagicAttackSpawnProjectileServerRpc()
    {
        if(MagicProjectile != null && MagicProjectileSpawnPoint != null)
        {
            GameObject newMagicProjectile = NetworkBehaviour.Instantiate(MagicProjectile, MagicProjectileSpawnPoint.position, MagicProjectileSpawnPoint.rotation);
        }
    }




    private void OnPlayerMoveStateReplicated(PlayerMoveState previousValue, PlayerMoveState newValue)
    {
        transform.position = newValue.position;
        transform.rotation = newValue.rotation;
        List<Move> stillUnprocessedMoves = new List<Move>();
        foreach (Move move in UnProcessedMoves)
        {
            if (newValue.timeStamp < move.timeStamp)
            {
                stillUnprocessedMoves.Add(move);
            }
        }
        UnProcessedMoves = stillUnprocessedMoves;
        foreach (Move move in UnProcessedMoves)
        {
            ProcessMove(move);
        }
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        JumpServerRpc();
    }
    [ServerRpc]
    void JumpServerRpc()
    {
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
        }
    }
    private void MouseMove(InputAction.CallbackContext obj)
    {
        mouseInput = obj.ReadValue<Vector2>();
    }

    private void OnMove(InputAction.CallbackContext obj)
    {
        moveInput = obj.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void Update()
    {
        //if this character is played locally and not the server
        if (IsOwner && !IsServer)
        {
            Move currentMove = new Move(moveInput, mouseInput, Time.deltaTime, Time.timeSinceLevelLoad);
            ProcessMoveServerRpc(currentMove);
            UnProcessedMoves.Add(currentMove);
            ProcessMove(currentMove);
        }
        //we are host (the server with a player playing)
        if (IsOwnedByServer && IsServer)
        {
            Move currentMove = new Move(moveInput, mouseInput, Time.deltaTime, Time.timeSinceLevelLoad);
            ProcessMoveServerRpc(currentMove);
        }
        //not server or locally controlled
        if (!IsOwner && !IsServer)
        {
            transform.position = netPlayerMoveState.Value.position;
            transform.rotation = netPlayerMoveState.Value.rotation;
        }
    }

    //we tell server to do it through this function
    [ServerRpc]
    void ProcessMoveServerRpc(Move moveStruct)
    {
        ProcessMove(moveStruct);
        netPlayerMoveState.Value = new PlayerMoveState(transform.position, transform.rotation, moveStruct.timeStamp);
    }

    //goal is to do it locally here
    void ProcessMove(Move moveStruct)
    {
        transform.position += (GetControlRight() * moveStruct.move.x + GetControlForward() * moveStruct.move.y).normalized * moveStruct.deltaTime * moveSpeed;
        //look left and right
        transform.Rotate(Vector3.up, moveStruct.look.x* rotationVelocity  * moveStruct.deltaTime);
        //look up and down
        playerSpringArm.transform.rotation *= Quaternion.Euler(-moveStruct.look.y * rotationVelocity * moveStruct.deltaTime, 0f, 0f);

        //handle animation change
        float currentMoveSpeed = moveInput.magnitude * moveSpeed;
        if (animator != null)
        {
            animator.SetFloat("speed", currentMoveSpeed);
        }
    }


    Vector3 GetControlRight()
    {
        return playerEye.transform.right;
    }

    Vector3 GetControlForward()
    {
        return Vector3.Cross(GetControlRight(), Vector3.up);
    }
}
