using UnityEngine;
using System;
using System.Linq;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

public class MFCCExtractorFixed : MonoBehaviour
{
    [Header("Audio Parameters - Must match Python exactly")]
    public int sampleRate = 48000;
    public int nMfcc = 40;
    public int nFft = 2048;
    public int hopLength = 480;
    public int nMels = 128;
    public int maxFrames = 100;

    private float[,] melFilterBank;
    private float[,] dctMatrix;
    private float[] hannWindow;

    private NativeArray<int> bitReversedIndices;
    private NativeArray<float2> twiddleFactors;

    void Awake()
    {
        PrecomputeMatrices();
        PrecomputeFFTData();
    }

    void OnDestroy()
    {
        if (bitReversedIndices.IsCreated) bitReversedIndices.Dispose();
        if (twiddleFactors.IsCreated) twiddleFactors.Dispose();
    }

    void PrecomputeMatrices()
    {
        hannWindow = new float[nFft];
        for (int i = 0; i < nFft; i++)
            hannWindow[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (nFft - 1)));

        InitializeMelFilterBank();
        InitializeDCTMatrix();
    }

    void PrecomputeFFTData()
    {
        bitReversedIndices = new NativeArray<int>(nFft, Allocator.Persistent);
        int bits = (int)math.log2(nFft);
        for (int i = 0; i < nFft; i++)
        {
            bitReversedIndices[i] = BitReverse(i, bits);
        }

        twiddleFactors = new NativeArray<float2>(nFft, Allocator.Persistent);
        for (int i = 0; i < nFft; i++)
        {
            float angle = -2f * Mathf.PI * i / nFft;
            twiddleFactors[i] = new float2(math.cos(angle), math.sin(angle));
        }
    }

    static int BitReverse(int x, int bits)
    {
        int y = 0;
        for (int i = 0; i < bits; i++)
        {
            y = (y << 1) | ((x >> i) & 1);
        }
        return y;
    }

    float HzToMel(float hz)
    {
        float f_min = 0.0f, f_sp = 200.0f / 3.0f;
        float min_log_hz = 1000.0f, min_log_mel = (min_log_hz - f_min) / f_sp;
        float logstep = Mathf.Log(6.4f) / 27.0f;

        return hz >= min_log_hz ?
            min_log_mel + Mathf.Log(hz / min_log_hz) / logstep :
            (hz - f_min) / f_sp;
    }

    float MelToHz(float mel)
    {
        float f_min = 0.0f, f_sp = 200.0f / 3.0f;
        float min_log_hz = 1000.0f, min_log_mel = (min_log_hz - f_min) / f_sp;
        float logstep = Mathf.Log(6.4f) / 27.0f;

        return mel >= min_log_mel ?
            min_log_hz * Mathf.Exp(logstep * (mel - min_log_mel)) :
            f_min + f_sp * mel;
    }

    void InitializeMelFilterBank()
    {
        int nFreqs = nFft / 2 + 1;
        melFilterBank = new float[nMels, nFreqs];
        float fMin = 0f, fMax = sampleRate / 2f;
        float melMin = HzToMel(fMin), melMax = HzToMel(fMax);
        float[] melPoints = new float[nMels + 2];

        float step = (melMax - melMin) / (nMels + 1);
        for (int i = 0; i < nMels + 2; i++)
            melPoints[i] = melMin + step * i;

        float[,] filters = new float[nMels, nFreqs];
        float[] areas = new float[nMels];

        for (int i = 0; i < nMels; i++)
        {
            float leftHz = MelToHz(melPoints[i]);
            float centerHz = MelToHz(melPoints[i + 1]);
            float rightHz = MelToHz(melPoints[i + 2]);
            areas[i] = (rightHz - leftHz) / 2.0f;

            for (int j = 0; j < nFreqs; j++)
            {
                float freq = j * sampleRate / (float)nFft;
                filters[i, j] = TriangularFilter(freq, leftHz, centerHz, rightHz);
            }
        }

        for (int i = 0; i < nMels; i++)
            if (areas[i] > 0)
                for (int j = 0; j < nFreqs; j++)
                    melFilterBank[i, j] = filters[i, j] / areas[i];
    }

    float TriangularFilter(float freq, float left, float center, float right)
    {
        if (freq <= left || freq >= right) return 0f;
        return freq <= center ? (freq - left) / (center - left) : (right - freq) / (right - center);
    }

    void InitializeDCTMatrix()
    {
        dctMatrix = new float[nMfcc, nMels];
        float scale0 = 1.0f / Mathf.Sqrt(nMels);
        float scaleK = Mathf.Sqrt(2.0f / nMels);

        for (int k = 0; k < nMfcc; k++)
        {
            float scale = (k == 0) ? scale0 : scaleK;
            for (int n = 0; n < nMels; n++)
                dctMatrix[k, n] = scale * Mathf.Cos(Mathf.PI * (n + 0.5f) * k / nMels);
        }
    }

    [BurstCompile(FloatPrecision.High, FloatMode.Default)]
    static void BurstFFT(NativeArray<float2> data, int n,
                         NativeArray<int> bitReversedIndices,
                         NativeArray<float2> twiddleFactors)
    {
        for (int i = 0; i < n; i++)
        {
            int j = bitReversedIndices[i];
            if (j > i)
            {
                var temp = data[i];
                data[i] = data[j];
                data[j] = temp;
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            int halfLen = len >> 1;
            int step = n / len;

            for (int i = 0; i < n; i += len)
            {
                for (int j = 0; j < halfLen; j++)
                {
                    float2 w = twiddleFactors[j * step];
                    float2 u = data[i + j];
                    float2 v = ComplexMul(data[i + j + halfLen], w);

                    data[i + j] = u + v;
                    data[i + j + halfLen] = u - v;
                }
            }
        }
    }

    static float2 ComplexMul(float2 a, float2 b)
    {
        return new float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
    }

    float[] ComputePowerSpectrumBurst(float[] windowed)
    {
        NativeArray<float2> fftData = new NativeArray<float2>(nFft, Allocator.TempJob);
        for (int i = 0; i < nFft; i++)
        {
            fftData[i] = new float2(windowed[i], 0);
        }

        BurstFFT(fftData, nFft, bitReversedIndices, twiddleFactors);

        int nFreqs = nFft / 2 + 1;
        float[] powerSpec = new float[nFreqs];

        powerSpec[0] = fftData[0].x * fftData[0].x;

        for (int i = 1; i < nFreqs - 1; i++)
        {
            powerSpec[i] = fftData[i].x * fftData[i].x + fftData[i].y * fftData[i].y;
        }

        if (nFft % 2 == 0)
        {
            powerSpec[nFreqs - 1] = fftData[nFreqs - 1].x * fftData[nFreqs - 1].x;
        }

        fftData.Dispose();
        return powerSpec;
    }

    public float[,] ExtractMFCC124D(float[] audioData)
    {
        float[] y = TrimSilence(audioData, 25f);
        if (y.Length < hopLength * 3) return null;

        float[,] mfcc = ExtractMFCC(y);
        float[] energy = ExtractRMS(y);
        int nFrames = mfcc.GetLength(0);

        float[,] baseFeatures = new float[nFrames, 41];
        for (int i = 0; i < nFrames; i++)
        {
            for (int j = 0; j < 40; j++)
                baseFeatures[i, j] = mfcc[i, j];
            baseFeatures[i, 40] = energy[i];
        }

        float[,] delta1 = ComputeDelta(baseFeatures, 1);
        float[,] delta2 = ComputeDelta(baseFeatures, 2);

        float[,] features123 = new float[nFrames, 123];
        for (int i = 0; i < nFrames; i++)
        {
            for (int j = 0; j < 41; j++) features123[i, j] = baseFeatures[i, j];
            for (int j = 0; j < 41; j++) features123[i, 41 + j] = delta1[i, j];
            for (int j = 0; j < 41; j++) features123[i, 82 + j] = delta2[i, j];
        }

        int changePointCount = DetectOnset(y);
        float[,] features124 = new float[nFrames, 124];
        for (int i = 0; i < nFrames; i++)
        {
            for (int j = 0; j < 123; j++) features124[i, j] = features123[i, j];
            features124[i, 123] = changePointCount;
        }

        return PadOrTruncate(features124, maxFrames);
    }

    float[] TrimSilence(float[] audio, float topDb)
    {
        int frameLength = nFft;
        int hopLen = 512;
        int nFrames = 1 + (audio.Length - frameLength) / hopLen;
        if (nFrames <= 0) return audio;

        float[] rms = new float[nFrames];
        for (int i = 0; i < nFrames; i++)
        {
            float sumSquares = 0f;
            int start = i * hopLen;
            for (int j = 0; j < frameLength && start + j < audio.Length; j++)
            {
                float sample = audio[start + j];
                sumSquares += sample * sample;
            }
            rms[i] = Mathf.Sqrt(sumSquares / frameLength);
        }

        float eps = 1e-10f;
        float[] db = new float[nFrames];
        float maxDb = -1000f;
        for (int i = 0; i < nFrames; i++)
        {
            db[i] = 20f * Mathf.Log10(rms[i] + eps);
            maxDb = Mathf.Max(maxDb, db[i]);
        }

        float threshold = maxDb - topDb;
        int startFrame = 0, endFrame = nFrames - 1;

        for (int i = 0; i < nFrames; i++)
            if (db[i] > threshold) { startFrame = i; break; }

        for (int i = nFrames - 1; i >= 0; i--)
            if (db[i] > threshold) { endFrame = i; break; }

        int startSample = startFrame * hopLen;
        int endSample = Mathf.Min((endFrame + 1) * hopLen + frameLength, audio.Length);
        if (endSample <= startSample) return audio;

        float[] result = new float[endSample - startSample];
        Array.Copy(audio, startSample, result, 0, result.Length);
        return result;
    }

    float[,] ExtractMFCC(float[] y)
    {
        int padLength = nFft / 2;
        float[] paddedY = new float[y.Length + 2 * padLength];
        Array.Copy(y, 0, paddedY, padLength, y.Length);

        int nFrames = 1 + (paddedY.Length - nFft) / hopLength;
        float[,] mfcc = new float[nFrames, nMfcc];

        for (int frame = 0; frame < nFrames; frame++)
        {
            float[] windowed = new float[nFft];
            int start = frame * hopLength;
            for (int i = 0; i < nFft; i++)
                windowed[i] = paddedY[start + i] * hannWindow[i];

            float[] powerSpec = ComputePowerSpectrumBurst(windowed);
            int nFreqs = nFft / 2 + 1;

            float[] melSpec = new float[nMels];
            for (int i = 0; i < nMels; i++)
            {
                float sum = 0;
                for (int j = 0; j < nFreqs; j++)
                    sum += powerSpec[j] * melFilterBank[i, j];
                melSpec[i] = sum;
            }

            for (int i = 0; i < nMels; i++)
            {
                float db = 10.0f * Mathf.Log10(melSpec[i] + 1e-10f);
                melSpec[i] = Mathf.Max(db, -55.75132f);
            }

            for (int k = 0; k < nMfcc; k++)
            {
                float sum = 0;
                for (int n = 0; n < nMels; n++)
                    sum += melSpec[n] * dctMatrix[k, n];
                mfcc[frame, k] = sum;
            }
        }
        return mfcc;
    }

    float[] ExtractRMS(float[] y)
    {
        int padLength = nFft / 2;
        float[] paddedY = new float[y.Length + 2 * padLength];
        Array.Copy(y, 0, paddedY, padLength, y.Length);

        int nFrames = 1 + (paddedY.Length - nFft) / hopLength;
        float[] rms = new float[nFrames];

        for (int i = 0; i < nFrames; i++)
        {
            float sum = 0;
            int start = i * hopLength;
            for (int j = 0; j < nFft; j++)
            {
                float val = paddedY[start + j];
                sum += val * val;
            }
            rms[i] = Mathf.Sqrt(sum / nFft);
        }
        return rms;
    }

    float[,] ComputeDelta(float[,] features, int order)
    {
        int nFrames = features.GetLength(0);
        int dims = features.GetLength(1);
        float[,] delta = new float[nFrames, dims];
        int width = 9, half = width / 2;

        for (int d = 0; d < dims; d++)
        {
            for (int t = 0; t < nFrames; t++)
            {
                float sum = 0, norm = 0;
                for (int i = -half; i <= half; i++)
                {
                    int idx = Mathf.Clamp(t + i, 0, nFrames - 1);
                    float weight = (order == 1) ? i : (float)(i * i);
                    sum += weight * features[idx, d];
                    norm += weight * weight;
                }
                delta[t, d] = sum / (norm + 1e-8f);
            }
        }
        return delta;
    }

    int DetectOnset(float[] y)
    {
        int onsetHopLength = 512;
        int padLength = nFft / 2;
        float[] paddedY = new float[y.Length + 2 * padLength];
        Array.Copy(y, 0, paddedY, padLength, y.Length);

        int nFrames = 1 + (paddedY.Length - nFft) / onsetHopLength;
        if (nFrames <= 0) return 0;

        float[] onsetEnv = new float[nFrames];
        float[] prevMag = new float[nFft / 2 + 1];

        for (int frame = 0; frame < nFrames; frame++)
        {
            float[] windowed = new float[nFft];
            int start = frame * onsetHopLength;
            for (int i = 0; i < nFft; i++)
                windowed[i] = paddedY[start + i] * hannWindow[i];

            float[] powerSpec = ComputePowerSpectrumBurst(windowed);
            int nFreqs = nFft / 2 + 1;

            float[] mag = new float[nFreqs];
            for (int i = 0; i < nFreqs; i++)
                mag[i] = Mathf.Sqrt(powerSpec[i]);

            float flux = 0;
            for (int i = 0; i < mag.Length; i++)
            {
                float diff = mag[i] - prevMag[i];
                if (diff > 0) flux += diff;
                prevMag[i] = mag[i];
            }
            onsetEnv[frame] = flux;
        }

        float mean = onsetEnv.Average(), std = StdDev(onsetEnv);
        float threshold = mean + 0.07f * std;

        int wait = 3, count = 0, lastOnset = -wait;
        for (int i = 3; i < onsetEnv.Length - 3; i++)
        {
            if (i - lastOnset < wait) continue;

            bool isMax = true;
            for (int j = -3; j < 0; j++)
            {
                if (i + j < 0 || onsetEnv[i] <= onsetEnv[i + j])
                {
                    isMax = false;
                    break;
                }
            }
            if (isMax)
            {
                for (int j = 1; j <= 3; j++)
                {
                    if (i + j >= onsetEnv.Length || onsetEnv[i] <= onsetEnv[i + j])
                    {
                        isMax = false;
                        break;
                    }
                }
            }

            if (isMax && onsetEnv[i] > threshold)
            {
                count++;
                lastOnset = i;
            }
        }
        return count;
    }

    float[,] PadOrTruncate(float[,] features, int targetFrames)
    {
        int currentFrames = features.GetLength(0), nDims = features.GetLength(1);
        float[,] result = new float[targetFrames, nDims];
        int copyFrames = Mathf.Min(currentFrames, targetFrames);
        for (int i = 0; i < copyFrames; i++)
            for (int d = 0; d < nDims; d++)
                result[i, d] = features[i, d];
        return result;
    }

    float StdDev(float[] data)
    {
        float m = data.Average();
        return Mathf.Sqrt(data.Sum(x => (x - m) * (x - m)) / data.Length);
    }
}