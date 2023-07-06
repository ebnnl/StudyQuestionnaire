using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using TMPro;


public class QuestionnaireManager : MonoBehaviour
{
    // Every question in one page like paper version?

    [SerializeField]
    private GameObject VRQuestionsPage;

    [SerializeField]
    private GameObject realQuestionsPage;

    [SerializeField]
    private Question[] VRQuestions;

    [SerializeField]
    private Question[] realQuestions;

    [SerializeField]
    private int[] answers;

    [SerializeField]
    private Button nextButton;
    [SerializeField]
    private Button previousButton;
    [SerializeField]
    private Button confirmButton;

    private int currentSource = REAL;
    private int progress = 0;

    private int currentQuestion = 0;

    public const int REAL = 0;
    public const int VIRTUAL = 1;
    public const int NEW = 2;

    private bool training = false;

    //################# DATA ########################
    [Header("Participant Data")]
    private int participantID = 0;

    [SerializeField]
    private TextAsset sets;
    [SerializeField]
    private TextAsset sources;
    [SerializeField]
    private TextAsset sequence;
    [SerializeField]
    private TextAsset realGameSequenceCsv;
    [SerializeField]
    private TextAsset virtualGameSequenceCsv;

    private TextWriter outputData;

    private int[] setSource = new int[3]; //setSource[i] is the source of set i for current participant
    private int[] objectSet = new int[30]; //objectSet[i] is the set of object i (same for every participant)
    private int[] objectSource = new int[30]; //objectSource[i] is the source of object i for current participant

    private int[] objectSequence = new int[20]; //sequence of objects that current participant will see
    private int[] sourceSequence = new int[20]; //sequence of sources that participant will be exposed to
    private int[] gamesSequence = new int[20]; //sequence of games the participant will play


    [SerializeField]
    private Button startTrainingButton;
    [SerializeField]
    private Button startStudyButton;

    [SerializeField]
    private TMP_InputField participantInput;

    [SerializeField]
    private GameObject participantGO;

    [SerializeField]
    private GameObject putHeadsetText;
    [SerializeField]
    private GameObject goBackCrossText;
    [SerializeField]
    private Button nextQuestionnaireButton;

    // Start is called before the first frame update
    void Start()
    {
        progress = 0;

        answers = new int[VRQuestions.Length];

        putHeadsetText.SetActive(false);
        goBackCrossText.SetActive(false);
        nextQuestionnaireButton.gameObject.SetActive(false);

        nextButton.onClick.AddListener(NextQuestion);
        previousButton.onClick.AddListener(PreviousQuestion);
        confirmButton.onClick.AddListener(Confirm);

        startTrainingButton.onClick.AddListener(StartTrainingQuestionnaire);
        startStudyButton.onClick.AddListener(StartQuestionnaire);
        nextQuestionnaireButton.onClick.AddListener(NextQuestionnaire);
    }

    private void StartTrainingQuestionnaire()
    {
        training = true;
        currentSource = VIRTUAL;
        int.TryParse(participantInput.text, out participantID);
        participantGO.SetActive(false);
        InitialiseOutput();
        LoadData();
        Initialise();
    }

    private void StartQuestionnaire()
    {
        training = false;
        /*int.TryParse(participantInput.text, out participantID);
        participantGO.SetActive(false);
        InitialiseOutput();
        LoadData();*/
        startStudyButton.gameObject.SetActive(false);
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        // Disable next if no answer selected
        if (currentSource == REAL)
        {
            if (realQuestions[currentQuestion].AnswerSelected())
            {
                nextButton.interactable = true;
                confirmButton.interactable = true;
            }
            else
            {
                nextButton.interactable = false;
                confirmButton.interactable = false;
            }
        }
        else
        {
            if (VRQuestions[currentQuestion].AnswerSelected())
            {
                nextButton.interactable = true;
                confirmButton.interactable = true;
            }
            else
            {
                nextButton.interactable = false;
                confirmButton.interactable = false;
            }
        }
       
    }

