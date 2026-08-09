using UnityEngine;

public class ResponseEvaluator : MonoBehaviour
{
    public BeatOutcome Evaluate(BeatDefinition beatDefinition, PlayerAction action, int floorNumber)
    {
        if (beatDefinition == null)
        {
            return BeatOutcome.WrongRevealed;
        }

        if (beatDefinition.Category == AnomalyCategory.Normal && action == PlayerAction.PressClose)
        {
            return BeatOutcome.Represented;
        }

        if (action == PlayerAction.None)
        {
            return BeatOutcome.WrongRevealed;
        }

        if (action != beatDefinition.CorrectAction)
        {
            return BeatOutcome.WrongRevealed;
        }

        if (beatDefinition.CorrectFloorNumber >= 0 && floorNumber != beatDefinition.CorrectFloorNumber)
        {
            return BeatOutcome.WrongRevealed;
        }

        return BeatOutcome.Correct;
    }
}
