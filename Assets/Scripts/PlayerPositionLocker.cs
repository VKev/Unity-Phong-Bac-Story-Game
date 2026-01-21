using UnityEngine;

public class PlayerPositionLocker : MonoBehaviour
{
    private Rigidbody rb;
    private CharacterController cc;
    private RigidbodyConstraints originalConstraints;
    private bool originalConstraintsStored;
    private bool originalIsKinematic;
    private bool originalCcEnabled;
    private bool hasCcState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CharacterController>();
        if (rb != null)
        {
            originalConstraints = rb.constraints;
            originalIsKinematic = rb.isKinematic;
            originalConstraintsStored = true;
        }

        if (cc != null)
        {
            originalCcEnabled = cc.enabled;
            hasCcState = true;
        }
    }

    public void LockPosition()
    {
        if (rb != null)
        {
            if (!originalConstraintsStored)
            {
                originalConstraints = rb.constraints;
                originalIsKinematic = rb.isKinematic;
                originalConstraintsStored = true;
            }

            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (cc != null)
        {
            if (!hasCcState)
            {
                originalCcEnabled = cc.enabled;
                hasCcState = true;
            }

            cc.enabled = false;
        }
    }

    public void UnlockPosition()
    {
        if (rb != null && originalConstraintsStored)
        {
            rb.constraints = originalConstraints;
            rb.isKinematic = originalIsKinematic;
        }

        if (cc != null && hasCcState)
        {
            cc.enabled = originalCcEnabled;
        }
    }
}
