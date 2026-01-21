using System.Linq;
using UnityEngine;

/// <summary>
/// Gates an InteractionUITrigger audio item (by index in its list) until all linked ItemSelectInteraction
/// components have been selected (pressed F). Attach to the same GameObject as InteractionUITrigger.
/// </summary>
[RequireComponent(typeof(InteractionUITrigger))]
public class InteractionItemGate : MonoBehaviour
{
    [SerializeField] private int targetItemIndex = 0;
    [SerializeField] private ItemSelectInteraction[] requiredSelections = new ItemSelectInteraction[2];

    public int TargetIndex => targetItemIndex;

    public bool MatchesIndex(int itemIndex)
    {
        if (targetItemIndex < 0)
            return itemIndex == targetItemIndex;

        // Accept either zero-based (Element #) or one-based (user-entered #) indexing.
        return itemIndex == targetItemIndex || itemIndex + 1 == targetItemIndex;
    }

    public bool AreRequirementsMet()
    {
        if (requiredSelections == null || requiredSelections.Length == 0)
            return true;

        return requiredSelections.All(sel => sel == null || sel.HasSelected);
    }
}
