using System;
using UnityEngine;

/// <summary>
/// ตัวสังเคราะห์เสียงเอฟเฟกต์สไตล์ 8-bit Famicom / Arcade (PCM AudioClip)
/// สำหรับช่อง SFX ที่ยังไม่ได้ลากไฟล์เสียงมาใส่ใน AudioManager
/// </summary>
public static class RetroSFXSynth
{
    private const int SampleRate = 44100;

    private static AudioClip cachedMeatThrow;
    private static AudioClip cachedMeatHit;
    private static AudioClip cachedMeatBounce;
    private static AudioClip cachedExtraLife;
    private static AudioClip cachedBalloonInflate;
    private static AudioClip cachedBoulderFall;
    private static AudioClip cachedFruitDrop;
    private static AudioClip cachedFruitHit;
    private static AudioClip cachedButtonClick;
    private static AudioClip cachedGroundRun;
    private static AudioClip cachedSnowRun;
    private static AudioClip cachedRainAmbience;

    public static AudioClip GetMeatThrow() => cachedMeatThrow != null ? cachedMeatThrow : (cachedMeatThrow = BuildMeatThrow());
    public static AudioClip GetMeatHit() => cachedMeatHit != null ? cachedMeatHit : (cachedMeatHit = BuildMeatHit());
    public static AudioClip GetMeatBounce() => cachedMeatBounce != null ? cachedMeatBounce : (cachedMeatBounce = BuildMeatBounce());
    public static AudioClip GetExtraLife() => cachedExtraLife != null ? cachedExtraLife : (cachedExtraLife = BuildExtraLife());
    public static AudioClip GetBalloonInflate() => cachedBalloonInflate != null ? cachedBalloonInflate : (cachedBalloonInflate = BuildBalloonInflate());
    public static AudioClip GetBoulderFall() => cachedBoulderFall != null ? cachedBoulderFall : (cachedBoulderFall = BuildBoulderFall());
    public static AudioClip GetFruitDrop() => cachedFruitDrop != null ? cachedFruitDrop : (cachedFruitDrop = BuildFruitDrop());
    public static AudioClip GetFruitHit() => cachedFruitHit != null ? cachedFruitHit : (cachedFruitHit = BuildFruitHit());
    public static AudioClip GetButtonClick() => cachedButtonClick != null ? cachedButtonClick : (cachedButtonClick = BuildButtonClick());
    public static AudioClip GetGroundRun() => cachedGroundRun != null ? cachedGroundRun : (cachedGroundRun = BuildGroundRun());
    public static AudioClip GetSnowRun() => cachedSnowRun != null ? cachedSnowRun : (cachedSnowRun = BuildSnowRun());
    public static AudioClip GetRainAmbience() => cachedRainAmbience != null ? cachedRainAmbience : (cachedRainAmbience = BuildRainAmbience());

    private static AudioClip BuildMeatThrow()
    {
        const float duration = 0.16f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        uint rng = 0x51A7F31u;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = u < 0.42f
                ? Mathf.Lerp(260f, 620f, u / 0.42f)
                : Mathf.Lerp(620f, 290f, (u - 0.42f) / 0.58f);

            phase += freq / SampleRate;
            float env = Envelope(t, duration, 0.004f, 0.04f);
            float tone = PulseWave(phase, 0.25f) * 0.65f + TriangleWave(phase) * 0.35f;
            float air = NextNoise(ref rng) * 0.15f * (1f - u);
            samples[i] = (tone + air) * env * 0.34f;
        }

