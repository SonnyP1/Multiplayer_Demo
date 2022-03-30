using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Player : NetworkBehaviour
{
    [SerializeField] float MoveSpeed = 5f;
    [SerializeField] Camera PlayerEye;
    PlayerInput playInput;
    Animator animator;

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
            PlayerStart playerStart = FindObjectOfType<PlayerStart>();
            transform.position = playerStart.GetRandomSpawnPos();
        }
    }
    private void SetupPlayerInput()
    {
        if(IsOwner && playInput != null)
        {
            playInput.Gameplay.Move.performed += Move;
            playInput.Gameplay.Move.canceled += Move;
        }
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
        if(IsServer)
        {
            transform.position += new Vector3(netMoveInput.Value.x, 0, netMoveInput.Value.y) * Time.deltaTime * MoveSpeed;
        }

        float currentMoveSpeed = netMoveInput.Value.magnitude * MoveSpeed;
        if(animator != null)
        {
            animator.SetFloat("speed",currentMoveSpeed);
        }
    }
}
