using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InteractionUITrigger : MonoBehaviour
{
    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private string nameTagPrefabTag = "NameTag";
    [SerializeField] private float nameTagExtraHeight = 0.25f;
    [SerializeField] private string nameTagText = string.Empty;
    [SerializeField] private float nameTagAudioThreshold = 0.01f;
    [SerializeField] private float nameTagAudioSensitivity = 10f;
    [SerializeField] private float nameTagFlashSpeed = 12f;
    [SerializeField] private float nameTagFlashReleaseSpeed = 4f;
    [SerializeField] private int nameTagSampleCount = 64;

    private Transform nameTagInstance;
    private TMP_Text nameTagLabel;
    private Graphic nameTagBorder;
    private Color nameTagBorderBaseColor = Color.white;
    private bool hasNameTagBorder;
    private bool isNameTagHighlighted;
    private bool nameTagAudioFlashEnabled;
    private float nameTagFlashIntensity;
    private float[] nameTagSamples;
    private bool warnedMissingNameTagPrefab;
    private bool warnedMissingNameTagTag;

    private void InitializeNameTag()
    {
        if (nameTagInstance != null)
            return;

        GameObject prefab = nameTagPrefab != null ? nameTagPrefab : FindNameTagPrefabByTag();
        if (prefab == null)
        {
            WarnMissingNameTagPrefab();
            return;
        }

        nameTagPrefab = prefab;
        Transform anchor = transform;
        GameObject instance = Instantiate(prefab);
        nameTagInstance = instance.transform;

        Vector3 worldPosition = GetNameTagWorldPosition(anchor);
        nameTagInstance.position = worldPosition;
        nameTagInstance.SetParent(anchor, true);

        CacheNameTagComponents(instance);
        ApplyNameTagText();
        SetNameTagHighlighted(false, true);
    }

    private void ApplyNameTagText()
    {
        if (nameTagLabel == null)
            return;

        string label = string.IsNullOrWhiteSpace(nameTagText) ? gameObject.name : nameTagText;
        nameTagLabel.text = label;
    }

    private void LateUpdate()
    {
        UpdateNameTagFacing();
        UpdateNameTagFlash();
    }

    private void UpdateNameTagFacing()
    {
        if (nameTagInstance == null)
            return;

        Camera cameraMain = Camera.main;
        if (cameraMain == null)
            return;

        Vector3 direction = cameraMain.transform.position - nameTagInstance.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        nameTagInstance.rotation = Quaternion.LookRotation(-direction, cameraMain.transform.up);
    }

    private void CacheNameTagComponents(GameObject instance)
    {
        if (instance == null)
            return;

        nameTagLabel = instance.GetComponentInChildren<TMP_Text>(true);

        Image[] images = instance.GetComponentsInChildren<Image>(true);
        if (images.Length > 0)
            nameTagBorder = images[0];
        else
            nameTagBorder = instance.GetComponentInChildren<Graphic>(true);

        if (nameTagBorder != null)
        {
            nameTagBorderBaseColor = nameTagBorder.color;
            hasNameTagBorder = true;
        }
    }

    private GameObject FindNameTagPrefabByTag()
    {
        if (string.IsNullOrWhiteSpace(nameTagPrefabTag))
            return null;

        try
        {
            return GameObject.FindGameObjectWithTag(nameTagPrefabTag);
        }
        catch (UnityException)
        {
            WarnMissingNameTagTag();
            return null;
        }
    }

    private Vector3 GetNameTagWorldPosition(Transform anchor)
    {
        Vector3 basePosition = anchor.position;

        Bounds bounds;
        if (TryGetNameTagBounds(anchor, out bounds))
            basePosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        basePosition.y += nameTagExtraHeight;
        return basePosition;
    }

    private bool TryGetNameTagBounds(Transform root, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds();

        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders2D.Length; i++)
        {
            Collider2D collider2D = colliders2D[i];
            if (collider2D == null)
                continue;

            if (!hasBounds)
            {
                bounds = collider2D.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider2D.bounds);
            }
        }

        return hasBounds;
    }

    private void SetNameTagHighlighted(bool isHighlighted, bool force = false)
    {
        if (!hasNameTagBorder || nameTagBorder == null)
            return;

        if (isNameTagHighlighted == isHighlighted && !force)
            return;

        isNameTagHighlighted = isHighlighted;
        nameTagAudioFlashEnabled = isHighlighted;

        if (!isHighlighted)
        {
            nameTagFlashIntensity = 0f;
            ApplyNameTagHighlightIntensity(0f);
        }
    }

    private void UpdateNameTagFlash()
    {
        if (!hasNameTagBorder || nameTagBorder == null)
            return;

        float targetIntensity = 0f;
        if (nameTagAudioFlashEnabled && isAudioPlaying && currentAudioSource != null && currentAudioItem != null && !currentAudioItem.multiChoice)
        {
            float rms = SampleAudioRms(currentAudioSource);
            if (rms > nameTagAudioThreshold)
                targetIntensity = Mathf.Clamp01((rms - nameTagAudioThreshold) * nameTagAudioSensitivity);
        }

        if (nameTagFlashSpeed <= 0f && nameTagFlashReleaseSpeed <= 0f)
        {
            nameTagFlashIntensity = targetIntensity;
        }
        else
        {
            float speed = targetIntensity > nameTagFlashIntensity
                ? Mathf.Max(0f, nameTagFlashSpeed)
                : Mathf.Max(0f, nameTagFlashReleaseSpeed);

            nameTagFlashIntensity = Mathf.MoveTowards(
                nameTagFlashIntensity,
                targetIntensity,
                speed * Time.deltaTime);
        }

        ApplyNameTagHighlightIntensity(nameTagFlashIntensity);
    }

    private float SampleAudioRms(AudioSource source)
    {
        if (source == null)
            return 0f;

        int sampleCount = Mathf.Clamp(nameTagSampleCount, 8, 512);
        if (nameTagSamples == null || nameTagSamples.Length != sampleCount)
            nameTagSamples = new float[sampleCount];

        source.GetOutputData(nameTagSamples, 0);
        float sum = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = nameTagSamples[i];
            sum += sample * sample;
        }

        return Mathf.Sqrt(sum / sampleCount);
    }

    private void ApplyNameTagHighlightIntensity(float intensity)
    {
        Color highlighted = Color.Lerp(nameTagBorderBaseColor, Color.white, Mathf.Clamp01(intensity));
        highlighted.a = nameTagBorderBaseColor.a;
        nameTagBorder.color = highlighted;
    }

    private void WarnMissingNameTagPrefab()
    {
        if (warnedMissingNameTagPrefab)
            return;

        warnedMissingNameTagPrefab = true;
        Debug.LogWarning("InteractionUITrigger: Missing name tag prefab reference or tagged prefab in scene.");
    }

    private void WarnMissingNameTagTag()
    {
        if (warnedMissingNameTagTag)
            return;

        warnedMissingNameTagTag = true;
        Debug.LogWarning($"InteractionUITrigger: Name tag tag '{nameTagPrefabTag}' is not defined.");
    }
}