        return CreateClip("SFX_MeatThrow_Retro", samples);
    }

    private static AudioClip BuildMeatHit()
    {
        const float duration = 0.15f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        uint rng = 0x8F23C41u;
        float lpNoise = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(480f, 95f, Mathf.Pow(u, 0.7f));
            phase += freq / SampleRate;

            float noise = NextNoise(ref rng);
            lpNoise = Mathf.Lerp(lpNoise, noise, 0.25f);
            float punchBurst = u < 0.32f ? (1f - u / 0.32f) : 0f;

            float env = Envelope(t, duration, 0.003f, 0.06f);
            float sig = TriangleWave(phase) * 0.75f + lpNoise * punchBurst * 0.65f;
            samples[i] = sig * env * 0.42f;
        }

        return CreateClip("SFX_MeatHit_Retro", samples);
    }

    private static AudioClip BuildMeatBounce()
    {
        const float duration = 0.14f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = u < 0.55f
                ? Mathf.Lerp(260f, 780f, u / 0.55f)
                : Mathf.Lerp(780f, 560f, (u - 0.55f) / 0.45f);
            phase += freq / SampleRate;

            float env = Envelope(t, duration, 0.004f, 0.045f);
            float sig = Mathf.Sin(phase * Mathf.PI * 2f) * 0.6f + TriangleWave(phase) * 0.4f;
            samples[i] = sig * env * 0.36f;
        }

        return CreateClip("SFX_MeatBounce_Retro", samples);
    }

    private static AudioClip BuildExtraLife()
    {
        float[] notes = { 659.25f, 783.99f, 1318.51f, 1046.50f, 1174.66f, 1567.98f };
        const float noteDur = 0.088f;
        float duration = notes.Length * noteDur;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            int noteIdx = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notes.Length - 1);
            float localT = t - noteIdx * noteDur;

            phase += notes[noteIdx] / SampleRate;
            float noteEnv = Envelope(localT, noteDur, 0.004f, 0.024f);
            float sig = PulseWave(phase, 0.5f) * 0.7f + PulseWave(phase * 0.5f, 0.25f) * 0.3f;
            samples[i] = sig * noteEnv * 0.32f;
        }

        return CreateClip("SFX_ExtraLife_Retro", samples);
    }

    private static AudioClip BuildBalloonInflate()
    {
        const float duration = 0.42f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        float subPhase = 0f;
        uint rng = 0x2468ACE1u;
        float smoothNoise = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            // สองจังหวะหายใจเป่า + ยางตึงขึ้นช่วงท้าย
            float swell = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI);
            float freq = Mathf.Lerp(155f, 540f, u * u) + Mathf.Sin(u * 26f) * 14f;
            phase += freq / SampleRate;
            subPhase += (freq * 0.5f) / SampleRate;

            smoothNoise = Mathf.Lerp(smoothNoise, NextNoise(ref rng), 0.12f);
            float airPuff = smoothNoise * (0.45f * (1f - u * 0.45f));
            float rubberTone = TriangleWave(phase) * 0.50f + TriangleWave(subPhase) * 0.25f;
            float env = Envelope(t, duration, 0.02f, 0.08f);
            samples[i] = (rubberTone + airPuff) * swell * env * 0.28f;
        }

        return CreateClip("SFX_BalloonInflate_Retro", samples);
    }

    private static AudioClip BuildBoulderFall()
    {
        const float duration = 0.55f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float lowPhase = 0f;
        float slidePhase = 0f;
        uint rng = 0x77119933u;
        float rumbleNoise = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;

            float lowFreq = Mathf.Lerp(90f, 40f, u);
            float slideFreq = Mathf.Lerp(540f, 130f, u);
            lowPhase += lowFreq / SampleRate;
            slidePhase += slideFreq / SampleRate;

            rumbleNoise = Mathf.Lerp(rumbleNoise, NextNoise(ref rng), 0.08f);
            float env = Envelope(t, duration, 0.01f, 0.14f);
            float sig = TriangleWave(lowPhase) * 0.55f + PulseWave(slidePhase, 0.25f) * 0.22f + rumbleNoise * 0.65f;
            samples[i] = Mathf.Clamp(sig * env * 0.42f, -0.95f, 0.95f);
        }

        return CreateClip("SFX_BoulderFall_Retro", samples);
    }

    private static AudioClip BuildFruitDrop()
    {
        const float duration = 0.15f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(880f, 440f, u);
            phase += freq / SampleRate;

            float env = Envelope(t, duration, 0.004f, 0.045f);
            samples[i] = PulseWave(phase, 0.25f) * env * 0.28f;
        }

        return CreateClip("SFX_FruitDrop_Retro", samples);
    }

    private static AudioClip BuildFruitHit()
    {
        const float duration = 0.20f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        uint rng = 0x99887766u;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float freq = t < 0.07f ? 987.77f : 1479.98f; // B5 -> F#6
            phase += freq / SampleRate;

            float pop = t < 0.03f ? NextNoise(ref rng) * (1f - t / 0.03f) * 0.35f : 0f;
            float env = Envelope(t, duration, 0.003f, 0.07f);
            samples[i] = (PulseWave(phase, 0.5f) * 0.7f + pop) * env * 0.32f;
        }

        return CreateClip("SFX_FruitHit_Retro", samples);
    }

    private static AudioClip BuildButtonClick()
    {
        const float duration = 0.055f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float freq = t < 0.025f ? 880f : 1318.51f;
            phase += freq / SampleRate;

            float env = Envelope(t, duration, 0.002f, 0.015f);
            samples[i] = PulseWave(phase, 0.5f) * env * 0.30f;
        }

        return CreateClip("SFX_ButtonClick_Retro", samples);
    }

    private static AudioClip BuildGroundRun()
    {
        const float duration = 0.048f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        uint rng = 0x3D9B4A17u;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(210f, 82f, u);
            phase += freq / SampleRate;

            float env = Envelope(t, duration, 0.002f, 0.028f);
            float thud = TriangleWave(phase) * 0.65f;
            float grit = NextNoise(ref rng) * 0.25f * (1f - u);
            samples[i] = (thud + grit) * env * 0.24f;
        }

        return CreateClip("SFX_GroundRun_Retro", samples);
    }

    private static AudioClip BuildSnowRun()
    {
        const float duration = 0.095f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float phase = 0f;
        uint rng = 0x7C4F29B1u;
        float lpLow = 0f;
        float lpHigh = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(155f, 68f, u);
            phase += freq / SampleRate;

            // เหยียบหิมะแบบ 2 จังหวะเบาๆ (จิกหิมะ + บดเกล็ดหิมะกรุบๆ)
            float crestA = Mathf.Exp(-Mathf.Pow((u - 0.18f) / 0.17f, 2f));
            float crestB = Mathf.Exp(-Mathf.Pow((u - 0.54f) / 0.24f, 2f)) * 0.85f;
            float crunchShape = crestA + crestB;

            float white = NextNoise(ref rng);
            lpLow = Mathf.Lerp(lpLow, white, 0.06f);
            lpHigh = Mathf.Lerp(lpHigh, white, 0.36f);
            float crispBand = lpHigh - lpLow;

            // เกล็ดหิมะแตกตัวเล็กๆ
            float grain = ((rng & 0x1Fu) == 0u) ? white * 0.45f : 0f;
            float softThud = TriangleWave(phase) * 0.28f * (1f - u * 0.7f);

            float env = Envelope(t, duration, 0.004f, 0.032f);
            samples[i] = Mathf.Clamp((softThud + (crispBand * 1.35f + grain) * crunchShape) * env * 0.68f, -0.9f, 0.9f);
        }

        return CreateClip("SFX_SnowRun_Retro", samples);
    }

    private static AudioClip BuildRainAmbience()
    {
        const float duration = 3.0f;
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] raw = new float[count];
        uint rng = 0x9E3779B9u;
        float lpBed = 0f;
        float lpWarm = 0f;
        float lpAir = 0f;

        // หยดน้ำตกกระทบเป็นระยะเบาๆ เพิ่มมิติฝนธรรมชาติ
        float dropEnv = 0f;
        float dropPhase = 0f;
        float dropFreq = 1100f;
        int nextDropSample = 1800;

        for (int i = 0; i < count; i++)
        {
            float white = NextNoise(ref rng);
            lpBed = Mathf.Lerp(lpBed, white, 0.025f);
            lpWarm = Mathf.Lerp(lpWarm, lpBed, 0.08f);
            lpAir = Mathf.Lerp(lpAir, white, 0.22f);
            float rainWash = lpWarm * 0.62f + (lpAir - lpBed) * 0.22f;

            if (i >= nextDropSample)
            {
                dropEnv = 0.22f + (((rng & 0xFFu) / 255f) * 0.25f);
                dropFreq = 850f + (((rng >> 8) & 0xFFu) / 255f) * 950f;
                dropPhase = 0f;
                nextDropSample = i + 900 + (int)((rng >> 16) & 0xFFFu);
            }

            float dropSig = 0f;
            if (dropEnv > 0.001f)
            {
                dropPhase += dropFreq / SampleRate;
                dropSig = Mathf.Sin(dropPhase * Mathf.PI * 2f) * dropEnv * 0.18f;
                dropEnv *= 0.9965f;
            }

            raw[i] = Mathf.Clamp((rainWash + dropSig) * 0.42f, -0.85f, 0.85f);
        }

        // Crossfade ปลาย-ต้นคลิป (0.25s) เพื่อให้วนลูป Rain Ambience ได้เนียนไม่มีรอยต่อ
        int xfadeSamples = Mathf.Min(count / 4, Mathf.CeilToInt(SampleRate * 0.25f));
        float[] samples = new float[count - xfadeSamples];
        Array.Copy(raw, xfadeSamples, samples, 0, samples.Length);
        for (int i = 0; i < xfadeSamples; i++)
        {
            float t = i / (float)xfadeSamples;
            float fadeIn = Mathf.Sin(t * Mathf.PI * 0.5f);
            float fadeOut = Mathf.Cos(t * Mathf.PI * 0.5f);
            int tailIdx = samples.Length - xfadeSamples + i;
            samples[tailIdx] = samples[tailIdx] * fadeOut + raw[i] * fadeIn;
        }

        return CreateClip("AMB_Rainy_Loop", samples);
    }

    private static AudioClip CreateClip(string clipName, float[] samples)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Envelope(float t, float duration, float attack, float release)
    {
        float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
        float rem = duration - t;
        float r = release > 0f ? Mathf.Clamp01(rem / release) : 1f;
        return a * r;
    }

    private static float PulseWave(float phase, float duty)
    {
        float p = phase - Mathf.Floor(phase);
        return p < duty ? 1f : -1f;
    }

    private static float TriangleWave(float phase)
    {
        float p = phase - Mathf.Floor(phase);
        return 1f - 4f * Mathf.Abs(p - 0.5f);
    }

    private static float NextNoise(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xFFFFu) / 32767.5f - 1f;
    }
}
