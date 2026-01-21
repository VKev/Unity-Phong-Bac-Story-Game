using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class InteractionUITrigger : MonoBehaviour
{
    [System.Serializable]
    public class InteractAudioItem
    {
        public int voiceId = 0;
        public bool useChoiceCondition = false;
        public int requiredChoiceIndex = 0;
        public bool multiChoice = false;
        public string title = string.Empty;
        public TMP_Text titlePrefab;
        public string titlePrefabTag = "ChoiceTitle";
        public List<ChoiceItem> choices = new List<ChoiceItem>();
        public bool autoPlay = false;
        public float delay = 0f;
        public TMP_Text subtitleTarget;
        public string subtitleText = string.Empty;
        public bool clearSubtitleOnEnd = true;
        public AudioClip clip;
        public UnityEngine.Audio.AudioMixerGroup mixerGroup;
    }

    [SerializeField] private List<InteractAudioItem> interactAudioClips = new List<InteractAudioItem>();

    private Coroutine destroyAudioRoutine;
    private Coroutine pendingPlayRoutine;
    private AudioSource currentAudioSource;
    private InteractAudioItem currentAudioItem;
    private InteractAudioItem pendingItem;
    private bool isAudioPlaying;

    private InteractAudioItem FindMultiChoiceItem()
    {
        if (interactAudioClips == null || interactAudioClips.Count == 0)
            return null;

        int currentId = PanelTimelineController.GlobalVoiceId;
        InteractAudioItem fallback = null;
        foreach (InteractAudioItem item in interactAudioClips)
        {
            if (item == null || !item.multiChoice || item.voiceId != currentId)
                continue;

            if (fallback == null)
                fallback = item;

            if (item.choices != null && item.choices.Count > 0)
                return item;
        }

        return fallback;
    }

    private InteractAudioItem FindMatchingItem()
    {
        if (interactAudioClips == null || interactAudioClips.Count == 0)
            return null;

        int currentId = PanelTimelineController.GlobalVoiceId;
        int currentChoice = GlobalChoiceIndex;
        InteractAudioItem defaultMatch = null;
        foreach (InteractAudioItem item in interactAudioClips)
        {
            if (item == null || item.voiceId != currentId || item.multiChoice)
                continue;

            if (item.useChoiceCondition)
            {
                if (currentChoice == item.requiredChoiceIndex)
                    return item;
                continue;
            }

            if (defaultMatch == null)
                defaultMatch = item;
        }

        return defaultMatch;
    }

    private void TrySchedulePlay(InteractAudioItem item)
    {
        if (item == null || item.clip == null)
            return;

        if (isAudioPlaying)
            return;

        if (PanelTimelineController.GlobalVoiceId != item.voiceId)
            return;

        if (pendingItem == item)
            return;

        ClearPendingPlay();

        if (item.delay > 0f)
        {
            pendingItem = item;
            pendingPlayRoutine = StartCoroutine(PlayAfterDelay(item));
            return;
        }

        PlayItem(item);
    }

    private IEnumerator PlayAfterDelay(InteractAudioItem item)
    {
        if (item == null)
            yield break;

        if (item.delay > 0f)
            yield return new WaitForSeconds(item.delay);

        pendingPlayRoutine = null;

        if (item == null || item.clip == null)
        {
            pendingItem = null;
            yield break;
        }

        if (isAudioPlaying)
        {
            pendingItem = null;
            yield break;
        }

        if (!item.autoPlay && !playerInRange)
        {
            pendingItem = null;
            yield break;
        }

        if (PanelTimelineController.GlobalVoiceId != item.voiceId)
        {
            pendingItem = null;
            yield break;
        }

        pendingItem = null;
        PlayItem(item);
    }

    private void PlayItem(InteractAudioItem item)
    {
        if (item == null || item.clip == null)
            return;

        currentAudioSource = SpawnAudioSource(item.clip, item.mixerGroup);
        if (currentAudioSource != null)
        {
            currentAudioItem = item;
            ApplySubtitle(item);
            isAudioPlaying = true;
            if (!item.multiChoice)
                SetNameTagHighlighted(true);
            SetVisible(false);
            destroyAudioRoutine = StartCoroutine(DestroyAfterPlay(currentAudioSource));
        }
    }

    private void ClearPendingPlay()
    {
        ClearPendingPlay(false);
    }

    private void ClearPendingPlay(bool keepAutoPlay)
    {
        if (pendingPlayRoutine != null)
        {
            if (keepAutoPlay && pendingItem != null && pendingItem.autoPlay)
                return;

            StopCoroutine(pendingPlayRoutine);
            pendingPlayRoutine = null;
        }

        pendingItem = null;
    }

    private AudioSource SpawnAudioSource(AudioClip clip, UnityEngine.Audio.AudioMixerGroup mixerGroup)
    {
        GameObject audioObject = new GameObject("InteractAudio");
        audioObject.transform.SetParent(transform, false);
        audioObject.transform.localPosition = Vector3.zero;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        if (mixerGroup != null)
            source.outputAudioMixerGroup = mixerGroup;
        source.clip = clip;
        source.Play();
        return source;
    }

    private void StopCurrentAudio()
    {
        if (destroyAudioRoutine != null)
        {
            StopCoroutine(destroyAudioRoutine);
            destroyAudioRoutine = null;
        }

        if (currentAudioSource == null)
            return;

        AudioSource source = currentAudioSource;
        currentAudioSource = null;
        isAudioPlaying = false;
        SetNameTagHighlighted(false);
        ClearSubtitle(currentAudioItem);
        currentAudioItem = null;
        source.Stop();
        Destroy(source.gameObject);
        PanelTimelineController.AdvanceGlobalVoiceId();

        if (playerInRange)
            UpdateState();
        else
            SetVisible(false);
    }

    private IEnumerator DestroyAfterPlay(AudioSource source)
    {
        if (source == null || source.clip == null)
            yield break;

        float waitTime = source.clip.length;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        if (source != null)
        {
            if (source == currentAudioSource)
                currentAudioSource = null;
            isAudioPlaying = false;
            SetNameTagHighlighted(false);
            destroyAudioRoutine = null;
            ClearSubtitle(currentAudioItem);
            currentAudioItem = null;
            PanelTimelineController.AdvanceGlobalVoiceId();
            if (playerInRange)
                UpdateState();
            else
                SetVisible(false);
            Destroy(source.gameObject);
        }
    }

    private void ApplySubtitle(InteractAudioItem item)
    {
        if (item == null || item.subtitleTarget == null)
            return;

        item.subtitleTarget.text = item.subtitleText ?? string.Empty;
        item.subtitleTarget.gameObject.SetActive(true);
    }

    private void ClearSubtitle(InteractAudioItem item)
    {
        if (item == null || item.subtitleTarget == null || !item.clearSubtitleOnEnd)
            return;

        item.subtitleTarget.text = string.Empty;
        item.subtitleTarget.gameObject.SetActive(false);
    }
}
