using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;
using PupilLabs;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;



public class MainTask : MonoBehaviour
{
    #region Variables Declaration

    public float pippo;

    [SerializeField]

    [HideInInspector]
    // Variable to hook this main_script onto the script 'PupilDataScript' that takes pupil data and the variable to understand if Pupil Lab is connected or not
    [Header("Collegamenti")] 
    public PupilDataStream PupilDataStreamScript;
    private RequestController RequestControllerScript;
    private bool PupilDataConnessionStatus;
    [HideInInspector] public GameObject Player;

    [Header("Saving info")]
    public string MEF;
    public string path_to_data = "C:/Users/stefa/Documents/LABORATORIO SIMULATO/Registrazioni_VR/";
    [HideInInspector] public long starttime = 0;

    [Header("Reward")]
    public int RewardLength = 50;
    public int reward_counter = 0; //just for having this information readibily accessible
    float RewardLength_in_sec; //Only for format reasons

    [Header("Trials Info")]
    public string file_name_positions; // stringa con il nome del file csv che deve essere in \Assets
    public int trials_for_cond;  // numero di trial per condition
    public int current_state;   // stato corrente della tasc
    private int last_state;     // ultimo stato prima del corrente
    [HideInInspector] public string error_state;    
    public int current_trial;  //numero trial attuale
    public int trials_win;    //totale trial vinti
    public int trials_lose;   //totale trial persi
    public int[] trials_for_target;  // lista che conta i trial giusti fatti per target

    // variables for managing the ball GameObject, taken each time from a prefab
    [Header("Target Info")]
    [HideInInspector] public int seed = 12345;      //rabdomizza                                                                                    // che roba è??????????
    public GameObject TargetPrefab; GameObject Target; // put here the prefab
    public Vector3 TargetCurrentPosition; // vettore in cui cadrà le coordinate del target scelto in maniera randomica 
    [HideInInspector] public int current_condition;     // target attuale                                                                GIUSTO FRA_EDO?
    public Vector3 TargetSize; // target dimensions

    public List<Vector3> target_positions = new List<Vector3>(); //defining a list because is chaning size during the runtime
    // [HideInInspector] public List<int> target_label;  // label del target associato al randomIndex;                                            QUESTO è STATO ELIMINATO? GIUSTO FRA_EDO?
    private int randomIndex;
    public List<int> condition_list;

    // variabili per la gestione  delle tempistiche delle epoche
    [Header("Epoches Info")]

    public float[] FREE_timing = { 0.3f, 0.6f, 0.9f }; //defining an array because is not chaning size during the runtime
    public float[] DELAY_timing = { 0.3f, 0.6f, 0.9f };
    public float[] RT_timing = { 0.3f, 0.6f, 0.9f };
    // liste con le squenze randomiche scelte in sequenza                                                                                      
    private List<int> FREE_timing_list;
    private List<int> DELAY_timing_list;
    private List<int> RT_timing_list;
    // qui ci andara a finire la durata scelta per quel trial presa dalle liste sopra                                                            
    private float FREE_duration;
    private float DELAY_duration;
    private float RT_duration;
    

    [Header("Arduino Info")]
    public Ardu ardu;
    public float arduX;
    public float arduY;
    //public int dead_zone;
      
    [Header("PupilLab Info")]
    public Vector2 centerRightPupilPx = new Vector2(float.NaN, float.NaN);  //pixel pupilla destra
    public Vector2 centerLeftPupilPx = new Vector2(float.NaN, float.NaN);   //pixel pupilla sinistra
    public float diameterRight = float.NaN;                  //pupil dianmeter destra
    public float diameterLeft = float.NaN;                   //pupil dianmeter sinistro

    //public bool pupilconnection;

    private float lastevent;                             // questa variabile mi serve per capire quanto passa tra un evento e l'altro                
    private string identifier;
    private bool isMoving = false;
    public bool first_frame;                         // questa variabile mi serve per capire se ho appena lanciato il gioco o no, cosi prendo il last event appena si attiva la connessione
    [HideInInspector] public int frame_number = 0;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(seed);

        current_state = 0;
        last_state = -1;
        error_state = "";
        current_trial = 0;
        trials_win = 0;
        trials_lose = 0;
        RewardLength_in_sec = RewardLength / 1000f;
        first_frame = true;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


