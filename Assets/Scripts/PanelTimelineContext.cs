using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelTimelineContext
{
    private readonly Image panel;
    private readonly AudioSource audioSource;
    private readonly IDictionary<string, bool> eventStates;

    public PanelTimelineContext(Image panel, AudioSource audioSource, IDictionary<string, bool> eventStates)
    {
        this.panel = panel;
        this.audioSource = audioSource;
        this.eventStates = eventStates;
    }

    public IEnumerator FadeBlackToTransparent(float duration)
    {
        if (panel == null)
            yield break;

        Color start = Color.black;
        Color end = new Color(0f, 0f, 0f, 0f);
        panel.color = start;

        if (duration <= 0f)
        {
            panel.color = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panel.color = Color.Lerp(start, end, t);
            yield return null;
        }

        panel.color = end;
    }

    public IEnumerator FadeTransparentToBlack(float duration)
    {
        if (panel == null)
            yield break;

        Color start = new Color(0f, 0f, 0f, 0f);
        Color end = Color.black;
        panel.color = start;

        if (duration <= 0f)
        {
            panel.color = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panel.color = Color.Lerp(start, end, t);
            yield return null;
        }

        panel.color = end;
    }

    public IEnumerator PlaySound(AudioClip clip, UnityEngine.Audio.AudioMixerGroup mixerGroup)
    {
        if (audioSource == null || clip == null)
            yield break;

        AudioSource tempSource = CreateTempSource(audioSource);
        if (tempSource == null)
            yield break;

        if (mixerGroup != null)
            tempSource.outputAudioMixerGroup = mixerGroup;

        tempSource.clip = clip;
        tempSource.loop = false;
        tempSource.Play();
        yield return DestroyAfterPlay(tempSource, clip.length);
    }

    public IEnumerator RunTextEvent(PanelEvent panelEvent)
    {
        if (panelEvent == null || panelEvent.textTarget == null)
            yield break;

        TMP_Text textTarget = panelEvent.textTarget;
        textTarget.text = panelEvent.textContent ?? string.Empty;

        Color baseColor = textTarget.color;
        Color transparent = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        Color opaque = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        textTarget.color = transparent;

        if (panelEvent.transitionInTime > 0f)
            yield return FadeText(textTarget, transparent, opaque, panelEvent.transitionInTime);
        else
            textTarget.color = opaque;

        yield return WaitForDisplayTime(panelEvent);

        if (panelEvent.transitionOutTime > 0f)
            yield return FadeText(textTarget, opaque, transparent, panelEvent.transitionOutTime);
        else
            textTarget.color = transparent;
    }

    private IEnumerator WaitForDisplayTime(PanelEvent panelEvent)
    {
        if (panelEvent == null)
            yield break;

        if (panelEvent.useDisplayTimeEventId)
        {
            string displayId = PanelTimelineIdUtility.NormalizeId(panelEvent.displayTimeEventId);
            if (!string.IsNullOrEmpty(displayId))
            {
                string currentId = PanelTimelineIdUtility.NormalizeId(panelEvent.eventId);
                if (displayId == currentId)
                {
                    Debug.LogWarning("PanelTimelineController: displayTimeEventId matches its own eventId. Skipping display time.");
                    yield break;
                }

                if (eventStates != null && eventStates.ContainsKey(displayId))
                {
                    yield return new WaitUntil(() => eventStates.TryGetValue(displayId, out bool done) && done);
                    yield break;
                }

                Debug.LogWarning($"PanelTimelineController: displayTimeEventId '{panelEvent.displayTimeEventId}' not found. Skipping display time.");
                yield break;
            }

            Debug.LogWarning("PanelTimelineController: displayTimeEventId is empty. Skipping display time.");
            yield break;
        }

        if (panelEvent.displayTime > 0f)
            yield return new WaitForSeconds(panelEvent.displayTime);
    }

    private IEnumerator FadeText(TMP_Text textTarget, Color from, Color to, float duration)
    {
        if (textTarget == null)
            yield break;

        if (duration <= 0f)
        {
            textTarget.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            textTarget.color = Color.Lerp(from, to, t);
            yield return null;
        }

        textTarget.color = to;
    }

    private AudioSource CreateTempSource(AudioSource template)
    {
        if (template == null)
            return null;

        GameObject tempObject = new GameObject("TempAudioSource");
        Transform templateTransform = template.transform;
        tempObject.transform.position = templateTransform.position;
        tempObject.transform.rotation = templateTransform.rotation;
        tempObject.transform.SetParent(templateTransform.parent, true);

        AudioSource tempSource = tempObject.AddComponent<AudioSource>();
        CopyAudioSourceSettings(template, tempSource);
        return tempSource;
    }

    private IEnumerator DestroyAfterPlay(AudioSource source, float clipLength)
    {
        if (source == null)
            yield break;

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        float waitTime = clipLength / safePitch;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        if (source != null)
            Object.Destroy(source.gameObject);
    }

    private void CopyAudioSourceSettings(AudioSource source, AudioSource target)
    {
        if (source == null || target == null)
            return;

        target.outputAudioMixerGroup = source.outputAudioMixerGroup;
        target.mute = source.mute;
        target.bypassEffects = source.bypassEffects;
        target.bypassListenerEffects = source.bypassListenerEffects;
        target.bypassReverbZones = source.bypassReverbZones;
        target.playOnAwake = source.playOnAwake;
        target.loop = source.loop;
        target.priority = source.priority;
        target.volume = source.volume;
        target.pitch = source.pitch;
        target.panStereo = source.panStereo;
        target.spatialBlend = source.spatialBlend;
        target.reverbZoneMix = source.reverbZoneMix;
        target.dopplerLevel = source.dopplerLevel;
        target.spread = source.spread;
        target.rolloffMode = source.rolloffMode;
        target.minDistance = source.minDistance;
        target.maxDistance = source.maxDistance;
        target.spatialize = source.spatialize;
        target.spatializePostEffects = source.spatializePostEffects;
        target.ignoreListenerVolume = source.ignoreListenerVolume;
        target.ignoreListenerPause = source.ignoreListenerPause;
        target.velocityUpdateMode = source.velocityUpdateMode;
    }
}
