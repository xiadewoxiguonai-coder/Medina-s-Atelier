using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using UnityEngine.SceneManagement;

public class StartGameArrow : MonoBehaviour
{
    private const int TOTAL_RUNE_COUNT = 24;

    private databaseword useAllWord;
    public int gameMode;
    private List<block> allTrueWordUsingSHOW;
    private List<block> falseBlock1;
    private List<block> falseBlock2;
    private List<block> falseBlock3;
    public string[] allToneSingle;
    public int nowListNumber = 0;
    private float paruseTime = 3f;
    public float nextCreateTime = 0f;
    public float realTime = 0f;
    public GameObject Newblock;
    public GameObject toraw;
    public List<int> allErrorShow;
    public int createNumber = 0;
    public List<word> ralUseWord;
    public int linkNumber = 0;
    public bool goend = false;
    public AudioClip[] allsoundTone;
    public float finalEndTime;
    public AudioClip[] truesound;
    public int havehit = 0;
    public bool isFirst = true;

    private string[] runeSymbols = {
        "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ",
        "ᚺ", "ᚾ", "ᛁ", "ᛃ", "ᛇ", "ᛈ", "ᛉ", "ᛋ",
        "ᛏ", "ᛒ", "ᛖ", "ᛗ", "ᛚ", "ᛜ", "ᛞ", "ᛟ"
    };

    private List<int> correctRuneIds = new List<int>();

    private AudioSource runeAudioSource;
    private AudioSource feedbackAudioSource;

    private Dictionary<int, bool> roundCounted = new Dictionary<int, bool>();

    void Start()
    {
        goend = true;
        firstPlay();
        InitAudioSources();

        roundCounted = new Dictionary<int, bool>();

        int allsoundToneLength = allsoundTone != null ? allsoundTone.Length : 0;
        if (allsoundTone == null || allsoundTone.Length != TOTAL_RUNE_COUNT)
        {
            Debug.LogError($"Rune pronunciation array length error: Current {allsoundToneLength}, Need 24");
        }
        if (truesound == null || truesound.Length < 2)
        {
            Debug.LogError($"Feedback sound array not configured: Need at least 2 audios (correct/error), Current {truesound?.Length ?? 0}");
        }
        else
        {
            Debug.Log($"Feedback sound array configured correctly: Correct sound={truesound[0]?.name ?? "Null"}, Error sound={truesound[1]?.name ?? "Null"}");
        }
    }

    private void InitAudioSources()
    {
        GameObject floorObj = GameObject.Find("/地板");
        if (floorObj == null)
        {
            Debug.LogError("Floor object not found! Check if object name is /地板");
            return;
        }

        AudioSource[] audioSources = floorObj.GetComponents<AudioSource>();
        Debug.Log($"Found {audioSources.Length} AudioSource components on Floor object");

        if (audioSources.Length >= 1)
        {
            runeAudioSource = audioSources[0];
            runeAudioSource.playOnAwake = false;
            Debug.Log("Rune pronunciation audio source initialized");
        }
        else
        {
            Debug.LogError("Floor object needs at least 1 AudioSource component!");
        }

        if (audioSources.Length >= 2)
        {
            feedbackAudioSource = audioSources[1];
            feedbackAudioSource.playOnAwake = false;
            Debug.Log("Feedback sound audio source initialized (using existing component)");
        }
        else
        {
            feedbackAudioSource = floorObj.AddComponent<AudioSource>();
            feedbackAudioSource.playOnAwake = false;
            Debug.Log("Feedback sound audio source initialized (created new component)");
        }
    }

    void Update()
    {
        setTrueSHow();

        GameObject startBtn = GameObject.Find("/开始游戏方块");
        if (!goend)
        {
            if (startBtn != null)
            {
                startBtn.GetComponent<MeshRenderer>().enabled = false;
                Transform displayTransform = startBtn.transform.Find("显示");
                if (displayTransform != null)
                {
                    TextMeshPro displayText = displayTransform.GetComponent<TextMeshPro>();
                    if (displayText != null) displayText.text = "";
                }
            }
            showTime();
        }
        else
        {
            if (startBtn != null)
            {
                startBtn.GetComponent<MeshRenderer>().enabled = true;
                Transform displayTransform = startBtn.transform.Find("显示");
                if (displayTransform != null)
                {
                    TextMeshPro displayText = displayTransform.GetComponent<TextMeshPro>();
                    if (displayText != null) displayText.text = "Start Game";
                }
            }
        }

        if (realTime < 5f && !isFirst)
        {
            realTime += Time.deltaTime;
            finalEndTime += Time.deltaTime;
        }

        if (finalEndTime >= 60) goend = true;

        if (!goend && realTime >= 4f)
        {
            addNewNeed();
            createBlockS();
            realTime = 0f;
        }

        if (goend && realTime >= 5f)
        {
            realTime = 5f;
            showEnd();
        }

        showLinkAndError();
    }

