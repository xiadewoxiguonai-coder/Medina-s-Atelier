using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Barracuda;
using System.Linq;

/// <summary>
/// Rune Voice Recognition Game with 3 ONNX Models Integration
/// Core functions: Microphone selection, audio recording, model prediction, confidence display
/// </summary>
public class RuneVoiceGame_3Model : MonoBehaviour
{
    public GameObject Startbutton;
    // Added: Reference to record button (for text update)
    public Button recordButton;

    [Header("Microphone Button Clone Settings")]
    public GameObject micButtonPrefab;         // Microphone button prefab
    public Transform micButtonParent;          // Parent transform for microphone buttons
    public Vector3 micButtonStartPos = new Vector3(0, 200, 0);
    public Vector3 micButtonSpacing = new Vector3(0, -80, 0);
    public AudioClip buttonSound;              // Button click sound effect

    [Header("Core UI Texts (Including Confidence)")]
    public TMP_Text currentRuneText;           // Current rune display (only rune symbol)
    public TMP_Text timerText;                 // Remaining time display
    public TMP_Text scoreText;                 // Accuracy & streak display
    public TMP_Text confidenceText;            // Added: Confidence display
    public TMP_Text recognizeResultText;       // Recognition result display
    public Slider volumeSlider;                // Added: Volume slider

    [Header("Game Basic Settings")]
    public float gameDuration = 60f;
    public int sampleRate = 48000;
    public int channelCount = 1;
    public float recordDuration = 1.5f;        // Recording duration (auto stop when reached)

    [Header("Model Settings (3 Models Ensemble)")]
    public NNModel onnxModel1;
    public NNModel onnxModel2;
    public NNModel onnxModel3;
    public MFCCExtractorFixed mfccExtractor;
    [Range(0.0f, 1.0f)]
    public float confidenceThreshold = 0.1f;

    [Header("Ensemble Settings")]              // Restore voting config from old script
    public int laguzRequiredVotes = 2;
    public float laguzProbabilityThreshold = 0.6f;
    public int ingwazRequiredVotes = 2;

    [Header("Rune Settings")]
    public string[] runeNames = new string[] {
        "Algiz", "Ansuz", "Berkano", "Dagaz", "Ehwaz",
        "Eihwaz", "Fehu", "Gebo", "Hagalaz", "Ingwaz",
        "Isa", "Jera", "Kenaz", "Laguz", "Mannaz",
        "Nauthiz", "Othala", "Perthro", "Raido", "Sowilo",
        "Thurisaz", "Tiwaz", "Uruz", "Wunjo"
    };
    private string[] runeSymbols = {
        "ᛉ", "ᚨ", "ᛒ", "ᛞ", "ᛖ",
        "ᛇ", "ᚠ", "ᚷ", "ᚺ", "ᛜ",
        "ᛁ", "ᛃ", "ᚲ", "ᛚ", "ᛗ",
        "ᚾ", "ᛟ", "ᛈ", "ᚱ", "ᛋ",
        "ᚦ", "ᛏ", "ᚢ", "ᚹ"
    };

    // ===================== State Variables =====================
    private string selectedMicDevice;
    private bool isRecording = false;
    private bool isGameRunning = false;
    public AudioClip currentRecordingClip;
    private float remainingGameTime;
    private float recordingElapsedTime;        // Added: Recording timer (for auto stop)
    private int currentRuneIndex;
    private int correctCount = 0;
    private int totalAttempts = 0;
    private int currentStreak = 0;
    private int maxStreak = 0;

    // Model related variables
    private IWorker[] workers = new IWorker[3];
    private Model[] models = new Model[3];
    private int laguzIndex = -1;
    private int ingwazIndex = -1;
    private float[] volumeSamples;
    private const int sampleWindow = 128;

    // Microphone button management
    private List<GameObject> clonedMicButtons = new List<GameObject>();

    private void Awake()
    {
        // Initialize record button click event
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(OnRecordButtonClick);
            // Initialize record button text
            UpdateRecordButtonText("Start Recording");
            // Hide record button when game not started
            recordButton.gameObject.SetActive(false);
        }

