using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Protótipo voz LOCAL (mesmo PC) com PTT e auto-escuta para testar o micro.
/// V = P1 fala, B = P2 fala. Para já ambos fazem loopback para as colunas,
/// por isso USA HEADSET e volume baixo para não fazer feedback.
/// Colocar uma vez na cena (objeto LocalVoice). Funciona em qualquer cena.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LocalVoiceChat : MonoBehaviour
{
    [Header("Push-to-talk (Input System)")]
    [Tooltip("P1 usa teclado+rato.")]
    [SerializeField] private string talkBindingP1 = "<Keyboard>/v";
    [Tooltip("P2 usa comando (RB).")]
    [SerializeField] private string talkBindingP2 = "<Gamepad>/rightShoulder";
    [Tooltip("Fallback para testar P2 sem comando ligado.")]
    [SerializeField] private string talkBindingP2Fallback = "<Keyboard>/b";

    [Header("Teste local")]
    [Tooltip("ON = ouves-te a ti próprio (para validar micro). OFF = mudo local (para 2 headsets).")]
    [SerializeField] private bool selfMonitor = true;
    [Tooltip("Simula rádio (lowpass). OFF = voz limpa. Liga só para prova de walkie-talkie.")]
    [SerializeField] private bool radioEffect = false;
    [Tooltip("Volume de saída. Baixa se distorcer.")]
    [SerializeField, Range(0f, 1f)] private float outputVolume = 0.7f;

    [Header("Microfone")]
    [Tooltip("Índice em Microphone.devices (só vale nesta máquina). Noutro PC usa palavra-chave ou teclas 1-9.")]
    [SerializeField] private int deviceIndex = -1;
    [Tooltip("Ex: 'headset'. Se algum micro contiver isto, é escolhido em qualquer PC.")]
    [SerializeField] private string preferredDevice = "";
    [SerializeField] private int clipSeconds = 1;

    [Header("UI")]
    [Tooltip("ON = painel completo de teste. OFF = só indicador a transmitir (para o jogo).")]
    [SerializeField] private bool showDebugPanel = false;

    private const string MicPrefKey = "voiceMic";

    private AudioSource output;
    private AudioClip micClip;
    private string device;
    private int talkingPlayer; // 0 = ninguém, 1 = P1, 2 = P2
    private string error;
    private InputAction talkActionP1;
    private InputAction talkActionP2;
    private Coroutine transmitRoutine;

    private void Update()
    {
        // Teclas 1-9 trocam de micro em direto (novo Input System).
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) SetDevice(0);
        if (kb.digit2Key.wasPressedThisFrame) SetDevice(1);
        if (kb.digit3Key.wasPressedThisFrame) SetDevice(2);
        if (kb.digit4Key.wasPressedThisFrame) SetDevice(3);
        if (kb.digit5Key.wasPressedThisFrame) SetDevice(4);
        if (kb.digit6Key.wasPressedThisFrame) SetDevice(5);
        if (kb.digit7Key.wasPressedThisFrame) SetDevice(6);
        if (kb.digit8Key.wasPressedThisFrame) SetDevice(7);
        if (kb.digit9Key.wasPressedThisFrame) SetDevice(8);
        if (kb.tKey.wasPressedThisFrame) StartCalibration();
    }

    private void SetDevice(int index)
    {
        if (index < 0 || index >= Microphone.devices.Length || index == deviceIndex) return;
        int who = talkingPlayer;
        if (who != 0) StopTransmit(who);
        deviceIndex = index;
        // Memoriza por máquina: no próximo arranque (Linux ou Windows) reusa este.
        PlayerPrefs.SetString(MicPrefKey, Microphone.devices[index]);
        PlayerPrefs.Save();
        if (who != 0) StartTransmit(who);
    }

        private Coroutine calibrateRoutine;
    private string calibrateDevice;

    private void StartCalibration()
    {
        if (calibrateRoutine != null || talkingPlayer != 0) return;
        calibrateRoutine = StartCoroutine(CalibrateRoutine());
    }

    private IEnumerator CalibrateRoutine()
    {
        error = null;
        float best = 0f;
        string winner = null;
        foreach (var d in Microphone.devices)
        {
            if (IsMonitor(d)) continue; // saída, nunca micro
            calibrateDevice = d;
            int minF, maxF;
            Microphone.GetDeviceCaps(d, out minF, out maxF);
            int rate = maxF == 0 ? AudioSettings.outputSampleRate : Mathf.Clamp(AudioSettings.outputSampleRate, minF, maxF);
            var clip = Microphone.Start(d, true, 1, rate);
            float t = 0f;
            while (t < 0.8f) { t += Time.deltaTime; yield return null; }
            float peak = MeasurePeak(clip, d);
            if (Microphone.IsRecording(d)) Microphone.End(d);
            if (clip != null) Destroy(clip);
            if (peak > best) { best = peak; winner = d; }
        }
        calibrateDevice = null;
        if (winner != null && best > 0.01f)
        {
            PlayerPrefs.SetString(MicPrefKey, winner);
            PlayerPrefs.Save();
            device = winner;
            int idx = Array.IndexOf(Microphone.devices, winner);
            if (idx >= 0) deviceIndex = idx;
            error = null;
        }
        else
        {
            error = "Calibração: não ouvi nada. Fala mais alto e prime T.";
        }
        calibrateRoutine = null;
    }

    private static float MeasurePeak(AudioClip clip, string dev)
    {
        if (clip == null) return 0f;
        int pos = Microphone.GetPosition(dev);
        int count = Math.Min(4000, pos);
        if (count <= 0) return 0f;
        float[] data = new float[count];
        clip.GetData(data, pos - count);
        float peak = 0f;
        foreach (var s in data)
        {
            float a = Math.Abs(s);
            if (a > peak) peak = a;
        }
        return peak;
    }

    private string ResolveDevice()
    {
        var list = Microphone.devices;
        if (list.Length == 0) return null;
        // 1. Último micro que funcionou nesta máquina.
        string saved = PlayerPrefs.GetString(MicPrefKey, "");
        if (!string.IsNullOrEmpty(saved))
            foreach (var d in list) if (d == saved) return d;
        // 2. Palavra-chave (portátil entre Linux/Windows).
        if (!string.IsNullOrEmpty(preferredDevice))
            foreach (var d in list)
                if (d.IndexOf(preferredDevice, StringComparison.OrdinalIgnoreCase) >= 0) return d;
        // 3. Automático: headset/USB primeiro; "monitor" nunca (é saída, não micro).
        // Genéricos "default" ficam para último: no Linux nem sempre gravam.
        foreach (var d in list) if (IsHeadsetMic(d)) return d;
        foreach (var d in list) if (!IsMonitor(d) && !IsGenericDefault(d)) return d;
        // 4. Índice da cena ou primeiro.
        if (deviceIndex >= 0 && deviceIndex < list.Length) return list[deviceIndex];
        return list[0];
    }

    private static bool IsMonitor(string n)
    {
        return n.IndexOf("monitor", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsGenericDefault(string n)
    {
        return n.IndexOf("default", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static readonly string[] HeadsetHints =
        { "headset", "headphone", "usb", "bluetooth", "wireless", "external", "webcam" };

    private static bool IsHeadsetMic(string n)
    {
        if (IsMonitor(n)) return false;
        foreach (var h in HeadsetHints)
            if (n.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private void Awake()
    {
        output = GetComponent<AudioSource>();
        if (output == null)
            output = gameObject.AddComponent<AudioSource>();
        output.loop = true;
        output.playOnAwake = false;
        output.volume = outputVolume;
        output.pitch = 1f;
        output.spatialBlend = 0f; // 2D: sem atenuação 3D a estragar o nível

        if (radioEffect && GetComponent<AudioLowPassFilter>() == null)
        {
            var lp = gameObject.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 2800f;
        }

        talkActionP1 = new InputAction("TalkP1", binding: talkBindingP1);
        talkActionP1.performed += _ => StartTransmit(1);
        talkActionP1.canceled += _ => StopTransmit(1);
        talkActionP1.Enable();

        talkActionP2 = new InputAction("TalkP2");
        talkActionP2.AddBinding(talkBindingP2);
        talkActionP2.AddBinding(talkBindingP2Fallback);
        talkActionP2.performed += _ => StartTransmit(2);
        talkActionP2.canceled += _ => StopTransmit(2);
        talkActionP2.Enable();
    }

    private void StartTransmit(int player)
    {
        if (talkingPlayer != 0 || transmitRoutine != null) return; // half-duplex: um de cada vez

        if (Microphone.devices.Length == 0)
        {
            error = "Sem microfone detetado.";
            return;
        }

        // Usa sempre o nome explícito do dispositivo:
        // GetPosition(null) devolve 0 e a espera pelo arranque nunca terminava.
        // A escolha é portátil: micro memorizado > palavra-chave > índice > primeiro.
        device = ResolveDevice();
        if (device == null)
        {
            error = "Sem microfone detetado.";
            return;
        }

        error = null;
        // Escolhe taxa suportada pelo micro: output rate se suportado,
        // senão o máximo do dispositivo (44100 fixo partia alguns micros).
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(device, out minFreq, out maxFreq);
        int rate = maxFreq == 0
            ? AudioSettings.outputSampleRate
            : Mathf.Clamp(AudioSettings.outputSampleRate, minFreq, maxFreq);
        micClip = Microphone.Start(device, true, clipSeconds, rate);
        if (micClip == null)
        {
            error = "Falha a iniciar o microfone.";
            return;
        }

        talkingPlayer = player; // reserva já o canal
        transmitRoutine = StartCoroutine(TransmitWhenReady());
    }

    private IEnumerator TransmitWhenReady()
    {
        // Espera o micro arrancar antes de tocar: corta o atraso inicial
        // e evita tocar buffer vazio.
        int waits = 0;
        while (Microphone.GetPosition(device) <= 0 && waits < 60)
        {
            waits++;
            yield return null;
        }

        if (selfMonitor && micClip != null)
        {
            output.clip = micClip;
            output.Play();
        }
        transmitRoutine = null;
    }

    private void StopTransmit(int player)
    {
        if (talkingPlayer != player) return;

        talkingPlayer = 0;
        if (transmitRoutine != null)
        {
            StopCoroutine(transmitRoutine);
            transmitRoutine = null;
        }
        output.Stop();
        output.clip = null;

        if (Microphone.IsRecording(device))
            Microphone.End(device);

        micClip = null;
    }

    private void OnDisable()
    {
        if (talkingPlayer != 0)
            StopTransmit(talkingPlayer);

        if (talkActionP1 != null) { talkActionP1.Disable(); talkActionP1.Dispose(); talkActionP1 = null; }
        if (talkActionP2 != null) { talkActionP2.Disable(); talkActionP2.Dispose(); talkActionP2 = null; }
    }

    private float MicLevel()
    {
        if (micClip == null || talkingPlayer == 0) return 0f;
        float[] data = new float[256];
        int pos = Microphone.GetPosition(device) - 256;
        if (pos < 0) return 0f;
        micClip.GetData(data, pos);
        float sum = 0f;
        for (int i = 0; i < data.Length; i++) sum += Mathf.Abs(data[i]);
        return sum / data.Length;
    }

    private void OnGUI()
    {
        // Modo jogo: só indicador enquanto alguém fala (sem painel de teste).
        if (!showDebugPanel)
        {
            if (talkingPlayer == 1 || talkingPlayer == 2)
            {
                GUI.color = talkingPlayer == 1 ? Color.green : Color.cyan;
                GUILayout.BeginArea(new Rect(10, 10, 220, 40), GUI.skin.box);
                GUILayout.Label(talkingPlayer == 1 ? "● P1 A FALAR" : "● P2 A FALAR");
                GUILayout.EndArea();
                GUI.color = Color.white;
            }
            else if (!string.IsNullOrEmpty(error))
            {
                GUI.color = Color.red;
                GUILayout.BeginArea(new Rect(10, 10, 340, 40), GUI.skin.box);
                GUILayout.Label(error);
                GUILayout.EndArea();
                GUI.color = Color.white;
            }
            return;
        }

        string keyP1 = talkActionP1 != null ? talkActionP1.GetBindingDisplayString() : talkBindingP1;
        string keyP2 = talkActionP2 != null ? talkActionP2.GetBindingDisplayString() : talkBindingP2;

        int w = 340;
        GUILayout.BeginArea(new Rect(10, 10, w, 220), GUI.skin.box);
        GUILayout.Label("<b>VOICE LOCAL (protótipo)</b>");
        GUILayout.Label($"Micro: {(Microphone.devices.Length == 0 ? "nenhum" : (device ?? Microphone.devices[0]))}");
        GUILayout.Label($"Segura {keyP1} = P1 fala | Segura {keyP2} = P2 fala");
        GUILayout.Label($"Auto-escuta: {(selfMonitor ? "ON (ouves-te)" : "OFF")}");

        if (!string.IsNullOrEmpty(error))
        {
            GUI.color = Color.red;
            GUILayout.Label(error);
            GUI.color = Color.white;
        }
        else if (talkingPlayer == 1)
        {
            GUI.color = Color.green;
            GUILayout.Label("● P1 A TRANSMITIR... (larga " + keyP1 + ")");
            GUI.color = Color.white;
        }
        else if (talkingPlayer == 2)
        {
            GUI.color = Color.cyan;
            GUILayout.Label("● P2 A TRANSMITIR... (larga " + keyP2 + ")");
            GUI.color = Color.white;
        }
        else
        {
            GUILayout.Label("Pronto. Segura V e fala para testar.");
        }

        // Medidor de nível simples
        float level = MicLevel();
        GUILayout.Label("Nível: " + new string('|', Mathf.Clamp((int)(level * 200f), 0, 20)));
        if (talkingPlayer != 0 && level <= 0.0001f)
            GUILayout.Label("Nível parado? Tenta outro micro (1-9) e vê Privacidade>Microfone no Windows.");

        // Lista de micros para trocar em direto
        GUILayout.Label("Micros (1-9 ou T=auto-calibrar falando):");
        for (int i = 0; i < Microphone.devices.Length; i++)
            GUILayout.Label((i == deviceIndex || Microphone.devices[i] == device ? "> " : "  ") + (i + 1) + ": " + Microphone.devices[i]);
        if (calibrateRoutine != null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("A CALIBRAR: fala! (a testar " + (calibrateDevice ?? "?") + ")");
            GUI.color = Color.white;
        }

        // Diagnóstico: diz exatamente onde está a falhar
        if (talkingPlayer != 0)
        {
            bool rec = Microphone.IsRecording(device);
            int pos = rec ? Microphone.GetPosition(device) : -1;
            GUILayout.Label($"Diag: gravar={rec} pos={pos} tocar={output.isPlaying} vol={output.volume:F2}");
        }
        GUILayout.EndArea();
    }
}
