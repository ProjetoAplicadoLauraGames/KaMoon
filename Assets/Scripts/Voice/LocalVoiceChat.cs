using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Voz com push-to-talk e auto-escuta para testar o microfone.
/// Usa a entrada predefinida do sistema em cada transmissão.
/// P1: V. P2: RB no comando, com B como alternativa no teclado.
///
/// Em split-screen o som reproduz-se no próprio PC (auto-escuta). Numa partida
/// de rede o microfone é capturado a 16 kHz e enviado ao servidor em frames PCM
/// pelo canal não fiável; o servidor reencaminha para o outro jogador, que os
/// reproduz num AudioSource próprio. A autoridade da sala e da bateria fica no
/// servidor (ver <see cref="NetMatch"/>).
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

    // Voz de rede: PCM mono 16 bits, 16 kHz. Um bloco de 30 ms são 480 amostras
    // (960 bytes) e cabe num datagrama do KCP mesmo com os cabeçalhos do Mirror.
    private const int VoiceRate = 16000;
    private const int VoiceChunkSamples = VoiceRate * 30 / 1000;
    private const int VoiceBufferMilliseconds = 250;
    private const int VoicePrefillMilliseconds = 80;
    private const float VoiceRestartGap = 0.3f;
    private const float VoiceSendInterval = 0.02f;

    private AudioSource output;
    private AudioClip micClip;
    private InputAction talkActionP1;
    private InputAction talkActionP2;
    private Coroutine transmitRoutine;
    private int talkingPlayer; // 0 = ninguém, 1 = P1, 2 = P2

    /// <summary>Jogador a transmitir agora (0 = ninguém). Usado pela bateria.</summary>
    public int TalkingPlayer => talkingPlayer;
    private string error;
    private Battery[] batteries;
    private bool restartPending;
    private float micLevel;
    private readonly float[] levelSamples = new float[256];

    // ---- Envio de voz em rede ----
    private bool micReady;                // o dispositivo já escreveu o arranque do clip
    private int micReadPosition;          // próxima amostra a ler no clip do microfone
    private double resamplePhase;         // parte fracionária entre amostras do microfone
    private float sendTimer;
    private float[] micWindow;
    private float[] micWrap;
    private float[] voiceSamples;

    // ---- Reprodução da voz remota ----
    private AudioSource remoteOutput;
    private AudioClip remoteClip;
    private readonly Queue<float> remoteSamples = new Queue<float>();
    private readonly object remoteLock = new object();
    private float remoteLastFrameAt;

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
        talkActionP1.performed += _ => StartTransmit(GameSession.multiplayer && GameSession.room == 2 ? 2 : 1);
        talkActionP1.canceled += _ => StopTransmit(GameSession.multiplayer && GameSession.room == 2 ? 2 : 1);

        talkActionP2 = new InputAction("TalkP2", InputActionType.Button);
        talkActionP2.AddBinding(talkBindingP2);
        talkActionP2.AddBinding(talkBindingP2Fallback);
        talkActionP2.performed += _ => StartTransmit(2);
        talkActionP2.canceled += _ => StopTransmit(2);
    }

    /// <summary>Verdadeiro se a bateria do jogador bloqueia o voice (task #17).</summary>
    private bool BatteryDepleted(int player)
    {
        if (batteries == null) return false;
        foreach (Battery battery in batteries)
        {
            if (battery != null && battery.PlayerNumber == player && battery.IsDepleted)
                return true;
        }
        return false;
    }

    private void OnEnable()
    {
        talkActionP1.Enable();
        talkActionP2.Enable();
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        // Sem bateria (task #17), o jogador não pode falar.
        batteries = FindObjectsByType<Battery>(FindObjectsSortMode.None);

        if (remoteOutput != null && remoteClip != null)
            remoteOutput.Play();
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
        SendNetworkVoice();

        // A bateria pode esgotar a meio da fala: corta e avisa.
        if (talkingPlayer != 0 && BatteryDepleted(talkingPlayer))
        {
            StopTransmit(talkingPlayer);
            error = $"Bateria esgotada (P{talkingPlayer}): voice bloqueado.";
        }
    }

    private void OnAudioConfigurationChanged(bool deviceWasChanged)
    {
        restartPending = true;
    }

    private bool IsTalkHeld(int player)
    {
        return player == 1 || (GameSession.multiplayer && GameSession.room == 2)
            ? talkActionP1.IsPressed() || (player == 2 && talkActionP2.IsPressed())
            : talkActionP2.IsPressed();
    }

    private void StartTransmit(int player)
    {
        if (!isActiveAndEnabled || talkingPlayer != 0)
            return;

        error = null;
        // Sem bateria (task #17), o jogador não pode falar.
        if (BatteryDepleted(player))
        {
            error = $"Sem bateria (P{player}): voice bloqueado.";
            return;
        }
        if (Microphone.devices.Length == 0)
        {
            error = "Sem microfone disponível.";
            return;
        }

        try
        {
            Microphone.GetDeviceCaps(DefaultDevice, out int minRate, out int maxRate);
            // Em partida de rede grava-se já a 16 kHz para não reamostrar e manter
            // os frames pequenos. Fora dela preserva-se a taxa de saída do sistema,
            // para a auto-escuta local ter a qualidade original.
            int rate = NetMatch.EmPartida ? VoiceRate : AudioSettings.outputSampleRate;
            if (rate <= 0)
                rate = 48000;
            if (maxRate > 0)
                rate = Mathf.Clamp(rate, Mathf.Max(1, minRate), maxRate);

            // Volta a usar o default atual em cada pressão de V/RB/B.
            micClip = Microphone.Start(DefaultDevice, true, Mathf.Max(1, clipSeconds), rate);
            if (micClip == null && NetMatch.EmPartida)
            {
                // Alguns dispositivos recusam os 16 kHz: grava na taxa de saída
                // do sistema e o envio reamostra para 16 kHz.
                int fallback = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
                if (fallback != rate)
                    micClip = Microphone.Start(DefaultDevice, true, Mathf.Max(1, clipSeconds), fallback);
            }
            if (micClip == null)
            {
                error = "Não foi possível iniciar o microfone predefinido.";
                return;
            }

            talkingPlayer = player;
            micReady = false;
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

        // Em rede não há auto-escuta: basta o dispositivo ter começado a escrever.
        // Esperar pelo buffer da auto-escuta (~128 ms) cortava o início da fala.
        int readySamples = NetMatch.EmPartida
            ? Mathf.Clamp(Mathf.CeilToInt(micClip.frequency * 0.02f), 1, micClip.samples - 1)
            : bufferedSamples;
        float deadline = Time.realtimeSinceStartup + StartupTimeout;
        int position = Microphone.GetPosition(DefaultDevice);

        while (position < readySamples)
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

        // Em rede a voz vai para o outro jogador: a auto-escuta só faria eco.
        if (selfMonitor && !NetMatch.EmPartida)
        {
            output.clip = micClip;
            // Lê atrás da escrita, sem reproduzir um segundo de silêncio antigo.
            output.timeSamples = position - bufferedSamples;
            output.Play();
        }

        // Começa a enviar no ponto atual da gravação, e não no início do clip,
        // para não ir um segundo de silêncio à frente da voz.
        micReadPosition = Microphone.GetPosition(DefaultDevice);
        resamplePhase = 0d;
        sendTimer = 0f;
        micReady = true;
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
        micReady = false;
        sendTimer = 0f;
        micReadPosition = 0;
        resamplePhase = 0d;
    }

    private void OnDisable()
    {
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        restartPending = false;
        talkActionP1?.Disable();
        talkActionP2?.Disable();
        StopCapture();

        if (remoteOutput != null) remoteOutput.Stop();
        lock (remoteLock) remoteSamples.Clear();
        remoteLastFrameAt = 0f;
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

    // ------------------------------------------------------------------ rede

    /// <summary>
    /// Lê a gravação em passos de 20 ms e envia o PCM ao servidor. Só corre em
    /// partida de rede e enquanto o push-to-talk estiver pressionado. O que chega
    /// ao <see cref="NetMatch"/> já não volta a este cliente (o servidor exclui o
    /// remetente pela sala).
    /// </summary>
    private void SendNetworkVoice()
    {
        if (talkingPlayer == 0 || micClip == null || !micReady || !NetMatch.EmPartida || !NetworkClient.active)
        {
            sendTimer = 0f;
            return;
        }

        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < VoiceSendInterval) return;
        sendTimer = 0f;

        int available = AvailableMicSamples();
        if (available < 2) return;

        // Acima de 100 ms de atraso a voz já não interessa: descarta o áudio antigo
        // em vez de o acumular (importante quando um frame do jogo demora).
        int maxSource = Mathf.CeilToInt(micClip.frequency * 0.1f);
        if (available > maxSource)
        {
            micReadPosition = (micReadPosition + available - maxSource) % micClip.samples;
            resamplePhase = 0d;
            available = maxSource;
        }

        ReadMicSamples(available);
        int produced = Resample(available);
        if (produced <= 0) return;

        // O PCM é dividido em blocos que cabem num datagrama; um atraso maior
        // produz dois ou três frames em vez de um frame gigante que o KCP descarta.
        for (int offset = 0; offset < produced; offset += VoiceChunkSamples)
            SendVoiceChunk(offset, Mathf.Min(VoiceChunkSamples, produced - offset));
    }

    /// <summary>Amostras novas na gravação circular desde a última leitura.</summary>
    private int AvailableMicSamples()
    {
        if (micClip == null || !Microphone.IsRecording(DefaultDevice)) return 0;

        int writePosition = Microphone.GetPosition(DefaultDevice);
        int available = writePosition - micReadPosition;
        if (available < 0) available += micClip.samples;
        if (available >= micClip.samples) available = 0;
        return available;
    }

    /// <summary>Copia <paramref name="count"/> amostras para <c>micWindow</c>, incluindo a volta do clip.</summary>
    private void ReadMicSamples(int count)
    {
        int clipSamples = micClip.samples;
        if (micWindow == null || micWindow.Length < count)
            micWindow = new float[Mathf.NextPowerOfTwo(count)];
        if (micWrap == null || micWrap.Length < clipSamples)
            micWrap = new float[clipSamples];

        int first = Mathf.Min(count, clipSamples - micReadPosition);
        if (first > 0)
            micClip.GetData(micWindow, micReadPosition);

        // GetData não dá a volta: o resto vem do início do clip. Tem de ser
        // depois da leitura direta, que escreve zeros para lá do fim do clip.
        if (first < count)
        {
            micClip.GetData(micWrap, 0);
            for (int i = first; i < count; i++)
                micWindow[i] = micWrap[i - first];
        }
    }

    /// <summary>Converte a janela do microfone para 16 kHz e devolve o número de amostras.</summary>
    private int Resample(int sourceCount)
    {
        int rate = micClip.frequency > 0 ? micClip.frequency : VoiceRate;
        double ratio = rate / (double)VoiceRate;

        // Um dispositivo abaixo de 16 kHz produz mais amostras do que consome.
        int capacity = rate == VoiceRate
            ? sourceCount
            : Mathf.CeilToInt(sourceCount / (float)ratio) + 2;
        if (voiceSamples == null || voiceSamples.Length < capacity)
            voiceSamples = new float[capacity];

        if (rate == VoiceRate)
        {
            Array.Copy(micWindow, voiceSamples, sourceCount);
            micReadPosition = (micReadPosition + sourceCount) % micClip.samples;
            return sourceCount;
        }

        // Fallback para um dispositivo que não aceitou os 16 kHz.
        int produced = Mathf.FloorToInt((float)((sourceCount - 1 - resamplePhase) / ratio)) + 1;
        if (produced <= 0) return 0;

        for (int i = 0; i < produced; i++)
        {
            double position = resamplePhase + i * ratio;
            int index = (int)position;
            float fraction = (float)(position - index);
            float a = micWindow[index];
            float b = micWindow[Mathf.Min(index + 1, sourceCount - 1)];
            voiceSamples[i] = a + (b - a) * fraction;
        }

        double end = resamplePhase + produced * ratio;
        int consumed = (int)end;
        if (consumed >= sourceCount)
        {
            consumed = sourceCount - 1;
            resamplePhase = 0d;
        }
        else
        {
            resamplePhase = end - consumed;
        }
        micReadPosition = (micReadPosition + consumed) % micClip.samples;
        return produced;
    }

    private void SendVoiceChunk(int offset, int count)
    {
        // Array exato: um bloco parcial não pode levar o resto do bloco anterior.
        byte[] pcm = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            int value = Mathf.Clamp(Mathf.RoundToInt(voiceSamples[offset + i] * 32767f), short.MinValue, short.MaxValue);
            pcm[i * 2] = (byte)(value & 0xFF);
            pcm[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        NetworkClient.Send(new VozJogadorMessage { pcm = pcm }, Channels.Unreliable);
    }

    /// <summary>
    /// Recebe PCM do outro jogador (já validado pelo servidor) e coloca-o no buffer
    /// que o AudioSource remoto consome. O buffer absorve o jitter da rede e é
    /// limitado para a voz não ficar atrasada numa rajada de pacotes.
    /// </summary>
    public void ReceberVoz(int sala, byte[] pcm)
    {
        if (!isActiveAndEnabled || pcm == null || pcm.Length < 2) return;
        if (GameSession.multiplayer && sala == GameSession.room) return;   // eco

        EnsureRemoteOutput();

        lock (remoteLock)
        {
            // O primeiro frame depois de uma pausa abre um novo turno de fala:
            // limpa o que sobrou e repõe a margem de jitter.
            if (Time.unscaledTime - remoteLastFrameAt > VoiceRestartGap)
            {
                remoteSamples.Clear();
                int prefill = VoiceRate * VoicePrefillMilliseconds / 1000;
                for (int i = 0; i < prefill; i++)
                    remoteSamples.Enqueue(0f);
            }
            remoteLastFrameAt = Time.unscaledTime;

            int samples = pcm.Length / 2;
            for (int i = 0; i < samples; i++)
            {
                short value = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
                remoteSamples.Enqueue(value / 32768f);
            }

            int max = VoiceRate * VoiceBufferMilliseconds / 1000;
            while (remoteSamples.Count > max)
                remoteSamples.Dequeue();
        }
    }

    private void EnsureRemoteOutput()
    {
        if (remoteOutput != null) return;

        remoteOutput = gameObject.AddComponent<AudioSource>();
        remoteOutput.playOnAwake = false;
        remoteOutput.loop = true;
        remoteOutput.volume = outputVolume;
        remoteOutput.spatialBlend = 0f;

        remoteClip = AudioClip.Create("VozRemota", VoiceRate, 1, VoiceRate, true, OnRemoteAudioRead);
        remoteOutput.clip = remoteClip;
        remoteOutput.Play();
    }

    /// <summary>Chamado na thread de áudio: entrega amostras ou silêncio se o buffer secar.</summary>
    private void OnRemoteAudioRead(float[] data)
    {
        lock (remoteLock)
        {
            int available = Mathf.Min(data.Length, remoteSamples.Count);
            for (int i = 0; i < available; i++)
                data[i] = remoteSamples.Dequeue();
            for (int i = available; i < data.Length; i++)
                data[i] = 0f;
        }
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
        GUILayout.Label("VOICE (protótipo)");
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
