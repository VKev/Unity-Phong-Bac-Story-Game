using UnityEngine;

public partial class InteractionUITrigger : MonoBehaviour
{
    private bool IsAudioItemAllowed(InteractAudioItem item)
    {
        if (item == null || interactAudioClips == null)
            return true;

        int index = interactAudioClips.IndexOf(item);
        if (index < 0)
            return true;

        InteractionItemGate[] gates = GetComponents<InteractionItemGate>();
        if (gates == null || gates.Length == 0)
            return true;

        for (int i = 0; i < gates.Length; i++)
        {
            InteractionItemGate gate = gates[i];
            if (gate == null)
                continue;

            if (!gate.MatchesIndex(index))
                continue;

            if (!gate.AreRequirementsMet())
                return false;
        }

        return true;
    }
}