        // Clone microphone buttons (only microphones are auto-cloned)
        CloneMicrophoneButtons();
        // Initialize microphone, models and special rune indices
        SetupMicrophone();
        SetupModels();
        SetupSpecialIndices();
        volumeSamples = new float[sampleWindow];

        // Initialize all UI texts (ensure no null values)
        InitializeAllUITexts();
    }

    // ===================== Initialize All UI Texts =====================
    private void InitializeAllUITexts()
    {
        // Initialize current rune text (prompt to select mic, show only rune symbol after game start)
        if (currentRuneText != null)
            currentRuneText.text = "Select microphone first";

        // Initialize remaining time text
        if (timerText != null)
            timerText.text = "01:00";

        // Initialize accuracy text
        if (scoreText != null)
            scoreText.text = "0.0%\n0\n0";

        // Initialize confidence text
        if (confidenceText != null)
            confidenceText.text = "--%";

        // Initialize recognition result text
        if (recognizeResultText != null)
            recognizeResultText.text = "Waiting...";

        // Initialize volume slider
        if (volumeSlider != null)
            volumeSlider.value = 0;
    }

    // ===================== Update Record Button Text =====================
    private void UpdateRecordButtonText(string text)
    {
        if (recordButton == null) return;
        TMP_Text btnText = recordButton.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
            btnText.text = text;
    }

    // ===================== Clone Microphone Buttons (Only auto-clone microphones) =====================
    private void CloneMicrophoneButtons()
    {
        // Clear existing cloned buttons
        foreach (var btn in clonedMicButtons)
            Destroy(btn);
        clonedMicButtons.Clear();

        if (micButtonPrefab == null || micButtonParent == null)
        {
            return;
        }

        // Get microphone list
        List<string> micList = new List<string>();
        if (Microphone.devices != null && Microphone.devices.Length > 0)
            micList.AddRange(Microphone.devices);
        else
            micList.Add("No microphone device");

        for (int i = 0; i < micList.Count; i++)
        {
            int sectionNumber = 3 + i;
            string micName = micList[i];

            // Clone button (UI Canvas compatible)
            GameObject newMicBtn = Instantiate(micButtonPrefab, micButtonParent);
            newMicBtn.name = $"MicButton_{i + 1}";
            newMicBtn.SetActive(true);

            // Key: Use RectTransform for UI relative position instead of localPosition
            RectTransform rt = newMicBtn.GetComponent<RectTransform>();
            if (rt != null)
            {
                // First button at micButtonStartPos, each subsequent button moves down by micButtonSpacing
                rt.anchoredPosition = micButtonStartPos + micButtonSpacing * i;
                // Optional: Set button size to avoid overlapping
                rt.sizeDelta = new Vector2(300, 60); // Width 300, Height 60, adjust according to your panel
            }
            else
            {
                // Use world position if not UI element (3D button compatible)
                newMicBtn.transform.localPosition = micButtonStartPos + micButtonSpacing * i;
            }

            // Configure soundUIbutton script
            soundUIbutton vrBtn = newMicBtn.GetComponent<soundUIbutton>();
            if (vrBtn != null)
                vrBtn.sectionNumber = sectionNumber;

            // Add audio source
            AudioSource audioSource = newMicBtn.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = newMicBtn.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.clip = buttonSound;
            }

            // Set button text (compatible with Chinese/English child objects)
            SetMicButtonText(newMicBtn, i + 1, micName);

            clonedMicButtons.Add(newMicBtn);
            Debug.Log($"Cloned microphone button {i + 1} → sectionNumber={sectionNumber}");
        }
    }

    // ===================== Set Microphone Button Text (Compatible with CN/EN child objects) =====================
    private void SetMicButtonText(GameObject buttonObj, int index, string micName)
    {
        // First try to find English Text
        Transform textChild = buttonObj.transform.Find("Text");
        // If not found, find Chinese text
        if (textChild == null)
            textChild = buttonObj.transform.Find("文本");

        if (textChild != null)
        {
            TMP_Text btnText = textChild.GetComponent<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = $"Microphone {index}: {micName}";
                btnText.fontSize = 20;
            }
        }
    }

    // ===================== Model Initialization =====================
    private void SetupModels()
    {
        NNModel[] modelAssets = { onnxModel1, onnxModel2, onnxModel3 };

        for (int i = 0; i < 3; i++)
        {
            if (modelAssets[i] == null)
            {
                Debug.LogError($"ONNX Model {i + 1} is not assigned!");
                continue;
            }

            models[i] = ModelLoader.Load(modelAssets[i]);
            workers[i] = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, models[i]);

            Debug.Log($"Model {i + 1} loaded successfully, input count: {models[i].inputs.Count}");
            // Restore model input log from old script
            for (int j = 0; j < models[i].inputs.Count; j++)
            {
                var input = models[i].inputs[j];
                Debug.Log($"  Input {j}: {input.name} - shape: {string.Join(", ", input.shape)}");
            }
        }

        if (runeNames.Length != 24)
        {
            Debug.LogError($"Rune names count ({runeNames.Length}) != 24!");
        }
    }

    private void SetupSpecialIndices()
    {
        // Find indices of Laguz and Ingwaz (for voting rules)
        for (int i = 0; i < runeNames.Length; i++)
        {
            if (runeNames[i] == "Laguz") laguzIndex = i;
            if (runeNames[i] == "Ingwaz") ingwazIndex = i;
        }
        Debug.Log($"Laguz index: {laguzIndex}, Ingwaz index: {ingwazIndex}");
    }

    private void SetupMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            selectedMicDevice = Microphone.devices[0];
            if (currentRuneText != null)
                currentRuneText.text = "Select microphone first"; // Keep prompt text
        }
        else
        {
            if (currentRuneText != null)
                currentRuneText.text = "No microphone found";
        }
    }

    // ===================== Game Core Logic =====================
    public void StartGame()
    {
        if (isGameRunning) return;
        if (string.IsNullOrEmpty(selectedMicDevice))
        {
            if (currentRuneText != null)
                currentRuneText.text = "No microphone!";
            return;
        }

        // Reset game state
        isGameRunning = true;
        remainingGameTime = gameDuration;
        correctCount = 0;
        totalAttempts = 0;
        currentStreak = 0;
        maxStreak = 0;

        // Update UI
        ShowRandomRune(); // Show first pure rune symbol
        UpdateTimerUI();
        UpdateScoreUI();

        // Hide start button, show record button
        if (Startbutton != null) Startbutton.SetActive(false);
        if (recordButton != null)
        {
            recordButton.gameObject.SetActive(true);
            UpdateRecordButtonText("Start Recording");
        }
    }

    // ===================== Key Modification: Show only rune symbol, no extra content =====================
    private void ShowRandomRune()
    {
        int newIndex = currentRuneIndex;
        while (newIndex == currentRuneIndex && runeNames.Length > 1)
            newIndex = UnityEngine.Random.Range(0, runeNames.Length);
        currentRuneIndex = newIndex;

        // Show only rune symbol, no extra text
        if (currentRuneText != null)
            currentRuneText.text = runeSymbols[currentRuneIndex];

        // Reset recognition result and confidence
        if (recognizeResultText != null)
            recognizeResultText.text = "Record...";

        if (confidenceText != null)
        {
            confidenceText.text = "--%";
            confidenceText.color = Color.white; // Reset color
        }

        // Reset volume slider
        if (volumeSlider != null)
            volumeSlider.value = 0;
    }

    // ===================== Recording & Recognition Logic (Auto Stop) =====================
    public void OnRecordButtonClick()
    {
        if (!isGameRunning) return;
        if (isRecording)
        {
            // Manual stop recording (keep manual operation)
            StopRecordingAndPredict();
            UpdateRecordButtonText("Start Recording");
        }
        else
        {
            StartRecording();
            UpdateRecordButtonText("Recording...");
        }
    }

    public void StartRecording()
    {
        if (isRecording || !isGameRunning) return;
        if (string.IsNullOrEmpty(selectedMicDevice))
        {
            if (recognizeResultText != null)
                recognizeResultText.text = "No microphone!";
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
                Debug.LogError("Failed to start recording!");
                if (recognizeResultText != null)
                    recognizeResultText.text = "Record failed!";
                return;
            }

            // Wait for microphone to start
            float startWaitTime = Time.time;
            while (Microphone.GetPosition(selectedMicDevice) <= 0 &&
                   Time.time - startWaitTime < 1f) { }

            isRecording = true;
            recordingElapsedTime = 0; // Reset recording timer
            if (recognizeResultText != null)
                recognizeResultText.text = "Recording...";

            Debug.Log($"Recording started: {selectedMicDevice} @ {sampleRate}Hz, duration={recordDuration}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"Recording failed: {e.Message}");
            if (recognizeResultText != null)
                recognizeResultText.text = "Error: " + e.Message;

            isRecording = false;
            UpdateRecordButtonText("Start Recording");
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

            // Verify recording length
            int maxSamples = Mathf.Min(finalPosition, (int)(sampleRate * recordDuration));
            if (currentRecordingClip == null || maxSamples < sampleRate * 0.3f)
            {
                if (recognizeResultText != null)
                    recognizeResultText.text = "Too short!";

                ShowRandomRune();
                return;
            }

            Debug.Log($"Audio extracted: {maxSamples} samples ({maxSamples / (float)sampleRate:F2}s)");

            // Extract audio data (convert to mono)
            float[] audioData = ExtractAudioData(currentRecordingClip, maxSamples);
            if (audioData == null || audioData.Length < sampleRate * 0.3f)
            {
                if (recognizeResultText != null)
                    recognizeResultText.text = "Audio error!";

                ShowRandomRune();
                return;
            }

            // Start recognition coroutine
            StartCoroutine(PredictEnsembleCoroutine(audioData));
        }
        catch (Exception e)
        {
            Debug.LogError($"Prediction failed: {e.Message}\n{e.StackTrace}");
            if (recognizeResultText != null)
                recognizeResultText.text = "Recognition error!";

            isRecording = false;
            UpdateRecordButtonText("Start Recording");
            ShowRandomRune();
        }
    }

    private float[] ExtractAudioData(AudioClip clip, int sampleCount)
    {
        int channels = clip.channels;
        sampleCount = Mathf.Min(sampleCount, clip.samples);

        float[] rawData = new float[sampleCount * channels];
        clip.GetData(rawData, 0);

        // Convert to mono (same logic as old script)
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

    // ===================== 3-Models Ensemble Prediction =====================
    private IEnumerator PredictEnsembleCoroutine(float[] audioData)
    {
        if (recognizeResultText != null)
            recognizeResultText.text = "Processing...";

        // Extract MFCC features asynchronously
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
            if (recognizeResultText != null)
                recognizeResultText.text = "Feature error!";

            ShowRandomRune();
            UpdateRecordButtonText("Start Recording");
            yield break;
        }

        Debug.Log($"MFCC features shape: [{mfccFeatures.GetLength(0)}, {mfccFeatures.GetLength(1)}]");

        // 3-models ensemble prediction
        string predictedRune;
        float confidence;
        PredictEnsemble(mfccFeatures, out predictedRune, out confidence);

        // Show result + confidence
        ShowRecognitionResult(predictedRune, confidence);

        // Update statistics
        totalAttempts++;
        bool isCorrect = predictedRune.Equals(runeNames[currentRuneIndex], StringComparison.OrdinalIgnoreCase);
        if (isCorrect)
        {
            correctCount++;
            currentStreak++;
            maxStreak = Mathf.Max(maxStreak, currentStreak);
        }
        else
        {
            currentStreak = 0;
        }

        // Update score
        UpdateScoreUI();

        // Switch to next rune
        ShowRandomRune();

        // Reset record button text
        UpdateRecordButtonText("Start Recording");
    }

    // ===================== Key Modification: Simplified Debug Log for Voting Process =====================
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

            // ======================================================================
            // Only keep simplified log: Actual / 3 Models / Final / Correct/Incorrect
            // ======================================================================
            string trueLabel = runeNames[currentRuneIndex];
            string m1 = runeNames[allPreds[0]];
            string m2 = runeNames[allPreds[1]];
            string m3 = runeNames[allPreds[2]];
            string final = runeName;
            bool correct = final.Equals(trueLabel, StringComparison.OrdinalIgnoreCase);

            Debug.Log($"[Actual] {trueLabel} | Models: {m1} / {m2} / {m3} | Final: {final} | {(correct ? "Correct" : "Incorrect")}");

        }
        catch (Exception e)
        {
            Debug.LogError($"Inference failed: {e.Message}");
        }
    }

    // ===================== Result Display =====================
    private void ShowRecognitionResult(string runeName, float confidence)
    {
        if (recognizeResultText != null)
        {
            recognizeResultText.text = runeName;
            if (confidence > 0.8f) recognizeResultText.color = Color.green;
            else if (confidence > 0.5f) recognizeResultText.color = Color.yellow;
            else recognizeResultText.color = Color.red;
        }

        // Force refresh confidence to ensure it always updates
        if (confidenceText != null)
        {
            confidenceText.text = confidence.ToString("P1");
        }
    }

    // ===================== Utility Methods =====================
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

    // ===================== UI Update =====================
    private void Update()
    {
        if (!isGameRunning) return;

        // Update countdown
        remainingGameTime -= Time.deltaTime;
        UpdateTimerUI();

        // Game over
        if (remainingGameTime <= 0)
        {
            isGameRunning = false;

            // Update game over text
            if (currentRuneText != null)
                currentRuneText.text = "Game Over";

            if (recognizeResultText != null)
            {
                float finalAccuracy = totalAttempts > 0 ? (float)correctCount / totalAttempts * 100 : 0;
                recognizeResultText.text = $"{finalAccuracy:F1}%";
                recognizeResultText.color = Color.white;
            }

            // Reset confidence text
            if (confidenceText != null)
            {
                confidenceText.text = "--%";
                confidenceText.color = Color.white;
            }

            // Show start button, hide record button
            if (Startbutton != null) Startbutton.SetActive(true);
            if (recordButton != null) recordButton.gameObject.SetActive(false);

            return; // Avoid subsequent logic execution
        }

        // Update recording volume + auto stop recording (core added logic)
        if (isRecording)
        {
            UpdateVolumeUI();

            // Accumulate recording time
            recordingElapsedTime += Time.deltaTime;

            // Auto stop when recording duration is reached
            if (recordingElapsedTime >= recordDuration)
            {
                StopRecordingAndPredict();
                UpdateRecordButtonText("Start Recording");
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingGameTime / 60);
        int seconds = Mathf.FloorToInt(remainingGameTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null) return;

        float accuracy = totalAttempts > 0 ? (float)correctCount / totalAttempts * 100 : 0;
        scoreText.text = $"{accuracy:F1}%\n{currentStreak}\n{maxStreak}";
    }

    private void UpdateVolumeUI()
    {
        // Multi-layer protection: Avoid invalid parameters
        if (currentRecordingClip == null || volumeSlider == null) return;

        int micPosition = Microphone.GetPosition(selectedMicDevice);
        // 1. Invalid microphone position (not started / stopped)
        if (micPosition <= 0 || micPosition < sampleWindow) return;
        // 2. Start position cannot exceed audio length
        int startSample = micPosition - sampleWindow;
        if (startSample < 0 || startSample >= currentRecordingClip.samples) return;

        try
        {
            float[] data = new float[sampleWindow];
            // Read audio data (add exception handling)
            currentRecordingClip.GetData(data, startSample);

            float sum = 0f;
            foreach (float sample in data)
                sum += Mathf.Abs(sample);

            float volume = Mathf.Clamp01(sum / sampleWindow);
            volumeSlider.value = volume; // Bind to volume slider
        }
        catch (Exception e)
        {
            // Catch invalid parameter exception to avoid crash
            Debug.LogWarning($"Volume update failed: {e.Message}");
        }
    }

    // ===================== External Interface (VR Microphone Selection) =====================
    public void OnMicDeviceSelected(int index)
    {
        if (Microphone.devices.Length == 0) return;
        if (index >= 0 && index < Microphone.devices.Length)
        {
            selectedMicDevice = Microphone.devices[index];
            if (currentRuneText != null)
                currentRuneText.text = "Select microphone first"; // Keep prompt text
            Debug.Log($"Selected microphone: {selectedMicDevice}");
        }
    }

    // ===================== Resource Release =====================
    private void OnDestroy()
    {
        // Stop recording
        if (isRecording && !string.IsNullOrEmpty(selectedMicDevice))
            Microphone.End(selectedMicDevice);

        // Release models
        foreach (var worker in workers)
            worker?.Dispose();

        // Clean up microphone buttons
        foreach (var btn in clonedMicButtons)
            Destroy(btn);
    }
}