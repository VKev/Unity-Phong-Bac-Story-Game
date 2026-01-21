using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;

public enum PanelAction
{
    Transparent,
    Sound,
    Text,
    SwitchScene,
    Black,
    LockPlayerCamera,
    UnlockPlayerCamera,
    RotatePlayerCamera,
    RotateCameraToTarget,
    LockPlayerPosition,
    UnlockPlayerPosition,
    AfterCredit
}

[Serializable]
public class PanelEvent
{
    public string eventId = string.Empty;
    public float timeStart = 0f;
    public bool useTimeStartEventId = false;
    public string timeStartEventId = string.Empty;
    public float timeStartEventDelay = 0f;
    public float transitionDelay = 0f;
    public bool useStartVoiceId = false;
    public int startVoiceId = 0;
    public bool useStartChoiceId = false;
    public int startChoiceId = 0;
    public float transitionTime = 0.5f;
    public PanelAction action = PanelAction.Transparent;
    public AudioClip audioClip;
    public AudioMixerGroup audioMixerGroup;
    public float transitionInTime = 0.5f;
    public float displayTime = 1f;
    public bool useDisplayTimeEventId = false;
    public string displayTimeEventId = string.Empty;
    public float transitionOutTime = 0.5f;
    public TMP_Text textTarget;
    public string textContent = string.Empty;
    public string sceneName = string.Empty;
    public CinemachineCamera cinemachineCamera;
    public bool lockPlayerCamera = true;
    public Vector2 rotatePanTilt = Vector2.zero;
    public Transform rotateTarget;
    public GameObject afterCreditPanel;
    public TMP_Text afterCreditText;
    public float afterCreditTextMoveSpeed = 10f;
    public float afterCreditMoveDuration = 0f;
    public Transform playerTransform;
}
