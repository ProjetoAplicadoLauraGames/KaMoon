using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Voz local com push-to-talk e auto-escuta para testar o microfone.
/// Usa a entrada predefinida do sistema em cada transmissão.
/// P1: V. P2: RB no comando, com B como alternativa no teclado.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LocalVoiceChat : MonoBehaviour
{
    [Header("Push-to-talk (Input System)")]
    [Tooltip("P1 usa teclado+rato.")]
    [SerializeField] private string talkBindingP1 = "<Keyboard>/v";
    [Tooltip("P2 usa comando (RB).")]
    [SerializeField] private string talkBindingP2 = "<Gamepad>/rightShoulder";
    [SerializeField] private string talkBindingP2Fallback = "<Keyboard>/b";

    [Header("Teste local")]
    [Tooltip("Reproduz a captura neste PC para testar o microfone. OFF desliga apenas a auto-escuta.")]
    [SerializeField] private bool selfMonitor = true;
    [Tooltip("Simula rádio. OFF mantém a voz sem o filtro.")]
    [SerializeField] private bool radioEffect = false;
    [SerializeField, Range(0f, 1f)] private float outputVolume = 0.7f;

    [Header("Microfone predefinido do sistema")]
    [SerializeField, Min(1)] private int clipSeconds = 1;

    [Header("UI")]
    [Tooltip("Mostra os dados da captura para diagnosticar o áudio.")]
    [SerializeField] private bool showDebugPanel = false;

    // null identifica o microfone predefinido em todas as APIs Microphone.
    // Não escolher devices[0]: pode ser um monitor, ou um micro antigo.
    private const string DefaultDevice = null;
    private const float StartupTimeout = 3f;
    private const float MonitorLatency = 0.05f;

    private AudioSource output;
    private AudioClip micClip;
    private InputAction talkActionP1;
    private InputAction talkActionP2;
    private Coroutine transmitRoutine;
    private int talkingPlayer;
    private string error;
    private bool restartPending;
    private float micLevel;
    private readonly float[] levelSamples = new float[256];

    private void Awake()
    {
        output = GetComponent<AudioSource>();
        output.playOnAwake = false;
        output.loop = true;
        output.volume = outputVolume;
        output.pitch = 1f;
        output.spatialBlend = 0f;

        AudioLowPassFilter filter = GetComponent<AudioLowPassFilter>();
        if (radioEffect && filter == null)
            filter = gameObject.AddComponent<AudioLowPassFilter>();
        if (filter != null)
        {
            filter.enabled = radioEffect;
            filter.cutoffFrequency = 2800f;
        }

        talkActionP1 = new InputAction("TalkP1", InputActionType.Button, talkBindingP1);
        talkActionP1.performed += _ => StartTransmit(1);
        talkActionP1.canceled += _ => StopTransmit(1);

        talkActionP2 = new InputAction("TalkP2", InputActionType.Button);
        talkActionP2.AddBinding(talkBindingP2);
        talkActionP2.AddBinding(talkBindingP2Fallback);
        talkActionP2.performed += _ => StartTransmit(2);
        talkActionP2.canceled += _ => StopTransmit(2);
    }

    private void OnEnable()
    {
        talkActionP1.Enable();
        talkActionP2.Enable();
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
    }

    private void Update()
    {
        // Reinicia no frame seguinte a uma mudança de dispositivo/saída.
        // O sistema operativo decide qual é a entrada predefinida atual.
        if (restartPending)
        {
            restartPending = false;
            int player = talkingPlayer;
            StopCapture();
            if (player != 0 && IsTalkHeld(player))
                StartTransmit(player);
        }

        UpdateMicLevel();
    }

    private void OnAudioConfigurationChanged(bool deviceWasChanged)
    {
        restartPending = true;
    }

    private bool IsTalkHeld(int player)
    {
        return player == 1 ? talkActionP1.IsPressed() : talkActionP2.IsPressed();
    }

    private void StartTransmit(int player)
    {
        if (!isActiveAndEnabled || talkingPlayer != 0)
            return;

        error = null;
        if (Microphone.devices.Length == 0)
        {
            error = "Sem microfone disponível.";
            return;
        }

        try
        {
            Microphone.GetDeviceCaps(DefaultDevice, out int minRate, out int maxRate);
            int rate = AudioSettings.outputSampleRate;
            if (rate <= 0)
                rate = 48000;
            if (maxRate > 0)
                rate = Mathf.Clamp(rate, Mathf.Max(1, minRate), maxRate);

            // Volta a usar o default atual em cada pressão de V/RB/B.
            micClip = Microphone.Start(DefaultDevice, true, Mathf.Max(1, clipSeconds), rate);
            if (micClip == null)
            {
                error = "Não foi possível iniciar o microfone predefinido.";
                return;
            }

            talkingPlayer = player;
            transmitRoutine = StartCoroutine(TransmitWhenReady());
        }
        catch (Exception exception)
        {
            StopCapture();
            error = "Falha no microfone: " + exception.Message;
        }
    }

    private IEnumerator TransmitWhenReady()
    {
        // Dá tempo ao dispositivo para começar, sem bloquear o jogo.
        yield return null;

        AudioSettings.GetDSPBufferSize(out int bufferSize, out _);
        int bufferedSamples = Mathf.Max(Mathf.CeilToInt(micClip.frequency * MonitorLatency), bufferSize * 2);
        bufferedSamples = Mathf.Clamp(bufferedSamples, 1, micClip.samples - 1);
        float deadline = Time.realtimeSinceStartup + StartupTimeout;
        int position = Microphone.GetPosition(DefaultDevice);

        while (position < bufferedSamples)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                transmitRoutine = null;
                StopCapture();
                error = "O microfone predefinido não iniciou. Confirma a entrada nas definições de som.";
                yield break;
            }

            yield return null;
            position = Microphone.GetPosition(DefaultDevice);
        }

        if (selfMonitor)
        {
            output.clip = micClip;
            // Lê atrás da escrita, sem reproduzir um segundo de silêncio antigo.
            output.timeSamples = position - bufferedSamples;
            output.Play();
        }
        transmitRoutine = null;
    }

    private void StopTransmit(int player)
    {
        if (talkingPlayer == player)
            StopCapture();
    }

    private void StopCapture()
    {
        if (transmitRoutine != null)
        {
            StopCoroutine(transmitRoutine);
            transmitRoutine = null;
        }
        if (output != null)
        {
            output.Stop();
            output.clip = null;
        }

        if (micClip != null)
        {
            Microphone.End(DefaultDevice);
            Destroy(micClip);
            micClip = null;
        }
        talkingPlayer = 0;
        micLevel = 0f;
    }

    private void OnDisable()
    {
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        restartPending = false;
        talkActionP1?.Disable();
        talkActionP2?.Disable();
        StopCapture();
    }

    private void OnDestroy()
    {
        talkActionP1?.Dispose();
        talkActionP2?.Dispose();
    }

    private void UpdateMicLevel()
    {
        if (micClip == null || !Microphone.IsRecording(DefaultDevice))
        {
            micLevel = 0f;
            return;
        }

        int frames = levelSamples.Length / Mathf.Max(1, micClip.channels);
        int position = Microphone.GetPosition(DefaultDevice);
        int offset = (position - frames + micClip.samples) % micClip.samples;
        if (!micClip.GetData(levelSamples, offset))
            return;

        float sum = 0f;
        foreach (float sample in levelSamples)
            sum += Mathf.Abs(sample);
        micLevel = sum / levelSamples.Length;
    }

    private void OnGUI()
    {
        Color oldColor = GUI.color;
        if (!showDebugPanel)
        {
            if (talkingPlayer != 0)
            {
                GUI.color = talkingPlayer == 1 ? Color.green : Color.cyan;
                GUI.Box(new Rect(10, 10, 220, 40), "● P" + talkingPlayer + " A FALAR");
            }
            else if (!string.IsNullOrEmpty(error))
            {
                GUI.color = Color.red;
                GUI.Box(new Rect(10, 10, 440, 60), error);
            }
            GUI.color = oldColor;
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 440, 230), GUI.skin.box);
        GUILayout.Label("VOICE LOCAL (protótipo)");
        GUILayout.Label("Micro: entrada predefinida do sistema");
        GUILayout.Label("P1: " + talkActionP1.GetBindingDisplayString() + " | P2: " + talkActionP2.GetBindingDisplayString());
        GUILayout.Label("Auto-escuta: " + (selfMonitor ? "ON" : "OFF"));
        GUILayout.Label(talkingPlayer == 0 ? "Pronto para falar." : "P" + talkingPlayer + " A TRANSMITIR");
        GUILayout.Label("Nível: " + new string('|', Mathf.Clamp((int)(micLevel * 200f), 0, 20)));
        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = Color.red;
            GUILayout.Label(error);
            GUI.color = oldColor;
        }
        if (talkingPlayer != 0)
        {
            GUILayout.Label("Gravar: " + Microphone.IsRecording(DefaultDevice) + " | Posição: " + Microphone.GetPosition(DefaultDevice));
            GUILayout.Label("Reproduzir: " + output.isPlaying + " | Volume: " + output.volume.ToString("F2"));
        }
        GUILayout.EndArea();
        GUI.color = oldColor;
    }
}
