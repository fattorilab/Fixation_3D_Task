using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using PupilLabs;




public class MainTask : MonoBehaviour
{
    #region Variables Declaration

    #region GameObjects and components

    [SerializeField]

    [HideInInspector]
    [Header("GameObjects and components")]

    // Cams
    [System.NonSerialized] Camera camM;
    [System.NonSerialized] Camera camL;
    [System.NonSerialized] Camera camR;

    // Pupil
    [System.NonSerialized] public PupilDataStream PupilDataStreamScript;
    private RequestController RequestControllerScript;
    private bool PupilDataConnessionStatus;

    // Game
    Rigidbody player_rb;
    [HideInInspector] GameObject environment;
    [HideInInspector] GameObject experiment;
    [HideInInspector] GameObject player;

    #endregion

    #region Saving info

    [Header("Saving info")]
    public string MEF;
    public string path_to_data = "C:/Users/stefa/Documents/LABORATORIO SIMULATO/Registrazioni_VR/";
    [HideInInspector] public long starttime = -10000000;
    private string identifier;
    [HideInInspector] public int seed = 12345;
    [HideInInspector] public int frame_number = 0;

    #endregion

    #region Reward info

    [Header("Reward")]
    public int RewardLength = 50;
    private float RewardLength_in_sec;
    public int reward_counter = 0;

    #endregion

    #region Trials Info

    [Header("Trials Info")]

    // Trials
    public int trials_win;
    public int trials_lose;
    [System.NonSerialized] public int current_trial;
    public int[] trials_for_target;
    public int trials_for_cond;

    // States
    public int current_state;
    [System.NonSerialized] public int last_state;
    [System.NonSerialized] public string error_state;

    // Conditions
    private int randomIndex;
    public List<int> condition_list;
    [System.NonSerialized] public int current_condition;

    // Tracking events
    private float lastevent;
    private bool first_frame;

    // Moving timer
    private static bool isMoving = false;

    #endregion

    #region Target Info

    [Header("Target Info")]
    public string file_name_positions;
    [System.NonSerialized] public GameObject TargetPrefab; GameObject Target;
    [System.NonSerialized] public Vector3 TargetCurrentPosition;
    public Vector3 TargetSize;

    public List<Vector3> target_positions = new List<Vector3>();

    #endregion

    #region Epochs Info

    [Header("Epoches Info")]

    // Array, because is not changing size during the runtime
    public float[] FREE_timing = { 0.3f, 0.6f, 0.9f }; 
    public float[] DELAY_timing = { 0.3f, 0.6f, 0.9f };
    public float[] RT_timing = { 0.3f, 0.6f, 0.9f };

    private List<int> FREE_timing_list;
    private List<int> DELAY_timing_list;
    private List<int> RT_timing_list;
   
    public float PRETRIAL_duration = 0.5f;
    public float INTERTRIAL_duration = 0.5f;
    private float FREE_duration;
    private float DELAY_duration;
    private float RT_duration;

    #endregion

    #region Arduino Info

    [Header("Arduino Info")]
    [System.NonSerialized] public Ardu ardu;
    [System.NonSerialized] public float arduX;
    [System.NonSerialized] public float arduY;

    #endregion

    #region PupilLab Info

    [Header("PupilLab Info")]
    [System.NonSerialized] public Vector2 centerRightPupilPx = new Vector2(float.NaN, float.NaN);
    [System.NonSerialized] public Vector2 centerLeftPupilPx = new Vector2(float.NaN, float.NaN);
    [System.NonSerialized] public float diameterRight = float.NaN;
    [System.NonSerialized] public float diameterLeft = float.NaN;

    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {

        // Generate random seed
        System.Random rand = new System.Random();
        seed = rand.Next();

        // Setup
        UnityEngine.Random.InitState(seed);
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        first_frame = true;

        // States
        current_state = -2;
        last_state = -2;
        error_state = "";

        // Trials
        current_trial = 0;
        trials_win = 0;
        trials_lose = 0;

        // GameObjects
        ardu = GetComponent<Ardu>();
        player = GameObject.Find("Player");
        player_rb = player.GetComponent<Rigidbody>();
        experiment = GameObject.Find("Experiment");
        environment = GameObject.Find("Environment");

        // PupilLab
        PupilDataStreamScript = GameObject.Find("PupilDataManagment").GetComponent<PupilDataStream>();
        RequestControllerScript = GameObject.Find("PupilDataManagment").GetComponent<RequestController>();

        // Init cameras
        camM = GameObject.Find("Main Camera").GetComponent<Camera>();
        camL = GameObject.Find("Left Camera").GetComponent<Camera>();
        camR = GameObject.Find("Right Camera").GetComponent<Camera>();

        current_condition = -1;

        // Target Prefab
        TargetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");

        // Import target_positions from CSV file
        LoadPositionsFromCSV();

        // Define number of trials per each target
        trials_for_target = new int[target_positions.Count];

        // Generating condition and timing vectors
        condition_list = CreateRandomSequence(target_positions.Count, trials_for_cond * target_positions.Count);
        FREE_timing_list = CreateRandomSequence(FREE_timing.Length, trials_for_cond * target_positions.Count);
        DELAY_timing_list = CreateRandomSequence(DELAY_timing.Length, trials_for_cond * target_positions.Count);
        RT_timing_list = CreateRandomSequence(RT_timing.Length, trials_for_cond * target_positions.Count);
    }