    public void createBlockS()
    {
        if (nowListNumber < allTrueWordUsingSHOW.Count)
        {
            while (createNumber != 4)
            {
                createBlockBYINT(createNumber);
                createNumber++;
            }

            if (createNumber == 4)
            {
                createNumber = 0;

                if (!roundCounted.ContainsKey(nowListNumber))
                {
                    roundCounted.Add(nowListNumber, false);
                    Debug.Log($"Initialized count flag for round {nowListNumber}: Not counted");
                }

                if (nowListNumber >= 0 && nowListNumber < correctRuneIds.Count)
                {
                    int runeId = correctRuneIds[nowListNumber];
                    playsound(runeId);
                }

                nowListNumber++;
            }
        }
    }

    public void createBlockBYINT(int a)
    {
        GameObject go = Instantiate(Newblock, toraw.transform);

        if (a == 0)
        {
            go.name = "方块_" + this.nowListNumber + "_1";
            go.GetComponent<BlockMoveArrow>().setAll(allTrueWordUsingSHOW[this.nowListNumber]);
        }
        else if (a == 1)
        {
            go.name = "方块_" + this.nowListNumber + "_2";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock1[this.nowListNumber]);
        }
        else if (a == 2)
        {
            go.name = "方块_" + this.nowListNumber + "_3";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock2[this.nowListNumber]);
        }
        else if (a == 3)
        {
            go.name = "方块_" + this.nowListNumber + "_4";
            go.GetComponent<BlockMoveArrow>().setAll(falseBlock3[this.nowListNumber]);
        }

        go.GetComponent<MeshRenderer>().enabled = true;
        go.GetComponent<BlockMoveArrow>().enabled = true;
    }

    public void showLinkAndError()
    {
        GameObject comboTextObj = GameObject.Find("/黑色背景测试/得分");
        if (comboTextObj != null)
        {
            TextMeshPro comboText = comboTextObj.GetComponent<TextMeshPro>();
            if (comboText != null) comboText.text = linkNumber + " combo";
        }

        if (havehit != 0)
        {
            GameObject errorTextObj = GameObject.Find("/黑色背景测试/错误次数");
            if (errorTextObj != null)
            {
                TextMeshPro errorText = errorTextObj.GetComponent<TextMeshPro>();
                if (errorText != null)
                {
                    int correctCount = havehit - allErrorShow.Count;
                    float accuracy = (float)correctCount / havehit * 100;
                    errorText.text = $"{correctCount} / {havehit}\n{accuracy:F1}%";
                    Debug.Log($"Current stats: Total attempts={havehit}, Correct={correctCount}, Errors={allErrorShow.Count}, Accuracy={accuracy:F1}%");
                }
            }
        }
    }

    public void choiceOrFalse(int number, bool Tf)
    {
        if (roundCounted.ContainsKey(number) && roundCounted[number])
        {
            Debug.LogWarning($"Round {number} already counted, skipping duplicate count");
            return;
        }

        if (roundCounted.ContainsKey(number))
            roundCounted[number] = true;
        else
            roundCounted.Add(number, true);

        havehit++;
        Debug.Log($"Counted hit for round {number}: Total attempts={havehit}");

        if (Tf)
        {
            linkNumber++;
            Debug.Log($"Round {number} - Correct, playing correct feedback sound");
            PlaySoundhit(true);

            GameObject feedbackTextObj = GameObject.Find("/黑色背景测试/第几个正确");
            if (feedbackTextObj != null)
            {
                TextMeshPro feedbackText = feedbackTextObj.GetComponent<TextMeshPro>();
                if (feedbackText != null) feedbackText.text = $"the {number} is right";
            }
        }
        else
        {
            if (allErrorShow.IndexOf(number) == -1)
            {
                allErrorShow.Add(number);
                linkNumber = 0;
                Debug.Log($"Round {number} - Incorrect, playing error feedback sound");
                PlaySoundhit(false);
            }

            GameObject feedbackTextObj = GameObject.Find("/黑色背景测试/第几个正确");
            if (feedbackTextObj != null)
            {
                TextMeshPro feedbackText = feedbackTextObj.GetComponent<TextMeshPro>();
                if (feedbackText != null) feedbackText.text = $"the {number} is error";
            }
        }
    }

    public void playsound(int a)
    {
        if (gameMode != 4) return;
        if (a < 0 || a >= TOTAL_RUNE_COUNT)
        {
            Debug.LogError($"Invalid rune index: {a}");
            return;
        }
        if (runeAudioSource == null)
        {
            Debug.LogError("Rune pronunciation audio source not initialized");
            return;
        }

        runeAudioSource.Stop();
        if (a < allsoundTone.Length && allsoundTone[a] != null)
        {
            runeAudioSource.PlayOneShot(allsoundTone[a], 1.5f);
            Debug.Log($"Playing rune pronunciation: Index {a} → {runeSymbols[a]}");
        }
        else
        {
            Debug.LogError($"No pronunciation file for rune index {a}");
        }
    }

    public void PlaySoundhit(bool isCorrect)
    {
        if (feedbackAudioSource == null)
        {
            Debug.LogError("Feedback sound audio source not initialized!");
            return;
        }

        if (truesound == null)
        {
            Debug.LogError("Feedback sound array truesound not assigned!");
            return;
        }

        feedbackAudioSource.Stop();
        feedbackAudioSource.volume = 1f;

        if (isCorrect)
        {
            if (truesound.Length > 0 && truesound[0] != null)
            {
                feedbackAudioSource.PlayOneShot(truesound[0]);
                Debug.Log($"Successfully played correct feedback sound: {truesound[0].name}");
            }
            else
            {
                Debug.LogError("Correct feedback sound truesound[0] not assigned!");
            }
        }
        else
        {
            if (truesound.Length > 1 && truesound[1] != null)
            {
                feedbackAudioSource.PlayOneShot(truesound[1]);
                Debug.Log($"Successfully played error feedback sound: {truesound[1].name}");
            }
            else
            {
                Debug.LogError("Error feedback sound truesound[1] not assigned!");
            }
        }
    }

    public void firstPlay()
    {
        AudioSource bgmSource = transform.GetComponent<AudioSource>();
        if (bgmSource != null && !bgmSource.enabled)
        {
            bgmSource.enabled = true;
            bgmSource.Play();
            Debug.Log("Playing background music");
        }
    }

    public void showEnd()
    {
        AudioSource bgmSource = transform.GetComponent<AudioSource>();
        if (bgmSource != null) bgmSource.enabled = false;

        string endText = endString();
        GameObject endTextObj = GameObject.Find("/黑色背景测试/第几个正确");
        if (endTextObj != null)
        {
            TextMeshPro endTextUI = endTextObj.GetComponent<TextMeshPro>();
            if (endTextUI != null) endTextUI.text = endText;
        }
        Debug.Log("[Game Over] Showing result screen");
    }

    public void showTime()
    {
        int remainingTime = (int)(60 - finalEndTime);
        GameObject timeTextObj = GameObject.Find("/黑色背景测试/第几个正确");
        if (timeTextObj != null)
        {
            TextMeshPro timeText = timeTextObj.GetComponent<TextMeshPro>();
            if (timeText != null) timeText.text = $"{remainingTime}s";
        }
    }

    public string endString()
    {
        string final = "";
        if (allErrorShow.Count == 0)
        {
            final = "Perfect! No mistakes";
        }
        else
        {
            final = "Incorrect Runes:\n";
            int errorCount = 0;

            foreach (int errorIndex in allErrorShow)
            {
                errorCount++;
                if (errorIndex >= 0 && errorIndex < allTrueWordUsingSHOW.Count)
                {
                    final += $"{errorCount}: {allTrueWordUsingSHOW[errorIndex].getShowString()}   ";
                    if (errorCount % 2 == 0) final += "\n";
                }
            }

            int totalAttempts = havehit;
            int correctAttempts = totalAttempts - allErrorShow.Count;
            float accuracy = totalAttempts > 0 ? (float)correctAttempts / totalAttempts * 100 : 0;
            final += $"\n\nAccuracy: {accuracy:F1}%\nTotal Attempts: {totalAttempts}";
        }
        return final;
    }

    public void restart()
    {
        if (goend)
        {
            ralUseWord = new List<word>();
            goend = false;
            allErrorShow.Clear();
            linkNumber = 0;
            gameMode = 4;

            allTrueWordUsingSHOW = new List<block>();
            falseBlock1 = new List<block>();
            falseBlock2 = new List<block>();
            falseBlock3 = new List<block>();
            correctRuneIds.Clear();
            roundCounted.Clear();

            finalEndTime = 0;
            realTime = 0f;
            nowListNumber = 0;
            havehit = 0;
            createNumber = 0;

            GameObject errorTextObj = GameObject.Find("/黑色背景测试/错误次数");
            if (errorTextObj != null)
            {
                TextMeshPro errorText = errorTextObj.GetComponent<TextMeshPro>();
                if (errorText != null) errorText.text = "";
            }

            GameObject feedbackTextObj = GameObject.Find("/黑色背景测试/第几个正确");
            if (feedbackTextObj != null)
            {
                TextMeshPro feedbackText = feedbackTextObj.GetComponent<TextMeshPro>();
                if (feedbackText != null) feedbackText.text = "";
            }

            isFirst = false;
        }
    }

    public void setTrueSHow()
    {
        if (gameMode == 4)
        {
            GameObject displayTextObj = GameObject.Find("/黑色背景测试/第几个正确");
            if (displayTextObj != null && goend)
            {
                TextMeshPro displayText = displayTextObj.GetComponent<TextMeshPro>();
                if (displayText != null) displayText.text = "";
            }
            return;
        }

        string useAllString = "";
        for (int i = havehit; i < nowListNumber; i++)
        {
            if (gameMode == 1 && i < ralUseWord.Count) useAllString += ralUseWord[i].getRune() + "\n";
            if (gameMode == 2 && i < ralUseWord.Count) useAllString += ralUseWord[i].getEnglish() + "\n";
            if (gameMode == 3 && i < ralUseWord.Count) useAllString += ralUseWord[i].getRune() + "\n";
        }

        GameObject modeTextObj = GameObject.Find("/黑色背景测试/第几个正确");
        if (modeTextObj != null)
        {
            TextMeshPro modeText = modeTextObj.GetComponent<TextMeshPro>();
            if (modeText != null) modeText.text = useAllString;
        }
    }

    public void backToMenu()
    {
        Debug.Log("[Return to Menu] Loading StartMenu scene");
        SceneManager.LoadScene("StartMenu");
    }

    public void addNewNeed()
    {
        if (gameMode == 4)
        {
            List<int> usedRuneIndices = new List<int>();
            List<int> usedPositions = new List<int>();

            int targetRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
            usedRuneIndices.Add(targetRuneIndex);
            correctRuneIds.Add(targetRuneIndex);

            int targetPosition = Random.Range(0, 10);
            usedPositions.Add(targetPosition);

            allTrueWordUsingSHOW.Add(new block(
                runeSymbols[targetRuneIndex],
                true,
                0,
                targetPosition,
                allTrueWordUsingSHOW.Count
            ));

            while (usedRuneIndices.Count < 4)
            {
                int randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                while (usedRuneIndices.Contains(randomRuneIndex))
                {
                    randomRuneIndex = Random.Range(0, TOTAL_RUNE_COUNT);
                }
                usedRuneIndices.Add(randomRuneIndex);

                int randomPosition = Random.Range(0, 10);
                while (usedPositions.Contains(randomPosition))
                {
                    randomPosition = Random.Range(0, 10);
                }
                usedPositions.Add(randomPosition);

                string distractorRune = runeSymbols[randomRuneIndex];
                int blockCount = usedRuneIndices.Count;

                if (blockCount == 2)
                {
                    falseBlock1.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
                else if (blockCount == 3)
                {
                    falseBlock2.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
                else if (blockCount == 4)
                {
                    falseBlock3.Add(new block(distractorRune, false, 0, randomPosition, allTrueWordUsingSHOW.Count - 1));
                }
            }
        }
    }
}