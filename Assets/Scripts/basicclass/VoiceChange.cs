using System;
using System.Linq;
using UnityEngine;

public class VoiceChange
{
    public enum VoiceType
    {
        Original,   // No change
        Female,     // (+7 semitones)
        Elderly,    // (-3.5 semitones)
        MaleDeep    // (-5 semitones)
    }

    private int sampleRate;
    private VoiceType currentType;

    // Filter states (for continuity)
    private float hpPrevInput, hpPrevOutput;
    private float lpPrevOutput;
    private float compressorEnvelope;

    public VoiceChange(int sampleRate = 48000)
    {
        this.sampleRate = sampleRate;
        this.currentType = VoiceType.Original;
    }

    public void SetVoiceType(VoiceType type)
    {
        currentType = type;
        ResetFilters();
    }

    public float[] Process(float[] input)
    {
        if (currentType == VoiceType.Original || input == null || input.Length == 0)
            return input;

        float[] samples = (float[])input.Clone();

        samples = ApplyPitchShift(samples, GetSemitones(currentType));

        switch (currentType)
        {
            case VoiceType.Female:
                samples = ApplyHighPass(samples, 200f);
                samples = ApplyCompressor(samples, -18f, 3f);
                samples = ApplyLowPass(samples, 12000f);
                samples = ApplyGain(samples, 3f);
                break;

            case VoiceType.Elderly:
                samples = ApplyLowPass(samples, 3500f);
                samples = ApplyCompressor(samples, -20f, 2.5f);
                samples = ApplyGain(samples, 4f);
                samples = ApplySaturation(samples, 0.05f);
                break;

            case VoiceType.MaleDeep:
                samples = ApplyLowPass(samples, 4000f);
                samples = ApplyCompressor(samples, -16f, 4f);
                samples = ApplyGain(samples, 2f);
                break;
        }

        samples = Normalize(samples);

        return samples;
    }

    private float GetSemitones(VoiceType type)
    {
        return type switch
        {
            VoiceType.Female => 7f,
            VoiceType.Elderly => -3.5f,
            VoiceType.MaleDeep => -5f,
            _ => 0f
        };
    }

    private void ResetFilters()
    {
        hpPrevInput = hpPrevOutput = 0;
        lpPrevOutput = 0;
        compressorEnvelope = 0;
    }

    private float[] ApplyPitchShift(float[] samples, float semitones)
    {
        float ratio = Mathf.Pow(2f, semitones / 12f);
        int newLength = Mathf.RoundToInt(samples.Length / ratio);
        float[] output = new float[newLength];

        float readPos = 0;
        for (int i = 0; i < newLength; i++)
        {
            int idx = Mathf.FloorToInt(readPos);
            float frac = readPos - idx;

            if (idx >= samples.Length - 1) break;

            output[i] = samples[idx] * (1 - frac) + samples[idx + 1] * frac;
            readPos += ratio;
        }

        if (output.Length < samples.Length)
        {
            Array.Resize(ref output, samples.Length);
        }

        return output;
    }

    private float[] ApplyHighPass(float[] samples, float cutoff)
    {
        float rc = 1f / (2f * Mathf.PI * cutoff);
        float alpha = rc / (rc + 1f / sampleRate);

        float[] output = new float[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            output[i] = alpha * (hpPrevOutput + samples[i] - hpPrevInput);
            hpPrevInput = samples[i];
            hpPrevOutput = output[i];
        }

        return output;
    }

    private float[] ApplyLowPass(float[] samples, float cutoff)
    {
        float rc = 1f / (2f * Mathf.PI * cutoff);
        float alpha = (1f / sampleRate) / (rc + 1f / sampleRate);

        float[] output = new float[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            output[i] = lpPrevOutput + alpha * (samples[i] - lpPrevOutput);
            lpPrevOutput = output[i];
        }

        return output;
    }

    private float[] ApplyCompressor(float[] samples, float thresholdDb, float ratio)
    {
        float threshold = Mathf.Pow(10, thresholdDb / 20f);
        float[] output = new float[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            float inputAbs = Mathf.Abs(samples[i]);

            compressorEnvelope = Mathf.Max(inputAbs, compressorEnvelope * 0.99f);

            float gain = 1f;
            if (compressorEnvelope > threshold)
            {
                float reduction = Mathf.Pow(compressorEnvelope / threshold, 1f - 1f / ratio);
                gain = threshold / compressorEnvelope * reduction;
            }

            output[i] = samples[i] * gain;
        }

        return output;
    }

    private float[] ApplyGain(float[] samples, float gainDb)
    {
        float gain = Mathf.Pow(10, gainDb / 20f);
        return samples.Select(s => s * gain).ToArray();
    }

    private float[] ApplySaturation(float[] samples, float amount)
    {
        return samples.Select(s => Mathf.Tan(s * amount) / amount).ToArray();
    }

    private float[] Normalize(float[] samples)
    {
        float max = samples.Select(Mathf.Abs).Max();
        if (max > 0.95f)
        {
            float scale = 0.95f / max;
            return samples.Select(s => s * scale).ToArray();
        }
        return samples;
    }
}