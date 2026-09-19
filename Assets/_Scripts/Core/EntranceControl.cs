using UnityEngine;

[RequireComponent(typeof(Interactable))]
public sealed class EntranceControl : MonoBehaviour
{
    public enum Kind { Mailbox, Key }
    [SerializeField] private EntranceAccessController entrance;
    [SerializeField] private Kind kind;
    [SerializeField] private string symbol;
    internal bool Available => entrance != null && entrance.CanUse(kind);
    internal bool IsKey => kind == Kind.Key;
    internal void Use()
    {
        if (!Available) return;
        if (kind == Kind.Mailbox) entrance.InspectMailbox();
        else entrance.Press(symbol);
    }
}
