using UnityEngine;

/// <summary>
/// 階数ボタンとElevatorMonitorを繋ぐ橋渡しスクリプト。
/// ElevatorButton.csは一切改造しない。
/// </summary>
[RequireComponent(typeof(ElevatorButton))]
public class FloorButtonConnector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("このボタンに対応する階数")]
    [SerializeField] private int floorNumber = 1;

    [Header("References")]
    [SerializeField] private ElevatorMonitor monitor;

    private void Start()
    {
        // ElevatorButtonのイベントに登録する
        // ElevatorButton.csは改造不要
        GetComponent<ElevatorButton>().OnButtonPressed.AddListener(OnPressed);
    }

    private void OnPressed()
    {
        monitor?.SetFloor(floorNumber);
    }
}