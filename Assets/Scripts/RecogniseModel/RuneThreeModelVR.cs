using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Linq;

public class RuneThreeModelRecognize_VROnly : MonoBehaviour
{
    [Header("Audio Settings")]
    public int sampleRate = 48000;
    public int channelCount = 1;
    public float recordDuration = 2f; //

    [Header("Model References - 3 Model Ensemble")]
    public NNModel onnxModel1;
    public NNModel onnxModel2;
    public NNModel onnxModel3;
    public MFCCExtractorFixed mfccExtractor;

    [Header("Rune Names (24 classes) - Must match training!")]
    public string[] runeNames = new string[] {
        "Algiz", "Ansuz", "Berkano", "Dagaz", "Ehwaz",
        "Eihwaz", "Fehu", "Gebo", "Hagalaz", "Ingwaz",
        "Isa", "Jera", "Kenaz", "Laguz", "Mannaz",
        "Nauthiz", "Othala", "Perthro", "Raido", "Sowilo",
        "Thurisaz", "Tiwaz", "Uruz", "Wunjo"
    };

    [Header("Recognition Settings")]
    [Range(0.0f, 1.0f)]
    public float confidenceThreshold = 0.1f;

    [Header("Voice Changer")]
    public VoiceChange.VoiceType voiceType = VoiceChange.VoiceType.Original;
    private VoiceChange voiceChanger;

    [Header("Ensemble Settings")]
    public int laguzRequiredVotes = 2;
    public float laguzProbabilityThreshold = 0.6f;
    public int ingwazRequiredVotes = 2;

    public event Action<string, float> OnRuneRecognitionComplete;

    private string selectedMicDevice;
    public bool isRecording { get; private set; }
    public AudioClip currentRecordingClip;

    private IWorker[] workers = new IWorker[3];
    private Model[] models = new Model[3];

    private int laguzIndex = -1;
    private int ingwazIndex = -1;

    private void Awake()
    {
        SetupMicrophone();
        SetupModels();
        SetupSpecialIndices();
        voiceChanger = new VoiceChange(sampleRate);
        voiceChanger.SetVoiceType(voiceType);
    }

    private void SetupSpecialIndices()
    {
        for (int i = 0; i < runeNames.Length; i++)
        {
            if (runeNames[i] == "Laguz") laguzIndex = i;
            if (runeNames[i] == "Ingwaz") ingwazIndex = i;
        }
    }

    private void SetupMicrophone()
    {
        //
        if (Microphone.devices.Length > 0)
        {
            selectedMicDevice = Microphone.devices[0];
            Debug.Log($"choice: {selectedMicDevice}");
        }
        else
        {
            Debug.LogError("not found！");
        }
    }

    private void SetupModels()
    {
        NNModel[] modelAssets = { onnxModel1, onnxModel2, onnxModel3 };

        for (int i = 0; i < 3; i++)
        {
            if (modelAssets[i] == null)
            {
                Debug.LogError($"ONNX model {i + 1} not assigned!");
                continue;
            }

            models[i] = ModelLoader.Load(modelAssets[i]);
            workers[i] = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, models[i]);
            Debug.Log($"Model {i + 1} loaded successfully");
        }

