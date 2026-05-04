using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 8f;
    public float gravity = -20f;
    public float mouseSensitivity = 0.1f;

    private CharacterController _controller;
    private float _verticalVelocity;
    private float _cameraPitch;
    private Camera _playerCamera;

    public override void Spawned()
    {
        _controller = GetComponent<CharacterController>();

        if (Object.HasStateAuthority)
        {
            _controller.enabled = false;
            transform.position = transform.position;
            _controller.enabled = true;

            GameObject camObj = new GameObject("PlayerCamera");
            _playerCamera = camObj.AddComponent<Camera>();
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            camObj.transform.localRotation = Quaternion.identity;

            Camera mainCam = Camera.main;
            if (mainCam != null)
                mainCam.gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            if (mesh != null)
                mesh.enabled = false;
        }
    }

    void Update()
    {
        if (!Object.HasStateAuthority) return;

        if (_controller == null)
            _controller = GetComponent<CharacterController>();
        if (_controller == null) return;

        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null) return;

        // Mouse look - left/right rotates player
        transform.Rotate(0, ms.delta.x.ReadValue() * mouseSensitivity, 0);

        // Mouse look - up/down tilts camera only
        _cameraPitch -= ms.delta.y.ReadValue() * mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);
        if (_playerCamera != null)
            _playerCamera.transform.localEulerAngles = new Vector3(_cameraPitch, 0, 0);

        // Movement
        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed) move += transform.forward;
        if (kb.sKey.isPressed) move -= transform.forward;
        if (kb.aKey.isPressed) move -= transform.right;
        if (kb.dKey.isPressed) move += transform.right;

        // Gravity
        if (_controller.isGrounded)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        if (kb.spaceKey.wasPressedThisFrame && _controller.isGrounded)
            _verticalVelocity = 8f;

        move.y = _verticalVelocity;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }
}