    void Update()
    {
        // Increase number of frames
        frame_number++;

        RewardLength_in_sec = RewardLength / 1000f;

        // Get coordinates from Ardu
        arduX = ardu.ax1; 
        arduY = ardu.ax2;

        // Check if the player is moving the joystick
        if ((!float.IsNaN(arduX) && arduX != 0) || (!float.IsNaN(arduY) && arduY != 0) || Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0) //if arduX is nan, I cannot compare it with 0
        {

            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        // Start on first operating frame
        if (first_frame)
        {
            Debug.Log("START TASK");
            // Start time main task unity
            starttime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            // Send START trigger
            ardu.SendStartRecordingOE();

            first_frame = false;
        }

        // Manual reward
        if (Input.GetKeyDown("space")) { ardu.SendReward(RewardLength); }
        reward_counter = ardu.reward_counter;

        #region StateMachine

        switch (current_state)
        {   
            
            case -2: // TASK BEGINS

                if (PupilDataStreamScript.subsCtrl.IsConnected || RequestControllerScript.ans)
                {   
                    foreach (Camera cam in player.GetComponentsInChildren<Camera>())
                    {
                        cam.backgroundColor = Color.black;
                    }

                    current_state = -1;
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case -1: // INTERTRIAL

                #region State Beginning (executed once upon entering)

                if (last_state != current_state)
                {
                    // Check if all conditions are done and end the session
                    if (condition_list.Count == 0) { QuitGame(); }

                    lastevent = Time.time;
                    last_state = current_state;
                    error_state = "";
                    current_condition = -1;

                    foreach (Camera cam in player.GetComponentsInChildren<Camera>())
                    {
                        cam.backgroundColor = Color.black;
                    }

                }

                #endregion

                #region State Body (executed every frame while in state)

                current_condition = -1;

                #endregion

                #region State End (executed once upon exiting)    

                if ((Time.time - lastevent) >= PRETRIAL_duration && !isMoving)
                {                 
                    current_state = 0;
                }
                #endregion

                break;


            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 0: // BASELINE

                #region State Beginning (executed once upon entering)

                if (last_state != current_state)
                {
                    lastevent = Time.time;
                    last_state = current_state;


                    // Prepare everything for the trial

                    // Choose and instantiate the target
                    current_condition = condition_list[0];
                    TargetCurrentPosition = target_positions[current_condition];
                    Target = Instantiate(TargetPrefab, TargetCurrentPosition, TargetPrefab.transform.rotation);
                    Target.transform.localScale = TargetSize;
                    Target.GetComponent<MeshRenderer>().enabled = false;

                    // Picking first time from the timing list to select epoch durations in this trial
                    FREE_duration = FREE_timing[FREE_timing_list[0]];
                    DELAY_duration = DELAY_timing[DELAY_timing_list[0]];
                    RT_duration = RT_timing[RT_timing_list[0]];

                    //the trial is starting
                    current_trial++;
                }
                #endregion

                #region State Body (executed every frame while in state)

                if (isMoving)
                {
                    error_state = "ERR: Moving in state 0";
                    current_state = -99;
                }
                #endregion

                #region State End (executed once upon exiting)
                if (((Time.time - lastevent) >= INTERTRIAL_duration) && !isMoving)  
                {   

                    current_state = 1;
                }
                #endregion

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            case 1: // FREE

                #region State Beginning
                if (last_state != current_state)
                {
                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                }
                #endregion

                #region State Body
                if (isMoving)
                {
                    error_state = "ERR: Moving in FREE";
                    current_state = -99;
                }
                #endregion

                #region State End
                if ((Time.time - lastevent) >= FREE_duration && !isMoving)
                {
                    current_state = 2;
                }
                #endregion

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 2: // DELAY

                #region State Beginning
                if (last_state != current_state) //StateBeginning
                {
                    // Switch ON target and set color
                    Target.GetComponent<MeshRenderer>().enabled = true;
                    Target.GetComponent<MeshRenderer>().material.color = Color.green;

                    // Add target to data to be saved
                    identifier = "Target" + current_condition.ToString();

                    GetComponent<Saver>().addObject(identifier,
                        "Target",
                        Target.transform.position.x,
                        Target.transform.position.y,
                        Target.transform.position.z,
                        TargetPrefab.transform.rotation[0],
                        TargetPrefab.transform.rotation[1],
                        TargetPrefab.transform.rotation[2],
                        TargetSize[0],
                        TargetSize[1],
                        TargetSize[2]);

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                }
                #endregion

                #region State Body
                if (isMoving)
                {
                    error_state = "ERR: Moving in DELAY";
                    current_state = -99;
                }
                #endregion

                #region State End
                if ((Time.time - lastevent) >= DELAY_duration && !isMoving)
                {
                    current_state = 3;
                }
                #endregion

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 3: // RT

                #region State Beginning

                if (last_state != current_state) 
                {
                    // Switch target color
                    Target.GetComponent<MeshRenderer>().material.color = Color.red;

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                }
                #endregion

                #region State Body

                if (isMoving) 
                {
                    current_state = 99;
                }
                #endregion

                #region State End

                if ((Time.time - lastevent) >= RT_duration && !isMoving)   
                {
                    error_state = "ERR: Not Moving in RT";
                    current_state = -99;
                }
                #endregion

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case -99: // ERROR

                #region State Beginning

                if (last_state != current_state) 
                {
                    Debug.Log(error_state);
                    GetComponent<Saver>().addObjectEnd(identifier);
                    reset_lose();

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                }
                #endregion

                #region State Body

                #endregion

                #region State End
                if (true)
                {
                    current_state = -1;
                }
                #endregion

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 99: // WIN

                #region State Beginning

                if (last_state != current_state)
                {
                    Debug.Log("TRIAL DONE");
                    GetComponent<Saver>().addObjectEnd(identifier);
                    reset_win();

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;

                }

                #endregion

                #region State Body

                #endregion

                #region State End
                if ((Time.time - lastevent) >= RewardLength_in_sec)
                {
                    current_state = -1;
                }
                #endregion

                break;


        }

        #endregion
    }

    #region Methods

    #region Quit

    void OnApplicationQuit()
    {
        ardu.SendStopRecordingOE();
        Debug.Log("END TASK");
        QuitGame();
    }

    public void QuitGame()
    {
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Reset

    void reset_win()
    {
        ardu.SendReward(RewardLength);
        Destroy(Target);

        trials_win++;
        trials_for_target[current_condition]++;

        condition_list.RemoveAt(0);
        FREE_timing_list.RemoveAt(0);
        DELAY_timing_list.RemoveAt(0);
        RT_timing_list.RemoveAt(0);
    }

    void reset_lose()
    {
        Destroy(Target);

        trials_lose++;

        condition_list = SwapVector(condition_list);
        FREE_timing_list = SwapVector(FREE_timing_list); //not strictly necessary, but better for coherence...
        DELAY_timing_list = SwapVector(DELAY_timing_list);
        RT_timing_list = SwapVector(RT_timing_list);

    }

    #endregion

    #region Conditions

    public List<int> CreateRandomSequence(int n, int k) //n, number of elements; k, length of the required vector
    {
        var vector = new List<int>();
        System.Random rnd = new System.Random(); // Create a new Random instance

        for (int i = 0; i < Math.Floor((double)k / n) + 1; i++)
        {
            var tmp = Enumerable.Range(0, n).OrderBy(x => rnd.Next(n)).ToList();
            vector.AddRange(tmp);
        }

        // If k is not a multiple of n, we need to remove the extra elements
        if (vector.Count > k)
        {
            vector = vector.Take(k).ToList();
        }

        return vector;
    }

    public List<int> SwapVector(List<int> vector)
    {
        int i = vector.Count / UnityEngine.Random.Range(2, 5); //moves the first half to fifth of the vector to the end of the vector  
        if (i > 0)
        {
            vector = vector.Skip(i).Concat(vector.Take(i)).ToList();
        }
        return vector;
    }

    void set_epochs_duration()
    {
        int randomIndex_FREE = UnityEngine.Random.Range(0, FREE_timing.Length);
        int randomIndex_DELAY = UnityEngine.Random.Range(0, DELAY_timing.Length);
        int randomIndex_RT = UnityEngine.Random.Range(0, RT_timing.Length);

        FREE_duration = FREE_timing[randomIndex_FREE];
        DELAY_duration = DELAY_timing[randomIndex_DELAY];
        RT_duration = RT_timing[randomIndex_RT];
    }

    #endregion

    #region Targets

    private void LoadPositionsFromCSV()
    {
        string filePath = Application.dataPath + "/" + file_name_positions + ".csv";
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = reader.ReadLine(); // Salta la riga degli header se presente
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    string[] fields = line.Split(',');
                    if (fields.Length >= 3)
                    {
                        float x, y, z;
                        if (float.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                            float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                            float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                        {
                            Vector3 position = new Vector3(x, y, z);
                            target_positions.Add(position);
                        }
                        else
                        {
                            Debug.LogWarning("Impossibile convertire le coordinate in numeri: " + line);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("La riga non ha abbastanza coordinate: " + fields.Length);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Il file non esiste: " + filePath);
        }
    }

    #endregion

    #endregion

}