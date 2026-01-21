using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IPanelTimelineActionHandler
{
    IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent);
}

public sealed class TransparentActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (context == null || panelEvent == null)
            yield break;

        if (panelEvent.transitionDelay > 0f)
            yield return new WaitForSeconds(panelEvent.transitionDelay);

        yield return context.FadeBlackToTransparent(panelEvent.transitionTime);
    }
}

public sealed class BlackActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (context == null || panelEvent == null)
            yield break;

        if (panelEvent.transitionDelay > 0f)
            yield return new WaitForSeconds(panelEvent.transitionDelay);

        yield return context.FadeTransparentToBlack(panelEvent.transitionTime);
    }
}

public sealed class SoundActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (context == null)
            yield break;

        yield return context.PlaySound(panelEvent.audioClip, panelEvent.audioMixerGroup);
    }
}

public sealed class TextActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (context == null)
            yield break;

        yield return context.RunTextEvent(panelEvent);
    }
}

public sealed class SwitchSceneActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        string sceneName = panelEvent.sceneName;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("PanelTimelineController: SwitchScene action missing scene name.");
            yield break;
        }

        if (!PanelTimelineController.TryLoadScene(sceneName.Trim(), "SwitchScene action"))
            yield break;
    }
}

public sealed class LockPlayerCameraActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        CinemachineCamera camera = panelEvent.cinemachineCamera;
        if (camera == null)
        {
            Debug.LogWarning("PanelTimelineController: LockPlayerCamera action missing CinemachineCamera.");
            yield break;
        }

        CinemachineInputAxisController inputController = camera.GetComponent<CinemachineInputAxisController>();
        if (inputController == null)
            inputController = camera.GetComponentInChildren<CinemachineInputAxisController>(true);
        if (inputController == null)
        {
            Debug.LogWarning("PanelTimelineController: LockPlayerCamera action missing CinemachineInputAxisController.");
            yield break;
        }

        inputController.enabled = !panelEvent.lockPlayerCamera;
    }
}

public sealed class UnlockPlayerCameraActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        CinemachineCamera camera = panelEvent.cinemachineCamera;
        if (camera == null)
        {
            Debug.LogWarning("PanelTimelineController: UnlockPlayerCamera action missing CinemachineCamera.");
            yield break;
        }

        CinemachineInputAxisController inputController = camera.GetComponent<CinemachineInputAxisController>();
        if (inputController == null)
            inputController = camera.GetComponentInChildren<CinemachineInputAxisController>(true);

        if (inputController == null)
        {
            Debug.LogWarning("PanelTimelineController: UnlockPlayerCamera action missing CinemachineInputAxisController.");
            yield break;
        }

        inputController.enabled = true;
    }
}

public sealed class RotatePlayerCameraActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        CinemachineCamera camera = panelEvent.cinemachineCamera;
        if (camera == null)
        {
            Debug.LogWarning("PanelTimelineController: RotatePlayerCamera action missing CinemachineCamera.");
            yield break;
        }

        CinemachinePanTilt panTilt = camera.GetComponent<CinemachinePanTilt>();
        if (panTilt == null)
            panTilt = camera.GetComponentInChildren<CinemachinePanTilt>(true);
        if (panTilt == null)
        {
            Debug.LogWarning("PanelTimelineController: RotatePlayerCamera action missing CinemachinePanTilt.");
            yield break;
        }

        Vector2 target = panelEvent.rotatePanTilt;
        float duration = Mathf.Max(0f, panelEvent.transitionTime);

        InputAxis panAxis = panTilt.PanAxis;
        InputAxis tiltAxis = panTilt.TiltAxis;

        float startPan = panAxis.Value;
        float startTilt = tiltAxis.Value;
        float targetPan = panAxis.ClampValue(target.x);
        float targetTilt = tiltAxis.ClampValue(target.y);

        if (duration <= 0f)
        {
            panAxis.Value = targetPan;
            tiltAxis.Value = targetTilt;
            panTilt.PanAxis = panAxis;
            panTilt.TiltAxis = tiltAxis;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float panValue = Mathf.LerpAngle(startPan, targetPan, t);
            float tiltValue = Mathf.Lerp(startTilt, targetTilt, t);

            panAxis.Value = panAxis.ClampValue(panValue);
            tiltAxis.Value = tiltAxis.ClampValue(tiltValue);
            panTilt.PanAxis = panAxis;
            panTilt.TiltAxis = tiltAxis;

            yield return null;
        }

        panAxis.Value = targetPan;
        tiltAxis.Value = targetTilt;
        panTilt.PanAxis = panAxis;
        panTilt.TiltAxis = tiltAxis;
    }
}