        ardu = GetComponent<Ardu>(); 


        //  serve ancora??
        Player = GameObject.Find("Player");

        

        
        PupilDataStreamScript = GameObject.Find("PupilDataManagment").GetComponent<PupilDataStream>();        // mi collego allo script PupilDAtaManagment da cui prendo lo stream dei dati
        RequestControllerScript = GameObject.Find("PupilDataManagment").GetComponent<RequestController>();    // mi collego allo script PupilDAtaManagment da cui prendo la richiesta di controllo per unity
     
        

       
        LoadPositionsFromCSV();     //import target_positions from csv file

  
        trials_for_target = new int[target_positions.Count];

        /*
        for (int i = 0; i < target_positions.Count; i++)
        {
            target_label.Add(i); //create condition vector (targets)
        }
        */


        // Generating condition and timing vectors
        condition_list = CreateRandomSequence(target_positions.Count, trials_for_cond * target_positions.Count);
        FREE_timing_list = CreateRandomSequence(FREE_timing.Length, trials_for_cond * target_positions.Count);
        DELAY_timing_list = CreateRandomSequence(DELAY_timing.Length, trials_for_cond * target_positions.Count);
        RT_timing_list = CreateRandomSequence(RT_timing.Length, trials_for_cond * target_positions.Count);
    }

    void Update()
    {
        // serve ancora ?
       // PupilDataConnessionStatus = PupilDataStreamScript.subsCtrl.IsConnected;  //verifico la connessione

        //if (PupilDataConnessionStatus)     // se si  connesso comincia a ciucciare i dati 
        //{
        //    //Debug.Log((centerRightPupilPx[0]).ToString());
        //    centerRightPupilPx = PupilDataStreamScript.CenterRightPupilPx;
        //    centerLeftPupilPx = PupilDataStreamScript.CenterLeftPupilPx;
        //    diameterRight = PupilDataStreamScript.DiameterRight;
        //    diameterLeft = PupilDataStreamScript.DiameterLeft;
        //    ardu.SendPupilLabData(centerRightPupilPx[0], centerRightPupilPx[1], centerLeftPupilPx[0], centerLeftPupilPx[1]); // e li mando a arduino
        //}
        // else di controlllo sulla connessione
        

        frame_number++;
        arduX = ardu.ax1;   //note: if arduino is not connected (or not working) the arduX,Y = NaN;
        arduY = ardu.ax2;

        if ((!float.IsNaN(arduX) && arduX != 0) || (!float.IsNaN(arduY) && arduY != 0) || Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0 ) //if arduX is nan, I cannot compare it with 0
        {
            
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }



        if (first_frame) // first operating frame 
        {
            Debug.Log("START TASK");
            starttime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(); //start time main task unity
            ardu.SendStartRecordingOE();             // Send START trigger
            first_frame = false;
            
        }

        

        #region StateMachine
 
        switch (current_state)
        {
            case 0: //INTERTRIAL
                if (last_state != current_state) //StateBeginning //State beginning (executed once each time the system enter the state)
                {

                    foreach (Camera cam in Player.GetComponentsInChildren<Camera>())
                    {
                        cam.backgroundColor = Color.white;
                    }

                    current_condition = -1;

                    lastevent = Time.time;
                    last_state = current_state;
                    error_state = "";
                }

                //StateBody //State body (executed every frame the system is in the state)
                ////////////////////////////////////////////////////
                current_condition = -1;
               
                if (((Time.time - lastevent) >= RewardLength_in_sec) && !isMoving && (PupilDataStreamScript.subsCtrl.IsConnected || RequestControllerScript.ans))   //StateEnd //State end (executed once each time the system exit the state)
                {   
                    // Prepare everything for next trial

                    // Choose and instantiate the target
                    current_condition = condition_list[0];
                    TargetCurrentPosition = target_positions[current_condition];
                    Target = Instantiate(TargetPrefab, TargetCurrentPosition, TargetPrefab.transform.rotation);
                    Target.transform.localScale = TargetSize;
                    Target.GetComponent<MeshRenderer>().enabled = false; //instantiate the target (not visible)
                                                     //OLD MANNER        //randomIndex = UnityEngine.Random.Range(0, target_label.Count);
                                                                         //current_condition = target_label[randomIndex];



                    // Choose the random times
                    // OLD MANNER // set_epochs_duration();

                    // Picking first time from the timing list to select epoch durations in this trial
                    FREE_duration = FREE_timing[FREE_timing_list[0]];
                    DELAY_duration = DELAY_timing[DELAY_timing_list[0]];
                    RT_duration = RT_timing[RT_timing_list[0]];

                    current_state = 1;

                    //the trial is starting
                    current_trial++;

                    foreach (Camera cam in Player.GetComponentsInChildren<Camera>())
                    {
                        cam.backgroundColor = Color.black;
                    }
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            case 1: //FREE
                if (last_state != current_state) //StateBeginning
                {
                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                    error_state = "";
                }

                //StateBody ////////////////////////////////////////
                if (isMoving)
                {
                    error_state = "ERR: Moving in FREE";
                    current_state = -99;
                }
                ////////////////////////////////////////////////////


                if ((Time.time - lastevent) >= FREE_duration && !isMoving) //StateEnd
                {
                    current_state = 2;
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 2: //DELAY
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
                    error_state = "";
                }

                //StateBody ////////////////////////////////////////
                if (isMoving)
                {
                    error_state = "ERR: Moving in DELAY";
                    current_state = -99;
                }
                ////////////////////////////////////////////////////

                if ((Time.time - lastevent) >= DELAY_duration && !isMoving)   //StateEnd
                {
                    current_state = 3;
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 3: //RT
                if (last_state != current_state) //StateBeginning
                {
                    // Switch target color
                    Target.GetComponent<MeshRenderer>().material.color = Color.red;

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                    error_state = "";
                }

                //StateBody ////////////////////////////////////////
                if (isMoving) 
                {
                    current_state = 99;
                }
                ////////////////////////////////////////////////////

                if ((Time.time - lastevent) >= RT_duration && !isMoving)   //StateEnd
                {
                    error_state = "ERR: Not Moving in RT";
                    current_state = -99;
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case -99: //ERROR
                if (last_state != current_state) //StateBeginning
                {
                    Debug.Log(error_state);
                    GetComponent<Saver>().addObjectEnd(identifier);
                    reset_lose();

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;
                }

                //StateBody ////////////////////////////////////////

                ////////////////////////////////////////////////////

                if (true)   //StateEnd 
                {
                    current_state = 0;
                    error_state = "";
                }

                break;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            case 99: //WIN
                if (last_state != current_state) //StateBeginning
                {
                    Debug.Log("TRIAL DONE");
                    GetComponent<Saver>().addObjectEnd(identifier);
                    reset_win();

                    //Beginning routine
                    lastevent = Time.time;
                    last_state = current_state;

                }

                //StateBody ////////////////////////////////////////
                if (condition_list.Count == 0) { QuitGame();  }

                ////////////////////////////////////////////////////

                    if (true)   //StateEnd
                {
                    current_state = 0;
                }

                break;


        }


        #endregion


        if (Input.GetKeyDown("space"))        { ardu.SendReward(RewardLength); }
        reward_counter = ardu.reward_counter;

    }

 

    void OnApplicationQuit()
    {
        ardu.SendStopRecordingOE();
        Debug.Log("END TASK");
        QuitGame();
    }

    void reset_win()
    {
        ardu.SendReward(RewardLength);
        Destroy(Target);

        current_state = 0;
        lastevent = Time.time;

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

        condition_list = SwapVector(condition_list);
        FREE_timing_list = SwapVector(FREE_timing_list); //not strictly necessary, but better for coherence...
        DELAY_timing_list = SwapVector(DELAY_timing_list);
        RT_timing_list = SwapVector(RT_timing_list);

        current_state = 0;
        lastevent = Time.time;
        trials_lose++;
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

    public List<int> CreateRandomSequence(int n, int k) //n, number of elements; k, length of the required vector
    {
        var vector = new List<int>();
        for (int i = 0; i < Math.Floor((double)k / n) + 1; i++)
        {
            var tmp = Enumerable.Range(0, n).OrderBy(x => UnityEngine.Random.Range(0, n)).ToList();
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
                        Debug.LogWarning("La riga non ha abbastanza coordinate: " + line);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Il file non esiste: " + filePath);
        }
    }
}