        if (runeNames.Length != 24)
        {
            Debug.LogError($"Rune names count ({runeNames.Length}) != 24! Fix the array.");
        }
    }

    private void Update()
    {
        // 
        if (isRecording && Microphone.GetPosition(selectedMicDevice) >= sampleRate * recordDuration)
        {
            StopRecordingAndPredict();
        }
    }

    //
    public void StartRecording()
    {
        if (isRecording || string.IsNullOrEmpty(selectedMicDevice))
        {
            Debug.LogWarning("could not start record!");
            return;
        }

        try
        {
            if (Microphone.IsRecording(selectedMicDevice))
                Microphone.End(selectedMicDevice);

            currentRecordingClip = Microphone.Start(
                selectedMicDevice,
                false,
                Mathf.CeilToInt(recordDuration),
                sampleRate
            );

            if (currentRecordingClip == null)
            {
                Debug.LogError("Failed to start microphone!");
                return;
            }

            float startWaitTime = Time.time;
            while (Microphone.GetPosition(selectedMicDevice) <= 0 &&
                   Time.time - startWaitTime < 1f) { }

            isRecording = true;
            Debug.Log($"Recording started (VR Mode): {selectedMicDevice} @ {sampleRate}Hz");
        }
        catch (Exception e)
        {
            Debug.LogError($"Recording failed: {e.Message}");
            isRecording = false;
            currentRecordingClip = null;
        }
    }

    public void StopRecordingAndPredict()
    {
        if (!isRecording) return;

        try
        {
            int finalPosition = Microphone.GetPosition(selectedMicDevice);
            Microphone.End(selectedMicDevice);
            isRecording = false;

            int maxSamples = Mathf.Min(finalPosition, (int)(sampleRate * recordDuration));

            if (currentRecordingClip == null || maxSamples < sampleRate * 0.3f)
            {
                Debug.LogWarning("is short!");
                OnRuneRecognitionComplete?.Invoke("TooShort", 0f);
                return;
            }

            float[] audioData = ExtractAudioData(currentRecordingClip, maxSamples);

            if (audioData == null || audioData.Length < sampleRate * 0.3f)
            {
                Debug.LogWarning("could not using");
                OnRuneRecognitionComplete?.Invoke("AudioError", 0f);
                return;
            }

            StartCoroutine(PredictCoroutine(audioData));
        }
        catch (Exception e)
        {
            Debug.LogError($"Prediction failed: {e.Message}");
            isRecording = false;
            OnRuneRecognitionComplete?.Invoke("Error", 0f);
        }
    }

    private float[] ExtractAudioData(AudioClip clip, int sampleCount)
    {
        int channels = clip.channels;
        sampleCount = Mathf.Min(sampleCount, clip.samples);

        float[] rawData = new float[sampleCount * channels];
        clip.GetData(rawData, 0);

        if (channels > 1)
        {
            float[] monoData = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += rawData[i * channels + c];
                monoData[i] = sum / channels;
            }
            return monoData;
        }

        return rawData;
    }

    private IEnumerator PredictCoroutine(float[] audioData)
    {
        if (voiceType != VoiceChange.VoiceType.Original)
        {
            audioData = voiceChanger.Process(audioData);
        }

        float[,] mfccFeatures = null;
        bool done = false;

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                mfccFeatures = mfccExtractor.ExtractMFCC124D(audioData);
                done = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"MFCC extraction failed: {e.Message}");
                done = true;
            }
        });

        while (!done) yield return null;

        if (mfccFeatures == null)
        {
            OnRuneRecognitionComplete?.Invoke("FeatureError", 0f);
            yield break;
        }

        PredictEnsemble(mfccFeatures, out string predictedRune, out float confidence);

        
        OnRuneRecognitionComplete?.Invoke(predictedRune, confidence);
        Debug.Log($"rune={predictedRune} percentage={confidence:P2}");
    }

    private void PredictEnsemble(float[,] mfccFeatures, out string runeName, out float confidence)
    {
        runeName = "Unknown";
        confidence = 0;

        if (workers.Any(w => w == null))
        {
            Debug.LogError("Some models not loaded!");
            return;
        }

        try
        {
            var inputTensor = new Tensor(1, 100, 124, 1);
            for (int h = 0; h < 100; h++)
            {
                for (int w = 0; w < 124; w++)
                {
                    float value = (h < mfccFeatures.GetLength(0) && w < mfccFeatures.GetLength(1))
                        ? mfccFeatures[h, w]
                        : 0f;
                    inputTensor[0, h, w, 0] = value;
                }
            }

            float[][] allProbs = new float[3][];
            int[] allPreds = new int[3];

            for (int i = 0; i < 3; i++)
            {
                workers[i].Execute(inputTensor);
                var outputTensor = workers[i].PeekOutput();
                float[] logits = outputTensor.ToReadOnlyArray();

                allProbs[i] = Softmax(logits);
                allPreds[i] = GetMaxIndex(allProbs[i]);
                outputTensor.Dispose();
            }

            inputTensor.Dispose();

            float[] avgProbs = new float[runeNames.Length];
            for (int c = 0; c < runeNames.Length; c++)
            {
                avgProbs[c] = (allProbs[0][c] + allProbs[1][c] + allProbs[2][c]) / 3f;
            }

            int laguzVotes = allPreds.Count(p => p == laguzIndex);
            int ingwazVotes = allPreds.Count(p => p == ingwazIndex);

            if (laguzIndex >= 0 && (laguzVotes < laguzRequiredVotes || avgProbs[laguzIndex] < laguzProbabilityThreshold))
            {
                avgProbs[laguzIndex] *= 0.3f;
            }

            if (ingwazIndex >= 0 && ingwazVotes < ingwazRequiredVotes)
            {
                avgProbs[ingwazIndex] *= 0.7f;
            }

            float sum = avgProbs.Sum();
            if (sum > 0)
            {
                for (int i = 0; i < avgProbs.Length; i++)
                    avgProbs[i] /= sum;
            }

            int maxIndex = GetMaxIndex(avgProbs);

            if (maxIndex < runeNames.Length)
            {
                runeName = runeNames[maxIndex];
                confidence = avgProbs[maxIndex];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ensemble inference failed: {e.Message}");
        }
    }

    private int GetMaxIndex(float[] probs)
    {
        int maxIndex = 0;
        for (int i = 1; i < probs.Length; i++)
        {
            if (probs[i] > probs[maxIndex])
                maxIndex = i;
        }
        return maxIndex;
    }

    private float[] Softmax(float[] logits)
    {
        if (logits == null || logits.Length == 0) return new float[0];

        float max = logits.Max();
        float sum = 0f;
        float[] exp = new float[logits.Length];

        for (int i = 0; i < logits.Length; i++)
        {
            exp[i] = Mathf.Exp(logits[i] - max);
            sum += exp[i];
        }

        for (int i = 0; i < logits.Length; i++)
            exp[i] = sum > 0 ? exp[i] / sum : 0f;

        return exp;
    }

    
    public void OnMicDeviceSelected(int index)
    {
        if (Microphone.devices.Length == 0) return;
        if (index >= 0 && index < Microphone.devices.Length)
        {
            selectedMicDevice = Microphone.devices[index];
         
        }
    }

    public void SetVoiceType(VoiceChange.VoiceType type)
    {
        voiceType = type;
        voiceChanger?.SetVoiceType(type);
    }

    private void OnDestroy()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedMicDevice))
            Microphone.End(selectedMicDevice);

        foreach (var worker in workers)
        {
            worker?.Dispose();
        }
    }
}