    public void UpdateAnswers(){
        if (!training)
        {
            if (currentSource == REAL)
            {
                for (int i = 0; i < realQuestions.Length; i++)
                {
                    answers[i] = realQuestions[i].GetAnswer();
                }
            }
            else
            {
                for (int i = 0; i < VRQuestions.Length; i++)
                {
                    answers[i] = VRQuestions[i].GetAnswer();
                }
            }
        }
        
    }

    void Initialise(){
        nextButton.gameObject.SetActive(true);
        previousButton.gameObject.SetActive(true);

        currentQuestion = 0;
        if (!training) { currentSource = sourceSequence[progress]; }

        if (currentSource == REAL)
        {
            realQuestionsPage.SetActive(true);
            VRQuestionsPage.SetActive(false);
        }
        else
        {
            VRQuestionsPage.SetActive(true);
            realQuestionsPage.SetActive(false);
        }

        // Reset and display first question
        for (int i=0; i<VRQuestions.Length; i++){
            VRQuestions[i].Reset();
            VRQuestions[i].Hide();
            answers[i] = 0;
        }
        for (int i = 0; i < realQuestions.Length; i++)
        {
            realQuestions[i].Reset();
            realQuestions[i].Hide();
            answers[i] = 0;
        }

        if (currentSource == REAL)
        {
            realQuestions[0].Show();
        }
        else
        {
            VRQuestions[0].Show();
        }
        

        // Disable previous button
        previousButton.interactable = false;
        // Hide confirm button
        confirmButton.gameObject.SetActive(false);
        // Show next button
        nextButton.gameObject.SetActive(true);
    }

    void LoadData()
    {
        // Read csv file with the source of each set for participant p, and store it setSource list 
        string sourcesData = sources.text;
        string[] sourcesLines = sourcesData.Split("\n"[0]);
        string[] lineData = (sourcesLines[participantID].Trim()).Split(";"[0]);
        for (int set = 0; set < 3; set++)
        {
            string value = lineData[set];
            int source;
            int.TryParse(value, out source);
            setSource[set] = source;

        }

        // Read csv file with the list of object for each set and store data in objectSet and objectSource lists
        string setsData = sets.text;
        string[] setsLines = setsData.Split("\n"[0]);
        for (int set = 0; set < 3; set++)
        {
            lineData = (setsLines[set].Trim()).Split(";"[0]);
            foreach (string value in lineData)
            {
                int objectId;
                int.TryParse(value, out objectId);
                objectSet[objectId] = set;
                objectSource[objectId] = setSource[set];
            }
        }

        string sequenceData = sequence.text;
        string[] sequenceLines = sequenceData.Split("\n"[0]);
        string outputLine;
        lineData = (sequenceLines[participantID].Trim()).Split(";"[0]);
        int i = 0;
        foreach (string value in lineData)
        {
            int objectId;
            int.TryParse(value, out objectId);
            // Add object to ordered list, with its source (if source is not new)
            Debug.Log(objectId);
            int sourceId = objectSource[objectId];
            if (sourceId != NEW)
            {
                objectSequence[i] = objectId;
                sourceSequence[i] = sourceId;
                i++;
            }
            else
            {
                outputLine = "x;" + objectId.ToString() + ";x;NEW;_;_";
                /*for (int j=0; j<nbQuestions; j++){
                    outputLine+=";x";
                }*/
                if (!training)
                {
                    outputData.WriteLine(outputLine);
                }

            }
        }
        outputData.Flush();

        int[] realGameSequence = new int[10]; //sequence of the real games for current participant
        int[] virtualGameSequence = new int[10]; //sequence of virtual games for current participant

        string realGameSequenceData = realGameSequenceCsv.text;
        string[] realGameSequenceLines = realGameSequenceData.Split("\n"[0]);
        lineData = (realGameSequenceLines[participantID].Trim()).Split(";"[0]);
        i = 0;
        foreach (string value in lineData)
        {
            int gameId;
            int.TryParse(value, out gameId);
            realGameSequence[i] = gameId;
            i++;
        }

        string virtualGameSequenceData = virtualGameSequenceCsv.text;
        string[] virtualGameSequenceLines = virtualGameSequenceData.Split("\n"[0]);
        lineData = (virtualGameSequenceLines[participantID].Trim()).Split(";"[0]);
        i = 0;
        foreach (string value in lineData)
        {
            int gameId;
            int.TryParse(value, out gameId);
            virtualGameSequence[i] = gameId;
            i++;
        }

        int r = 0;
        int v = 0;
        for (int j = 0; j < 20; j++)
        {
            if (sourceSequence[j] == REAL)
            {
                gamesSequence[j] = realGameSequence[r];
                r++;
            }
            else if (sourceSequence[j] == VIRTUAL)
            {
                gamesSequence[j] = virtualGameSequence[v];
                v++;
            }
        }
    }

