using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;
using PupilLabs;
using Newtonsoft.Json;  
using Newtonsoft.Json.Linq;


public class FixationTask : MonoBehaviour
{
    [SerializeField]

    [HideInInspector]
    [Header("Collegamenti")]
    public PupilDataStream PupilDataStreamScript;
    private RequestController RequestControllerScript;
    private bool PupilDataConnessionStatus;
    [HideInInspector] public GameObject Player;

    [Header("Saving info")]
    public string MEF;
    public string path_to_data = "C:/Users/admin/Desktop/Registrazioni_VR/";


    [Header("Target Info")]
    public string file_name_positions; 
    public GameObject TargetPrefab; GameObject Target; // qui ci andra il prefab
    public Vector3 TargetCurrentPosition;    
    public int TargetCurrentLabel;   
    public Vector3 TargetSize;

    [HideInInspector] public List<Vector3> positions = new List<Vector3>(); 
    [HideInInspector] public List<int> target_label;     
    private int randomIndex;      

    [Header("Epoches Info")]
    
    public float[] BLACK_timing = { 0.3f, 0.6f, 0.9f };
    public float[] GREEN_timing = { 0.3f, 0.6f, 0.9f };
    public float[] RED_timing = { 0.3f, 0.6f, 0.9f };    

    private float BLACK_duration = 1f;
    private float GREEN_duration;  
    private float RED_duration; 

 
    [HideInInspector] public int frame_number = 0; 

    [Header("Trials Info")]
    
    public int state;
    public int last_state;
    public int error_state; 
    public int trials_tot;   
    public int trials_win;         
    public int trials_lose;           
    public int[] trials_for_target;   
    public int ripetition_for_target; 

 
    private float lastevent;

   
    [HideInInspector] public bool first;

    // variabili per joystic
    [Header("Arduino Info")]
    public Ardu arduscript;
    public float arduX = 0;
    public float arduY = 0;
    //public int dead_zone;

    // per reward
    public int RewardLength = 20;
    
    [Header("PupilLab Info")]
    public Vector2 centerRightPupilPx = new Vector2(float.NaN, float.NaN);
    public Vector2 centerLeftPupilPx = new Vector2(float.NaN, float.NaN);
    public float diameterRight = float.NaN;
    public float diameterLeft = float.NaN;
    public bool pupilconnection;    

    // Start is called before the first frame update
    void Start()
    {

        foreach (Camera cam in Player.GetComponentsInChildren<Camera>())
        {
            cam.backgroundColor = Color.white;
        }
     
        state = 0;
        last_state = -1;
        error_state = 0;
        trials_tot = 0;
        trials_win = 0;
        trials_lose = 0;
        first = true;

        arduscript = GameObject.Find("ARDUINO").GetComponent<Ardu>(); // Assumi che l'oggetto Joystick abbia il nome "Joystick"

        PupilDataStreamScript = GameObject.Find("PupilDataManagment").GetComponent<PupilDataStream>();
        
        RequestControllerScript = GameObject.Find("PupilDataManagment").GetComponent<RequestController>();
        RequestControllerScript.connectOnEnable = pupilconnection;
        // importo le posizioni dal file csv
        LoadPositionsFromCSV();
        
        trials_for_target = new int[positions.Count];

        for (int i = 0; i < positions.Count; i++)
        {
            target_label.Add(i); 
        }
        //string json = SaveToJson();
        //File.WriteAllText("C:/Users/stefa/Documents/Unity Projects/Registrazioni VR/person.json", json);
        //SaveToJson(");
    }
   
