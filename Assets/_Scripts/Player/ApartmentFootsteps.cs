using UnityEngine;

// Distance-driven steps: turning in place or walking into a wall must remain silent.
[RequireComponent(typeof(CharacterController), typeof(PlayerLook))]
public sealed class ApartmentFootsteps : MonoBehaviour
{
    [SerializeField] private AudioClip[] steps;
    [SerializeField] private AudioClip ventilation;
    private ElevatorTuning tuning;
    private CharacterController body;
    private PlayerLook movement;
    private AudioSource foot, room;
    private Vector3 previous;
    private float distance;
    private int index;

    private void Awake()
    {
        tuning=FindFirstObjectByType<ElevatorTuning>();
        body=GetComponent<CharacterController>(); movement=GetComponent<PlayerLook>();
        foot=gameObject.AddComponent<AudioSource>(); foot.playOnAwake=false;
        foot.volume=.38f; foot.spatialBlend=0;
        room=gameObject.AddComponent<AudioSource>(); room.playOnAwake=false;
        room.clip=ventilation; room.loop=true; room.volume=.3f; room.spatialBlend=0;
        if(ventilation!=null)room.Play();
        previous=transform.position;
    }

    private void LateUpdate()
    {
        if(tuning!=null) { foot.volume=tuning.足音; room.volume=tuning.換気音; }
        Vector3 delta=transform.position-previous;previous=transform.position;delta.y=0;
        float moved=delta.magnitude;
        if(DemoSession.BlocksGameplay || !movement.enabled || !body.isGrounded || moved>1.5f)
        { distance=0;return; }
        if(moved<.0001f)return;
        distance+=moved;
        if(distance<1.1f || steps==null || steps.Length==0)return;
        distance%=1.1f;
        var clip=steps[index%steps.Length];index++;
        foot.pitch=(index%2==0)?1.025f:.975f;
        if(clip!=null)foot.PlayOneShot(clip);
    }
}