public sealed class RotateCameraToTargetActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        CinemachineCamera camera = panelEvent.cinemachineCamera;
        Transform target = panelEvent.rotateTarget;
        if (camera == null || target == null)
        {
            Debug.LogWarning("PanelTimelineController: RotateCameraToTarget action missing camera or target.");
            yield break;
        }

        CinemachinePanTilt panTilt = camera.GetComponent<CinemachinePanTilt>();
        if (panTilt == null)
            panTilt = camera.GetComponentInChildren<CinemachinePanTilt>(true);
        if (panTilt == null)
        {
            Debug.LogWarning("PanelTimelineController: RotateCameraToTarget action missing CinemachinePanTilt.");
            yield break;
        }

        Vector3 toTarget = target.position - camera.transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            yield break;

        Vector3 dir = toTarget.normalized;
        float desiredPan = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float desiredTilt = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;

        InputAxis panAxis = panTilt.PanAxis;
        InputAxis tiltAxis = panTilt.TiltAxis;

        desiredPan = panAxis.ClampValue(desiredPan);
        desiredTilt = tiltAxis.ClampValue(desiredTilt);

        float duration = Mathf.Max(0f, panelEvent.transitionTime);
        float startPan = panAxis.Value;
        float startTilt = tiltAxis.Value;

        if (duration <= 0f)
        {
            panAxis.Value = desiredPan;
            tiltAxis.Value = desiredTilt;
            panTilt.PanAxis = panAxis;
            panTilt.TiltAxis = tiltAxis;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float panValue = Mathf.LerpAngle(startPan, desiredPan, t);
            float tiltValue = Mathf.Lerp(startTilt, desiredTilt, t);

            panAxis.Value = panAxis.ClampValue(panValue);
            tiltAxis.Value = tiltAxis.ClampValue(tiltValue);
            panTilt.PanAxis = panAxis;
            panTilt.TiltAxis = tiltAxis;

            yield return null;
        }

        panAxis.Value = desiredPan;
        tiltAxis.Value = desiredTilt;
        panTilt.PanAxis = panAxis;
        panTilt.TiltAxis = tiltAxis;
    }
}

public sealed class LockPlayerPositionActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        Transform target = panelEvent != null ? panelEvent.playerTransform : null;
        if (target == null)
        {
            Debug.LogWarning("PanelTimelineController: LockPlayerPosition action missing playerTransform.");
            yield break;
        }

        PlayerPositionLocker locker = target.GetComponent<PlayerPositionLocker>();
        if (locker == null)
            locker = target.gameObject.AddComponent<PlayerPositionLocker>();

        locker.LockPosition();
    }
}

public sealed class UnlockPlayerPositionActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        Transform target = panelEvent != null ? panelEvent.playerTransform : null;
        if (target == null)
        {
            Debug.LogWarning("PanelTimelineController: UnlockPlayerPosition action missing playerTransform.");
            yield break;
        }

        PlayerPositionLocker locker = target.GetComponent<PlayerPositionLocker>();
        if (locker == null)
            yield break;

        locker.UnlockPosition();
    }
}

public sealed class AfterCreditActionHandler : IPanelTimelineActionHandler
{
    public IEnumerator Execute(PanelTimelineContext context, PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        GameObject panelObject = panelEvent.afterCreditPanel;
        if (panelObject == null)
        {
            Debug.LogWarning("PanelTimelineController: AfterCredit action missing afterCreditPanel.");
            yield break;
        }

        if (panelEvent.transitionDelay > 0f)
            yield return new WaitForSeconds(panelEvent.transitionDelay);

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = panelObject.AddComponent<CanvasGroup>();

        panelObject.SetActive(true);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        float fadeDuration = Mathf.Max(0f, panelEvent.transitionTime);
        if (fadeDuration <= 0f)
        {
            group.alpha = 1f;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                group.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            group.alpha = 1f;
        }

        group.interactable = true;
        group.blocksRaycasts = true;

        TMP_Text creditText = panelEvent.afterCreditText;
        if (creditText == null)
            yield break;

        float speed = panelEvent.afterCreditTextMoveSpeed;
        if (Mathf.Approximately(speed, 0f))
            yield break;

        RectTransform rectTransform = creditText.rectTransform;
        Vector2 startAnchored = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        Vector3 startLocal = creditText.transform.localPosition;

        float moveDuration = panelEvent.afterCreditMoveDuration;
        float elapsedMove = 0f;

        if (moveDuration <= 0f)
        {
            while (true)
            {
                elapsedMove += Time.deltaTime;
                float offset = speed * elapsedMove;
                if (rectTransform != null)
                    rectTransform.anchoredPosition = startAnchored + Vector2.up * offset;
                else
                    creditText.transform.localPosition = startLocal + Vector3.up * offset;
                yield return null;
            }
        }
        else
        {
            while (elapsedMove < moveDuration)
            {
                elapsedMove += Time.deltaTime;
                float offset = speed * elapsedMove;
                if (rectTransform != null)
                    rectTransform.anchoredPosition = startAnchored + Vector2.up * offset;
                else
                    creditText.transform.localPosition = startLocal + Vector3.up * offset;
                yield return null;
            }
        }
    }
}