    void Update()
    {
        PupilDataConnessionStatus = PupilDataStreamScript.subsCtrl.IsConnected;
      
       if (PupilDataConnessionStatus)
        {
            //Debug.Log((centerRightPupilPx[0]).ToString());
            centerRightPupilPx = PupilDataStreamScript.CenterRightPupilPx;
            centerLeftPupilPx = PupilDataStreamScript.CenterLeftPupilPx;
            diameterRight = PupilDataStreamScript.DiameterRight;
            diameterLeft = PupilDataStreamScript.DiameterLeft;
            arduscript.SendPupilLabData(centerRightPupilPx[0], centerRightPupilPx[1], centerLeftPupilPx[0], centerLeftPupilPx[1]);
        }
        
        arduX = arduscript.ax1;
        arduY = arduscript.ax2;
        //dead_zone = arduscript.dead_zone;

        if ((PupilDataConnessionStatus && state != 4) || (RequestControllerScript.connectOnEnable == false))
        {
            frame_number++;
            if (first) // PRIMO FRAME CON PUPIL CONNESSO
            {
                foreach (Camera cam in Player.GetComponentsInChildren<Camera>())
                {
                    cam.backgroundColor = Color.black;
                }
                //Camera.main.backgroundColor = Color.black;
                Debug.Log("START TASK");
                // TRIGGER START REGISTRAZIONE
                arduscript.SendStartRecordingOE();
                first = false; 
            }

            if (target_label.Count == 0 && state != 4)
            {
                // TRIGGER OFF REGISTRAZIONE
                arduscript.SendStopRecordingOE();
                Debug.Log("END TASK");
                state = 4;
                QuitGame();
            }

            switch (state)
            {
                case 0:
                    if (last_state != 0)
                    {
                        last_state = 0; //track that we were in state 0

                        randomIndex = Random.Range(0, target_label.Count); // add  fra 22/feb/2024
                        TargetCurrentLabel = target_label[randomIndex]; // add  fra 22/feb/2024
                        TargetCurrentPosition = positions[TargetCurrentLabel]; // add  fra 22/feb/2024
                        Target = Instantiate(TargetPrefab, TargetCurrentPosition, TargetPrefab.transform.rotation);
                        Target.transform.localScale = TargetSize;
                        Target.GetComponent<MeshRenderer>().enabled = false;
                        //Renderer renderer = Target.GetComponent<Renderer>();
                        //renderer.enabled = false;

                        GetComponent<Saver>().addObject(TargetCurrentLabel.ToString(),
                            Target.transform.localPosition.x,
                            Target.transform.localPosition.y,
                            Target.transform.localPosition.z,
                            Target.transform.localScale.x,
                            Target.transform.localScale.y,
                            Target.transform.localScale.z,
                            Target.transform.localRotation.x,
                            Target.transform.localRotation.y,
                            Target.transform.localRotation.z);
                    }

                    if (arduX == 0 && arduY == 0)
                    {
                        Debug.Log("FREE");
                        state = 1;
                        error_state = 0;
                        
                        //randomIndex = Random.Range(0, target_label.Count); comment fra 22/feb/2024
                        //TargetCurrentLabel = target_label[randomIndex];
                        
                        set_epochs_duration();
                        trials_tot++;
                        lastevent = Time.time;
                    }
                    else
                    {
                       
                        error_state = 0;                        
                    }
                    break;

                case 1:
                    if (last_state != 1)
                    {
                        last_state = 1; //track that we were in state 1
                    }
                    if ((Time.time - lastevent) >= BLACK_duration && arduX == 0 && arduY == 0) 
                    {
                        Debug.Log("DELAY");
                        //TargetCurrentPosition = positions[TargetCurrentLabel]; comment fra 22/feb/2024
                        //Target = Instantiate(TargetPrefab, TargetCurrentPosition, TargetPrefab.transform.rotation);

                        //Target.transform.localScale = TargetSize;

                        /* comment fra 22/feb/2024 
                        
                        GetComponent<Saver>().addObject(TargetCurrentLabel.ToString(),
                            Target.transform.localPosition.x,
                            Target.transform.localPosition.y,
                            Target.transform.localPosition.z,
                            Target.transform.localScale.x,
                            Target.transform.localScale.y,
                            Target.transform.localScale.z,
                            Target.transform.localRotation.x,
                            Target.transform.localRotation.y,
                            Target.transform.localRotation.z);
                        */
                        Target.GetComponent<MeshRenderer>().enabled = true;
                        Target.GetComponent<MeshRenderer>().material.color = Color.green;
                        state = 2;
                        lastevent = Time.time;
                    }
                    else if ((Time.time - lastevent) < BLACK_duration && (arduX != 0 || arduY != 0))
                    {
                   
                        Debug.Log("FREE ERROR");
                        error_state = 1;
                        reset_lose();
                    }
                    break;

                case 2:
                    if (last_state != 2)
                    {
                        last_state = 2; //track that we were in state 1
                    }
                    if ((Time.time - lastevent) >= GREEN_duration && arduX == 0 && arduY == 0)
                    {
                        Debug.Log("RT");
                        Target.GetComponent<MeshRenderer>().material.color = Color.red;
                        state = 3;
                        lastevent = Time.time;
                    }
                    else if ((Time.time - lastevent) < GREEN_duration && (arduX != 0 || arduY != 0))
                    {
                        // errore nel DELAY
                        Debug.Log("DELAY ERROR");
                        error_state = 2;
                        reset_lose();

                    }
                    break;

                case 3:
                    if (last_state != 3)
                    {
                        last_state = 3; //track that we were in state 1
                    }
                    if ((Time.time - lastevent) <= RED_duration && (arduX != 0 || arduY != 0 || Input.GetKey("space")))
                    {
                        //Debug.Log("RT");

                        reset_win();
                    }
                    else if ((Time.time - lastevent) > RED_duration)
                    {
                        // errore nel RT
                        Debug.Log("RT ERROR");
                        error_state = 3;
                        reset_lose();
                    }
                    break;
            }
        }

        if (PupilDataStreamScript.subsCtrl.IsConnected && state == 4)
        {
            QuitGame();
        }
    }

