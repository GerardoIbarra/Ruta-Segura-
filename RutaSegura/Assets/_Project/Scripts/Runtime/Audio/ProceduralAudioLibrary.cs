using UnityEngine;

namespace RutaSegura.Runtime.Audio
{
    /// <summary>
    /// Generador procedural de AudioClips diegeticos en tiempo de ejecucion (44.1 kHz, mono)
    /// para garantizar audio funcional sin depender de archivos binarios externos.
    /// Todos los clips incluyen envolvente de entrada/salida para evitar chasquidos.
    /// </summary>
    public static class ProceduralAudioLibrary
    {
        private const int SampleRate = 44100;

        private static AudioClip heartbeatClip;
        private static AudioClip[] footstepClips;
        private static AudioClip cityAmbienceClip;
        private static AudioClip protestSirenClip;
        private static AudioClip scanTickClip;
        private static AudioClip scanCompleteClip;
        private static AudioClip heavyBreathingClip;
        private static AudioClip metalShutterClip;
        private static AudioClip distantCarHornClip;
        private static AudioClip streetMurmurClip;

        public static AudioClip GetHeartbeatClip()
        {
            if (heartbeatClip != null) return heartbeatClip;

            // Doble golpe fisiologico "lub-dub": ~0.48s
            int totalSamples = Mathf.RoundToInt(SampleRate * 0.48f);
            float[] data = new float[totalSamples];

            // S1 (lub): 0.00s..0.14s, barrido 55 -> 34 Hz
            AddHeartbeatPulse(data, startSec: 0.0f, durSec: 0.14f, fStart: 55f, fEnd: 34f, amp: 1.0f);
            // S2 (dub): 0.17s..0.29s, barrido 68 -> 40 Hz
            AddHeartbeatPulse(data, startSec: 0.17f, durSec: 0.12f, fStart: 68f, fEnd: 40f, amp: 0.78f);

            NormalizeAndSmoothEnds(data, fadeSamples: 256);
            heartbeatClip = BuildClip("Proc_Heartbeat", data, loop: false);
            return heartbeatClip;
        }

        public static AudioClip GetFootstepClip(int variant)
        {
            if (footstepClips == null || footstepClips.Length != 4)
            {
                footstepClips = new AudioClip[4];
            }

            int idx = Mathf.Abs(variant) % 4;
            if (footstepClips[idx] != null) return footstepClips[idx];

            // Paso breve sobre asfalto/concreto (~0.11s)
            int totalSamples = Mathf.RoundToInt(SampleRate * 0.11f);
            float[] data = new float[totalSamples];
            uint rng = (uint)(73856093 ^ (idx * 19349663));
            float lp = 0f;
            float basePitch = 95f + idx * 11f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float thud = Mathf.Sin(2f * Mathf.PI * basePitch * t) * Mathf.Exp(-t * 48f);

                // Grano de gravilla filtrado en pasa-banda suave
                float white = NextWhite(ref rng);
                lp = Mathf.Lerp(lp, white, 0.24f);
                float grit = (lp - white * 0.12f) * Mathf.Exp(-t * 36f);

                data[i] = thud * 0.55f + grit * 0.65f;
            }

            NormalizeAndSmoothEnds(data, fadeSamples: 120);
            footstepClips[idx] = BuildClip($"Proc_Footstep_{idx}", data, loop: false);
            return footstepClips[idx];
        }

        public static AudioClip GetCityAmbienceClip()
        {
            if (cityAmbienceClip != null) return cityAmbienceClip;

            // 8 segundos con mezcla cruzada para loop sin saltos
            float duration = 8.0f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] raw = new float[totalSamples];
            uint rng = 1299709u;

            float b0 = 0f, b1 = 0f, b2 = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float white = NextWhite(ref rng);
                // Filtro pasa-bajas escalonado para ruido urbano/viento tipo "ruido marron"
                b0 = b0 * 0.992f + white * 0.008f;
                b1 = b1 * 0.965f + white * 0.035f;
                b2 = b2 * 0.85f + white * 0.15f;

