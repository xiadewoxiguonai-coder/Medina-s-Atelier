using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Barracuda;
using System.Linq;

public class RuneVoiceRecognizer : MonoBehaviour
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

    [Header("Model References")]
    public NNModel onnxModel;
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

    private string selectedMicDevice;
    private bool isRecording = false;
    public AudioClip currentRecordingClip;
    private IWorker worker;
    private Model model;

    private float[] volumeSamples;
    private const int sampleWindow = 128;

    private void Awake()
    {
        SetupMicrophone();
        SetupModel();
        volumeSamples = new float[sampleWindow];
        if (recordButton != null)
            recordButton.onClick.AddListener(OnRecordButtonClick);
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

    private void SetupModel()
    {
        if (onnxModel == null)
        {
            Debug.LogError("ONNX model not assigned!");
            return;
        }

        model = ModelLoader.Load(onnxModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        Debug.Log($"Model inputs: {model.inputs.Count}");
        for (int i = 0; i < model.inputs.Count; i++)
        {
            var input = model.inputs[i];
            Debug.Log($"Input {i}: {input.name} - shape: {string.Join(", ", input.shape)}");
        }

        Debug.Log($"Model outputs: {model.outputs.Count}");
        for (int i = 0; i < model.outputs.Count; i++)
        {
            var output = model.outputs[i];
            Debug.Log($"Output {i}: {output}");
        }

        Debug.Log($"Rune names count: {runeNames.Length}");

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
        Predict(mfccFeatures, out predictedRune, out confidence);

        ShowResult(predictedRune, confidence);
    }

    private void Predict(float[,] mfccFeatures, out string runeName, out float confidence)
    {
        runeName = "Unknown";
        confidence = 0;

        if (worker == null)
        {
            Debug.LogError("Model not loaded!");
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

            worker.Execute(inputTensor);
            var outputTensor = worker.PeekOutput();

            float[] logits = outputTensor.ToReadOnlyArray();

            Debug.Log($"Output logits count: {logits.Length}");

            float[] probabilities = Softmax(logits);

            int maxIndex = 0;
            for (int i = 1; i < probabilities.Length; i++)
            {
                if (probabilities[i] > probabilities[maxIndex])
                    maxIndex = i;
            }

            if (maxIndex < runeNames.Length && probabilities[maxIndex] >= confidenceThreshold)
            {
                runeName = runeNames[maxIndex];
                confidence = probabilities[maxIndex];
            }
            else if (maxIndex < runeNames.Length)
            {
                runeName = runeNames[maxIndex];
                confidence = probabilities[maxIndex];
                Debug.LogWarning($"Low confidence: {confidence:P2} < threshold {confidenceThreshold:P2}");
            }
            else
            {
                runeName = $"Class_{maxIndex}";
                Debug.LogWarning($"Predicted class {maxIndex} exceeds runeNames length {runeNames.Length}");
            }

            inputTensor.Dispose();
            outputTensor.Dispose();

            Debug.Log($"Predicted: {runeName} ({confidence:P2}), class index: {maxIndex}/{probabilities.Length}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Inference failed: {e.Message}\n{e.StackTrace}");
        }
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

    private void OnDestroy()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedMicDevice))
            Microphone.End(selectedMicDevice);
        worker?.Dispose();
    }
}