    void OnApplicationQuit() 
    {
        arduscript.SendStopRecordingOE();
        Debug.Log("END");
        state = 4;
        QuitGame();
    }

    void reset_win()
    {

        //last_win = true;
        arduscript.SendReward(RewardLength);
        GetComponent<Saver>().addObjectEnd(TargetCurrentLabel.ToString());
        Destroy(Target);


        state = 0;
        error_state = 0;
        lastevent = Time.time;

        trials_win++;
        trials_for_target[TargetCurrentLabel]++;
        if (trials_for_target[TargetCurrentLabel] == ripetition_for_target)
        {
            target_label.Remove(TargetCurrentLabel);
        }


    }

    void reset_lose()
    {
        Debug.Log("FAIL");
        //Debug.Log(TargetCurrentLabel.ToString());
        GetComponent<Saver>().addObjectEnd(TargetCurrentLabel.ToString());
        Destroy(Target);

        state = 0;
        TargetCurrentLabel = -1;
        lastevent = Time.time;
        trials_lose = trials_tot - trials_win;
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

    void set_epochs_duration()
    {
        int randomIndex_BLACK = Random.Range(0, BLACK_timing.Length);
        int randomIndex_GREEN = Random.Range(0, GREEN_timing.Length);
        int randomIndex_RED = Random.Range(0, RED_timing.Length);

        BLACK_duration = BLACK_timing[randomIndex_BLACK];
        GREEN_duration = GREEN_timing[randomIndex_GREEN];
        RED_duration = RED_timing[randomIndex_RED];

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
                            positions.Add(position);
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



public class SaveManager : MonoBehaviour
{
    // Riferimento alla tua istanza di FixationTask
    public FixationTask fixationTask;

    // Il percorso del file JSON in cui desideri salvare i dati
    public string saveFilePath = "C:/Users/stefa/Documents/Unity Projects/Registrazioni VR/person.json";

    public void SaveToJson()
    {
        // Serializza l'istanza di FixationTask in una stringa JSON
        string jsonData = JsonConvert.SerializeObject(fixationTask, Formatting.Indented);

        // Salva la stringa JSON su disco
        System.IO.File.WriteAllText(saveFilePath, jsonData);

        Debug.Log("Dati salvati con successo in " + saveFilePath);
    }
}