                float windSwell = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * (t / 4.0f));
                float trafficHum = Mathf.Sin(2f * Mathf.PI * 58f * t) * 0.04f +
                                   Mathf.Sin(2f * Mathf.PI * 87f * t) * 0.025f;

                raw[i] = (b0 * 4.2f + b1 * 1.1f * windSwell + b2 * 0.18f) + trafficHum;
            }

            MakeSeamlessLoop(raw, overlapSamples: SampleRate / 2);
            Normalize(raw, peakTarget: 0.72f);
            cityAmbienceClip = BuildClip("Proc_CityAmbience", raw, loop: true);
            return cityAmbienceClip;
        }

        public static AudioClip GetProtestSirenClip()
        {
            if (protestSirenClip != null) return protestSirenClip;

            // 10 segundos: consignas coreadas de multitud + megafono distorsionado + sirena distante
            float duration = 10.0f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] raw = new float[totalSamples];
            uint rng = 982451653u;

            float low = 0f, mid = 0f, prevMid = 0f, megaLP = 0f;
            float sirenPhase = 0f;
            float megaPhase = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float white = NextWhite(ref rng);

                // Cadencia ritmica de consignas colectivas (~0.85 Hz) sobre murmullo continuo
                float chantBeat = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.85f * t), 2.2f);
                low = low * 0.97f + white * 0.03f;
                mid = mid * 0.89f + white * 0.11f;
                float murmur = (mid - low * 0.45f) * (0.55f + 0.75f * chantBeat);
                prevMid = prevMid * 0.93f + murmur * 0.07f;

                // Rafagas amortiguadas de megafono cada ~3.33s (con distorsion controlada)
                float megaSlot = (t % 3.333f);
                float megaActive = (megaSlot > 0.4f && megaSlot < 1.5f)
                    ? Mathf.Sin(Mathf.PI * (megaSlot - 0.4f) / 1.1f)
                    : 0f;
                float voiceCarrier = 185f + 55f * Mathf.Sin(2f * Mathf.PI * 4.5f * t);
                megaPhase += 2f * Mathf.PI * voiceCarrier / SampleRate;
                float rawMega = Mathf.Sin(megaPhase) * 0.6f + white * 0.25f;
                megaLP = megaLP * 0.82f + rawMega * 0.18f;
                float mega = (float)System.Math.Tanh(megaLP * 2.2f) * 0.18f * megaActive;

                // Sirena mexicana lejana amortiguada (~540 -> 780 Hz)
                float cycle = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * 0.5f * t);
                float freq = Mathf.Lerp(540f, 780f, cycle);
                sirenPhase += 2f * Mathf.PI * freq / SampleRate;
                float sirenEnv = 0.35f + 0.65f * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * 0.1f * t));
                float siren = Mathf.Sin(sirenPhase) * 0.11f * sirenEnv;

                raw[i] = low * 1.3f + prevMid * 2.6f + mega + siren;
            }

            MakeSeamlessLoop(raw, overlapSamples: SampleRate / 2);
            Normalize(raw, peakTarget: 0.70f);
            protestSirenClip = BuildClip("Proc_ProtestSiren", raw, loop: true);
            return protestSirenClip;
        }

        public static AudioClip GetScanTickClip()
        {
            if (scanTickClip != null) return scanTickClip;

            int totalSamples = Mathf.RoundToInt(SampleRate * 0.12f);
            float[] data = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / 0.12f)) * Mathf.Exp(-t * 14f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1080f * t) * env * 0.5f;
            }

            scanTickClip = BuildClip("Proc_ScanTick", data, loop: false);
            return scanTickClip;
        }

        public static AudioClip GetScanCompleteClip()
        {
            if (scanCompleteClip != null) return scanCompleteClip;

            int totalSamples = Mathf.RoundToInt(SampleRate * 0.28f);
            float[] data = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float f = t < 0.11f ? 740f : 1110f;
                float localT = t < 0.11f ? t : (t - 0.11f);
                float env = Mathf.Exp(-localT * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
            }

            NormalizeAndSmoothEnds(data, fadeSamples: 128);
            scanCompleteClip = BuildClip("Proc_ScanComplete", data, loop: false);
            return scanCompleteClip;
        }

        public static AudioClip GetHeavyBreathingClip()
        {
            if (heavyBreathingClip != null) return heavyBreathingClip;

            // Ciclo de respiracion agitada (~2.6s): inhalacion tensa + pausa breve + exhalacion pesada
            float duration = 2.6f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] raw = new float[totalSamples];
            uint rng = 941083u;
            float lpFast = 0f;
            float lpSlow = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float white = NextWhite(ref rng);

                float env = 0f;
                float cutoffA = 0.18f;
                float cutoffB = 0.05f;
                if (t < 0.95f)
                {
                    float u = t / 0.95f;
                    env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.35f) * 0.72f;
                    cutoffA = 0.24f;
                    cutoffB = 0.07f;
                }
                else if (t >= 1.18f && t < 2.38f)
                {
                    float u = (t - 1.18f) / 1.20f;
                    env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.15f) * 1.0f;
                    cutoffA = 0.14f;
                    cutoffB = 0.03f;
                }

                lpFast = lpFast * (1f - cutoffA) + white * cutoffA;
                lpSlow = lpSlow * (1f - cutoffB) + white * cutoffB;
                float bandNoise = lpFast - lpSlow;
                float airflowTone = Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.08f;
                raw[i] = (bandNoise * 2.2f + airflowTone) * env;
            }

            MakeSeamlessLoop(raw, overlapSamples: SampleRate / 4);
            Normalize(raw, peakTarget: 0.76f);
            heavyBreathingClip = BuildClip("Proc_HeavyBreathing", raw, loop: true);
            return heavyBreathingClip;
        }

        public static AudioClip GetMetalShutterClip()
        {
            if (metalShutterClip != null) return metalShutterClip;

            // Cortina metalica enrollable bajando con traqueteo y golpe seco final a los 1.45s (~1.6s total)
            float duration = 1.60f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[totalSamples];
            uint rng = 551923u;
            float lp = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float white = NextWhite(ref rng);
                lp = lp * 0.72f + white * 0.28f;

                float sample = 0f;
                if (t < 1.45f)
                {
                    float norm = t / 1.45f;
                    float rattleFreq = Mathf.Lerp(18f, 34f, norm);
                    float rattle = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Mathf.PI * rattleFreq * t));
                    float metalHarm = Mathf.Sin(2f * Mathf.PI * 410f * t) * 0.24f +
                                      Mathf.Sin(2f * Mathf.PI * 785f * t) * 0.18f +
                                      Mathf.Sin(2f * Mathf.PI * 1360f * t) * 0.12f;
                    float screechEnv = Mathf.Sin(Mathf.PI * Mathf.Clamp01(norm * 0.9f + 0.05f));
                    sample = (metalHarm + lp * 0.55f) * rattle * screechEnv * 0.65f;
                }
                else
                {
                    float impactT = t - 1.45f;
                    float subThump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(78f, 36f, Mathf.Clamp01(impactT * 8f)) * impactT)
                                     * Mathf.Exp(-impactT * 22f);
                    float clang = (Mathf.Sin(2f * Mathf.PI * 310f * impactT) * 0.5f + lp * 0.8f)
                                  * Mathf.Exp(-impactT * 28f);
                    sample = subThump * 1.15f + clang * 0.75f;
                }

                data[i] = sample;
            }

            NormalizeAndSmoothEnds(data, fadeSamples: 256);
            metalShutterClip = BuildClip("Proc_MetalShutter", data, loop: false);
            return metalShutterClip;
        }

        public static AudioClip GetDistantCarHornClip()
        {
            if (distantCarHornClip != null) return distantCarHornClip;

            // Claxon automotriz bitonal calido (~0.72s): 405 Hz + 510 Hz con armonicos suaves
            float duration = 0.72f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.035f);
                float release = Mathf.Clamp01((duration - t) / 0.14f);
                float env = attack * release;

                float toneA = Mathf.Sin(2f * Mathf.PI * 405f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * 810f * t) + 0.14f * Mathf.Sin(2f * Mathf.PI * 1215f * t);
                float toneB = Mathf.Sin(2f * Mathf.PI * 510f * t) + 0.30f * Mathf.Sin(2f * Mathf.PI * 1020f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * 1530f * t);
                float warm = (float)System.Math.Tanh((toneA * 0.45f + toneB * 0.45f) * 1.15f);
                data[i] = warm * env * 0.68f;
            }

            NormalizeAndSmoothEnds(data, fadeSamples: 256);
            distantCarHornClip = BuildClip("Proc_DistantCarHorn", data, loop: false);
            return distantCarHornClip;
        }

        public static AudioClip GetStreetMurmurClip()
        {
            if (streetMurmurClip != null) return streetMurmurClip;

            // 6 segundos: murmullo de transeuntes, pasos lejanos y ralentis de trafico detenido
            float duration = 6.0f;
            int totalSamples = Mathf.RoundToInt(SampleRate * duration);
            float[] raw = new float[totalSamples];
            uint rng = 613517u;
            float lpLow = 0f;
            float lpMid = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float white = NextWhite(ref rng);

                lpLow = lpLow * 0.985f + white * 0.015f;
                lpMid = lpMid * 0.88f + white * 0.12f;

                float crowdMod = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                float voiceBand = (lpMid - lpLow * 0.5f) * crowdMod;
                float engineIdle = (Mathf.Sin(2f * Mathf.PI * 46f * t) * 0.08f + Mathf.Sin(2f * Mathf.PI * 92f * t) * 0.04f)
                                   * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 1.5f * t));

                raw[i] = lpLow * 1.6f + voiceBand * 1.9f + engineIdle;
            }

            MakeSeamlessLoop(raw, overlapSamples: SampleRate / 2);
            Normalize(raw, peakTarget: 0.66f);
            streetMurmurClip = BuildClip("Proc_StreetMurmur", raw, loop: true);
            return streetMurmurClip;
        }

        private static void AddHeartbeatPulse(float[] data, float startSec, float durSec, float fStart, float fEnd, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            float phase = 0f;

            for (int i = 0; i < len && (startIdx + i) < data.Length; i++)
            {
                float norm = i / (float)len;
                float freq = Mathf.Lerp(fStart, fEnd, norm);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Sin(Mathf.PI * norm) * Mathf.Exp(-norm * 2.2f);
                float wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f);
                // Saturacion suave calida
                float warm = (float)System.Math.Tanh(wave * 1.4f);
                data[startIdx + i] += warm * env * amp;
            }
        }

        private static void MakeSeamlessLoop(float[] data, int overlapSamples)
        {
            int len = data.Length;
            int n = Mathf.Min(overlapSamples, len / 4);
            for (int i = 0; i < n; i++)
            {
                float w = 0.5f * (1f - Mathf.Cos(Mathf.PI * i / n));
                int tailIdx = len - n + i;
                float blended = data[i] * w + data[tailIdx] * (1f - w);
                data[i] = blended;
                data[tailIdx] = blended;
            }
        }

        private static void Normalize(float[] data, float peakTarget)
        {
            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > peak) peak = a;
            }

            float scale = peakTarget / peak;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i] * scale, -1f, 1f);
            }
        }

        private static void NormalizeAndSmoothEnds(float[] data, int fadeSamples)
        {
            Normalize(data, 0.92f);
            int n = Mathf.Min(fadeSamples, data.Length / 4);
            for (int i = 0; i < n; i++)
            {
                float w = i / (float)n;
                data[i] *= w;
                data[data.Length - 1 - i] *= w;
            }
        }

        private static AudioClip BuildClip(string name, float[] data, bool loop)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float NextWhite(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return ((state & 0x00FFFFFF) / 8388608f) - 1f;
        }
    }
}
