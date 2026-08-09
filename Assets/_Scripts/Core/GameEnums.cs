public enum BeatState
{
    Travel,
    Arrive,
    Diagnosis,
    Committed,
    Reveal,
    Grace,
    Resolve,
    Depart
}

public enum AnomalyCategory
{
    Lure,
    Provocation,
    Hijack,
    Normal
}

public enum PlayerAction
{
    PressFloor,
    PressClose,
    PressOpen,
    PressEmergencyStop,
    ExitCab,
    TouchHomeDoor,
    None
}

public enum BeatOutcome
{
    Correct,
    Represented,
    WrongRevealed,
    GraceRecovered,
    Death,
    RunClear
}
