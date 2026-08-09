using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 1.8f;
    [SerializeField] private LayerMask buttonLayer;      // "Button" レイヤーを指定

    [Header("UI Prompt")]
    [SerializeField] private GameObject interactPromptUI; // "E: 操作" テキスト

    private ElevatorButton _currentTarget;

    private void Update()
    {
        ScanForButton();
        HandleInteractInput();
    }

    private void ScanForButton()
    {
        // 画面中央からレイを飛ばす
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, buttonLayer))
        {
            _currentTarget = hit.collider.GetComponent<ElevatorButton>();
        }
        else
        {
            _currentTarget = null;
        }

        // プロンプトUIの表示切替
        if (interactPromptUI != null)
            interactPromptUI.SetActive(_currentTarget != null);
    }

    private void HandleInteractInput()
    {
        if (_currentTarget == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            _currentTarget.Press();
        }
    }
}