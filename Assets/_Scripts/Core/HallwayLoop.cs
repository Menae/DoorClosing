using UnityEngine;

// The passage is rotationally symmetric at the crossing. Only the far corner reveals
// the familiar hall; no scene reload, fade, input reset or encounter action is involved.
[AddComponentMenu("Graduation Project/同じホールへ戻る通路")]
public sealed class HallwayLoop : MonoBehaviour
{
    [Header("対称な通路の接続面（青軸が進入方向）")]
    [SerializeField] private Transform entrancePlane;
    [SerializeField] private Transform returnPlane;
    [SerializeField] private CharacterController player;
    [SerializeField, Min(.1f)] private float halfWidth = .9f;
    [SerializeField, Min(.1f)] private float passageHeight = 2.8f;
    private Vector3 previous;
    private bool hasPrevious;
    public int Traversals { get; private set; }

    private void LateUpdate()
    {
        if (player == null || entrancePlane == null || returnPlane == null) return;
        Vector3 current = entrancePlane.InverseTransformPoint(player.transform.position);
        if (!hasPrevious || DemoSession.BlocksGameplay || !player.enabled || Time.deltaTime <= 0)
        { previous = current; hasPrevious = true; return; }

        // Segment/plane intersection handles a sprint and a long frame without relying
        // on a thin trigger. Reject external respawns and crossings outside this passage.
        if (previous.z < 0 && current.z >= 0 && Vector3.Distance(previous, current) < 2f)
        {
            float t = -previous.z / (current.z - previous.z);
            Vector3 crossing = Vector3.Lerp(previous, current, t);
            if (Mathf.Abs(crossing.x) <= halfWidth && crossing.y >= 0 && crossing.y <= passageHeight)
            {
                var rotation = returnPlane.rotation * Quaternion.Inverse(entrancePlane.rotation);
                player.enabled = false;
                player.transform.SetPositionAndRotation(returnPlane.TransformPoint(current), rotation * player.transform.rotation);
                player.enabled = true;
                // Preserve PlayerLook's local pitch and gravity; resetting it would snap the view.
                Physics.SyncTransforms();
                Traversals++;
                current = entrancePlane.InverseTransformPoint(player.transform.position);
            }
        }
        previous = current;
    }

    private void OnDisable() => hasPrevious = false;
    private void OnDrawGizmosSelected()
    {
        if (entrancePlane == null) return;
        Gizmos.color = Color.cyan; Gizmos.matrix = entrancePlane.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0, passageHeight / 2, 0), new Vector3(halfWidth * 2, passageHeight, .03f));
    }
}
