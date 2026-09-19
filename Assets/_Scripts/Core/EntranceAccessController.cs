using System.Collections;
using TMPro;
using UnityEngine;

// The first visit teaches the home number through the postbox and the entrance keypad.
public sealed class EntranceAccessController : MonoBehaviour
{
    [Header("入口の設定")]
    [SerializeField] private string roomNumber = "805";
    [SerializeField] private TMP_Text display;
    [SerializeField] private Transform leftDoor, rightDoor, mailboxLid;
    [SerializeField, Min(.1f)] private float doorSeconds = 1.6f;
    [SerializeField, Min(0f)] private float doorTravel = 1.08f;
    [SerializeField, Range(0, 1)] private float keyVolume = .035f;
    [SerializeField] private AudioClip keySound;
    [SerializeField] private AudioClip doorSound;
    [SerializeField, Range(0, 1)] private float doorVolume = .025f;
    private AudioSource audioSource;
    private AudioClip generatedKeySound;
    private string entered = "";
    private bool rejecting;
    public bool MailboxInspected { get; private set; }
    public bool Unlocked { get; private set; }
    public bool DoorOpen { get; private set; }
    public string EnteredNumber => entered;

    private void Awake()
    {
        var soundObject = new GameObject("オートロック操作音");
        soundObject.transform.SetParent(transform, false);
        if (display != null) soundObject.transform.position = display.transform.position;
        audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = 1;
        audioSource.minDistance = 1; audioSource.maxDistance = 5;
        if (keySound == null)
        {
            // Quiet equipment acknowledgement, with a short ramp to avoid a click at each end.
            const int rate = 22050, count = 1543;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = Mathf.Sin(2 * Mathf.PI * 1100 * i / rate) *
                    Mathf.Min(1, i / 150f, (count - 1 - i) / 250f) * .4f;
            generatedKeySound = AudioClip.Create("EntranceKeyAcknowledgement", count, 1, rate, false);
            generatedKeySound.SetData(samples, 0); keySound = generatedKeySound;
        }
        Draw("---", new Color(.55f, .67f, .64f));
    }

    internal bool CanUse(EntranceControl.Kind kind) => !Unlocked &&
        (kind == EntranceControl.Kind.Mailbox ? !MailboxInspected : MailboxInspected && !rejecting);

    internal void InspectMailbox()
    {
        if (!CanUse(EntranceControl.Kind.Mailbox)) return;
        MailboxInspected = true;
        Draw("---", new Color(.82f, .96f, .84f));
        StartCoroutine(OpenMailbox());
    }

    private IEnumerator OpenMailbox()
    {
        if (mailboxLid == null) yield break;
        var start = mailboxLid.localRotation;
        float time = 0;
        while (time < .45f)
        {
            time += Time.deltaTime;
            mailboxLid.localRotation = start * Quaternion.Euler(0, 55 * Mathf.SmoothStep(0, 1, time / .45f), 0);
            yield return null;
        }
    }

    internal void Press(string symbol)
    {
        if (!CanUse(EntranceControl.Kind.Key)) return;
        if (keySound != null) audioSource.PlayOneShot(keySound, keyVolume);
        if (symbol == "C") { entered = ""; Draw("---", Color.white); return; }
        if (symbol.Length != 1 || symbol[0] < '0' || symbol[0] > '9') return;
        entered += symbol;
        Draw(entered.PadRight(roomNumber.Length, '-'), Color.white);
        if (entered.Length < roomNumber.Length) return;
        if (entered == roomNumber)
        {
            Unlocked = true;
            Draw(roomNumber, new Color(.64f, 1, .72f));
            StartCoroutine(OpenDoors());
        }
        else StartCoroutine(Reject());
    }

    private IEnumerator Reject()
    {
        rejecting = true; Draw(entered, new Color(1, .45f, .35f));
        yield return new WaitForSeconds(.65f);
        entered = ""; rejecting = false; Draw("---", Color.white);
    }

    private IEnumerator OpenDoors()
    {
        if (doorSound != null) audioSource.PlayOneShot(doorSound, doorVolume);
        Vector3 left = leftDoor.localPosition, right = rightDoor.localPosition;
        float time = 0;
        while (time < doorSeconds)
        {
            time += Time.deltaTime;
            float amount = Mathf.SmoothStep(0, 1, time / doorSeconds) * doorTravel;
            leftDoor.localPosition = left + Vector3.left * amount;
            rightDoor.localPosition = right + Vector3.right * amount;
            yield return null;
        }
        DoorOpen = true;
    }

    private void Draw(string value, Color color)
    {
        if (display == null) return;
        display.text = value; display.color = color;
    }
    private void OnDestroy()
    {
        if (generatedKeySound != null) Destroy(generatedKeySound);
    }
}
