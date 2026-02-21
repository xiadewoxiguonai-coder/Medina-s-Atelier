using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Barracuda;
using System.Linq;

public class RuneThreeModelRocognize : MonoBehaviour
{
    [Header("Audio Settings")]
    public int sampleRate = 48000;
    public int channelCount = 1;
    public float recordDuration = 1.5f;

    [Header("UI References")]
    public TMP_Dropdown microphoneDropdown;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI confidenceText;
    public Button recordButton;
    public Slider volumeSlider;

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

    private string selectedMicDevice;
    private bool isRecording = false;
    public AudioClip currentRecordingClip;

    private IWorker[] workers = new IWorker[3];
    private Model[] models = new Model[3];

    private int laguzIndex = -1;
    private int ingwazIndex = -1;

    private float[] volumeSamples;
    private const int sampleWindow = 128;

    private void Awake()
    {
        SetupMicrophone();
        SetupModels();
        SetupSpecialIndices();
        volumeSamples = new float[sampleWindow];
        if (recordButton != null)
            recordButton.onClick.AddListener(OnRecordButtonClick);
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
        if (microphoneDropdown != null)
        {
            microphoneDropdown.ClearOptions();
            List<string> micOptions = new List<string>();
            if (Microphone.devices != null && Microphone.devices.Length > 0)
            {
                micOptions.AddRange(Microphone.devices);
                selectedMicDevice = Microphone.devices[0];
            }
            else
            {
                micOptions.Add("No Microphone");
                selectedMicDevice = null;
            }
            microphoneDropdown.AddOptions(micOptions);
            microphoneDropdown.onValueChanged.AddListener(OnMicDeviceSelected);
        }
        else if (Microphone.devices.Length > 0)
        {
            selectedMicDevice = Microphone.devices[0];
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

            Debug.Log($"Model {i + 1} inputs: {models[i].inputs.Count}");
            for (int j = 0; j < models[i].inputs.Count; j++)
            {
                var input = models[i].inputs[j];
                Debug.Log($"  Input {j}: {input.name} - shape: {string.Join(", ", input.shape)}");
            }
        }

        if (runeNames.Length != 24)
        {
            Debug.LogError($"Rune names count ({runeNames.Length}) != 24! Fix the array.");
        }
    }

    private void Update()
    {
        if (isRecording)
        {
            UpdateVolumeUI();
            if (Microphone.GetPosition(selectedMicDevice) >= sampleRate * recordDuration)
            {
                StopRecordingAndPredict();
            }
        }
    }

    private void UpdateVolumeUI()
    {
        if (currentRecordingClip == null || volumeSlider == null) return;
        int micPosition = Microphone.GetPosition(selectedMicDevice);
        if (micPosition < sampleWindow) return;

        float[] data = new float[sampleWindow];
        currentRecordingClip.GetData(data, micPosition - sampleWindow);

        float sum = 0f;
        foreach (float sample in data)
            sum += Mathf.Abs(sample);

        float volume = Mathf.Clamp01(sum / sampleWindow);
        volumeSlider.value = volume;
    }

    public void OnRecordButtonClick()
    {
        if (isRecording) StopRecordingAndPredict();
        else StartRecording();
    }

    public void StartRecording()
    {
        if (isRecording) return;
        if (string.IsNullOrEmpty(selectedMicDevice))
        {
            Debug.LogError("No microphone selected!");
            ShowResult("Error: No Microphone", 0);
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

            if (recordButton != null)
            {
                TextMeshProUGUI buttonText = recordButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Recording...";
            }
            if (resultText != null) resultText.text = "Listening...";
            if (confidenceText != null) confidenceText.text = "";

            Debug.Log($"Recording started: {selectedMicDevice} @ {sampleRate}Hz, duration={recordDuration}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"Recording failed: {e.Message}");
            ShowResult("Error: " + e.Message, 0);
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

            if (recordButton != null)
            {
                TextMeshProUGUI buttonText = recordButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Record";
            }

            int maxSamples = Mathf.Min(finalPosition, (int)(sampleRate * recordDuration));

            if (currentRecordingClip == null || maxSamples < sampleRate * 0.3f)
            {
                ShowResult("Error: Recording too short", 0);
                return;
            }

            float[] audioData = ExtractAudioData(currentRecordingClip, maxSamples);

            if (audioData == null || audioData.Length < sampleRate * 0.3f)
            {
                ShowResult("Error: Audio too short", 0);
                return;
            }

            Debug.Log($"Audio extracted: {audioData.Length} samples ({audioData.Length / (float)sampleRate:F2}s)");

            StartCoroutine(PredictCoroutine(audioData));
        }
        catch (Exception e)
        {
            Debug.LogError($"Prediction failed: {e.Message}\n{e.StackTrace}");
            ShowResult("Error: " + e.Message, 0);
            isRecording = false;
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
        if (resultText != null) resultText.text = "Processing...";

        if (voiceType != VoiceChange.VoiceType.Original)
        {
            Debug.Log($"Applying voice effect: {voiceType}");
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
            ShowResult("Error: Feature extraction failed", 0);
            yield break;
        }

        Debug.Log($"MFCC features shape: [{mfccFeatures.GetLength(0)}, {mfccFeatures.GetLength(1)}]");

        string predictedRune;
        float confidence;
        PredictEnsemble(mfccFeatures, out predictedRune, out confidence);

        ShowResult(predictedRune, confidence);
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

            Debug.Log($"Ensemble: M1={runeNames[allPreds[0]]} M2={runeNames[allPreds[1]]} M3={runeNames[allPreds[2]]} | Votes: Laguz={laguzVotes} Ingwaz={ingwazVotes} | Final: {runeName} ({confidence:P2})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ensemble inference failed: {e.Message}\n{e.StackTrace}");
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

    private void ShowResult(string runeName, float confidence)
    {
        if (resultText != null)
        {
            resultText.text = runeName;
            if (confidence > 0.8f) resultText.color = Color.green;
            else if (confidence > 0.5f) resultText.color = Color.yellow;
            else resultText.color = Color.red;
        }

        if (confidenceText != null)
            confidenceText.text = $"Confidence: {confidence:P1}";
    }

    public void OnMicDeviceSelected(int index)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            selectedMicDevice = null;
            return;
        }
        if (index >= 0 && index < Microphone.devices.Length)
        {
            selectedMicDevice = Microphone.devices[index];
            Debug.Log($"Selected microphone: {selectedMicDevice}");
        }
    }

    public void SetVoiceType(VoiceChange.VoiceType type)
    {
        voiceType = type;
        voiceChanger?.SetVoiceType(type);
        Debug.Log($"Voice type changed to: {type}");
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