    void InitialiseOutput()
    {
        // Create new output data file
        outputData = new StreamWriter("data_participant" + participantID.ToString() + ".csv", false);
        
        // First line: participant ID

        // Second line: labels
        // Item number, object id, game ID, source, source monitoring result, type of error, q1...
        string line2 = "Item number;Object ID;Source;Source monitoring result;Type of error";
        for(int i = 0; i<VRQuestions.Length; i++){
        line2+=";Question "+(i+1).ToString();
        }

        outputData.WriteLine("Participant ID;" + participantID.ToString());
        outputData.WriteLine(line2);
        
        // Lines 3 to 11: list of new object (no data collected for questionnaire)
        // Done in LoadData
        outputData.Flush();
    }

    private void SaveAnswers()
    {
        if (!training)
        {
            string line = "";
            line += progress.ToString();
            line += ";" + objectSequence[progress].ToString();
            if (currentSource == REAL)
            {
                line += ";REAL";
            }
            else
            {
                line += ";VIRTUAL";
            }

            line += ";_;_";

            for (int i = 0; i < realQuestions.Length; i++)
            {
                line += ";" + answers[i].ToString();
            }

            outputData.WriteLine(line);

            outputData.Flush();
        }
    }

    void NextQuestion(){
        if (currentSource == REAL)
        {
            // Hide previous question
            realQuestions[currentQuestion].Hide();
            currentQuestion++;
            // Show new question
            realQuestions[currentQuestion].Show();
        }
        else
        {
            // Hide previous question
            VRQuestions[currentQuestion].Hide();
            currentQuestion++;
            // Show new question
            VRQuestions[currentQuestion].Show();
        }
        
        if(currentQuestion+1>=VRQuestions.Length){
            // Hide next
            nextButton.gameObject.SetActive(false);
            // Show confirm button
            confirmButton.gameObject.SetActive(true);
        }
        if(currentQuestion>0){
            // Enable previous
            previousButton.interactable = true;
        }
    }

    void PreviousQuestion(){
        if(currentQuestion>0){
            if (currentSource == REAL)
            {
                // Hide current question
                realQuestions[currentQuestion].Hide();
                currentQuestion--;
                // Show current question
                realQuestions[currentQuestion].Show();
            }
            else
            {
                // Hide current question
                VRQuestions[currentQuestion].Hide();
                currentQuestion--;
                // Show current question
                VRQuestions[currentQuestion].Show();
            }
        }
        if (currentQuestion==0){
            // Disable previous
            previousButton.interactable = false;
        }
        if(currentQuestion+1<VRQuestions.Length){
            // Hide confirm
            confirmButton.gameObject.SetActive(false);
            // Show next button
            nextButton.gameObject.SetActive(true);
        }
    }

    void Confirm(){
       
        //Save answers
        SaveAnswers();

        //Hide questionnaires
        VRQuestionsPage.SetActive(false);
        realQuestionsPage.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        previousButton.gameObject.SetActive(false);

        if (training)
        {
            if (currentSource == REAL) { currentSource = VIRTUAL; }
            else { currentSource = REAL; }
        }
        else
        {
            // Progress
            progress++;
            currentSource = sourceSequence[progress];
        }


        // Display what to do
        nextQuestionnaireButton.gameObject.SetActive(true);
        if (currentSource == REAL)
        {
            goBackCrossText.SetActive(true);
        }
        else
        {
            putHeadsetText.SetActive(true);
        }

    }

    private void NextQuestionnaire()
    {
        
        putHeadsetText.SetActive(false);
        goBackCrossText.SetActive(false);
        nextQuestionnaireButton.gameObject.SetActive(false);
        // Reset questionnaire
        Initialise();
    }



}
