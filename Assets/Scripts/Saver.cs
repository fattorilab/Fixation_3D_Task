using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System;
using PupilLabs;

/* WHAT TO KNOW FOR THIS SAVER TO WORK
 * - The main script/component of the experiment needs to be named MainTask.
 * - MainTask must have a public bool variable SAVE_CSV (whether to record or not).
 * - Define in MainTask the same public variables as in region Task general variables.
 * - Here, modify regions DEFINE FRAME DATA and Create Data Writer (method saveAllData) 
 *      to include variables specific to your task (see task_specific_vars)
 */

public class Saver : MonoBehaviour
{

    #region Time variables
    [HideInInspector] public int frame_counter = 0;
    #endregion

    #region Saving variables
    [HideInInspector] public bool SAVE_CSV;
    [HideInInspector] public string path_to_data;
    int lastIDFromDB;
    #endregion

    #region GameObjects and components
    MainTask main; // Experiment main script
    Ardu ardu;
    GameObject DB;
    GameObject player;
    GameObject experiment;
    [HideInInspector] public GameObject PupilData;
    PupilDataStream PupilDataStream;
    #endregion

    #region Task general variables
    [HideInInspector] int current_trial;
    [HideInInspector] int current_condition;
    [HideInInspector] int current_state;
    [HideInInspector] string error_state;
    #endregion

    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        #region Choose Monkey and set path

        experiment = GameObject.Find("Experiment");
        SAVE_CSV = experiment.GetComponent<MainTask>().SAVE_CSV;
        string MEF = experiment.GetComponent<MainTask>().MEF;
        path_to_data = experiment.GetComponent<MainTask>().path_to_data;
        if (MEF.ToLower() == "ciuffa") { path_to_data = Path.Combine(path_to_data, "MEF27"); }
        else if (MEF.ToLower() == "lisca") { path_to_data = Path.Combine(path_to_data, "MEF28"); }
        else 
        {
            bool ans = EditorUtility.DisplayDialog("Wrong MEF name", "Unable to find the monkey" + MEF, //don't know how to put a simple popup here (the choice is irrelevant)
                            "Close and check MEF in MainTask", "Close and check MEF in MainTask");
            QuitGame();     
        }

        try
        {
            lastIDFromDB = DB.GetComponent<InteractWithDB>().GetLastIDfromDB();
        }
        catch
        {
            bool ans = EditorUtility.DisplayDialog("Cannot interact with DB", "It is not possible to read last ID from database. You may not to be able to save data",
                            "Close and check DB", "Proceed anyway");
            if (ans) { QuitGame(); }  
        }
            

        #endregion

        #region Get GameObjects and Components

        main = GetComponent<MainTask>();
        ardu = GetComponent<Ardu>();
        PupilDataStream = PupilData.GetComponent<PupilDataStream>();
        //DB = GameObject.Find("DB");
        //player = GameObject.Find("Player");


        #endregion

        #region Get Task variables
        current_trial = main.current_trial;
        current_state = main.current_state;
        error_state = main.error_state;
        #endregion

        //starttime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds() + 1000000; // why?????
        addObject("Seed", "Seed", main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed);
    }

    void LateUpdate()
    {   
        if (frame_counter == 0) //first frame
        {
            player = GameObject.Find("Player");
            DB = GameObject.Find("DB");
        }
        // Add current frame data if not first state
        frame_counter++;
        if (experiment.GetComponent<MainTask>().SAVE_CSV) { addDataPerFrame(); }


        if (Input.GetKeyDown("escape"))
        {
            QuitGame();
        }
    }

    private void OnApplicationQuit()
    {
        if (experiment.GetComponent<MainTask>().SAVE_CSV) { saveAllData(";"); }
        QuitGame();
    }

    #region DEFINE PerFrame DATA

    // Initiate List to store data
    List<List<string>> PerFrameData = new List<List<string>>();

    private void addDataPerFrame()
    {
        // Add new sub List
        PerFrameData.Add(new List<string>());

        // Frames and time
        long milliseconds = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (main.starttime == 0) { main.starttime = milliseconds; }
        PerFrameData[(PerFrameData.Count - 1)].Add((milliseconds - main.starttime).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((frame_counter).ToString());
        // Trials
        PerFrameData[(PerFrameData.Count - 1)].Add((main.current_trial).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((main.trials_win).ToString("F5"));
        // Condition
        PerFrameData[(PerFrameData.Count - 1)].Add((main.current_condition).ToString("F5"));
        // State
        PerFrameData[(PerFrameData.Count - 1)].Add((main.current_state).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add(main.error_state); //is already a string
        // Arduino
        PerFrameData[(PerFrameData.Count - 1)].Add((ardu.reward_counter).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((ardu.ax1).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((ardu.ax2).ToString("F5"));
        // Player positions
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.position.x).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.position.y).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.position.z).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.eulerAngles.x).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.eulerAngles.y).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((player.transform.eulerAngles.z).ToString("F5"));
        // Eyes
        try //sistemare per gestire eccezioni di PupilLab!
        {
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.PupilTimeStamps).ToString());
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.CenterRightPupilPx[0]).ToString("F5"));
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.CenterRightPupilPx[1]).ToString("F5"));
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.CenterLeftPupilPx[0]).ToString("F5"));
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.CenterLeftPupilPx[1]).ToString("F5"));
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.DiameterLeft).ToString("F5"));
            PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.DiameterRight).ToString("F5"));
            //PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.confidence_L).ToString("F5"));
            //PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStream.confidence_R).ToString("F5"));
        }
        catch { }

    }

    #endregion

    #region DEFINE SUPPLEMENT (OBJECTS) DATA

    // Initiate List to store data
    List<List<string>> SupplementData = new List<List<string>>();

    public void addObject(string identifier, string type,
                            float x_pos, float y_pos, float z_pos,
                                float x_rot, float y_rot, float z_rot,
                                 float x_scale, float y_scale, float z_scale)
    {
        long milliseconds = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (main.starttime == 0) { main.starttime = milliseconds; }

        SupplementData.Add(new List<string>()); //Adds new sub List
        SupplementData[(SupplementData.Count - 1)].Add(identifier);
        SupplementData[(SupplementData.Count - 1)].Add(type);
        SupplementData[(SupplementData.Count - 1)].Add((x_pos).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((y_pos).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((z_pos).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((x_rot).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((y_rot).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((z_rot).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((x_scale).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((y_scale).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((z_scale).ToString("F5"));
        SupplementData[(SupplementData.Count - 1)].Add((milliseconds - main.starttime).ToString());
        SupplementData[(SupplementData.Count - 1)].Add("-1");
    }


    public void addObjectEnd(string identifier)
    {
        //Debug.Log("Trying to remove " + identifier);
        // Someone broke the function. Please leave this function alone! All the main saving was broken. Gianni

        long milliseconds = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (main.starttime == 0) { main.starttime = milliseconds; }

        bool found = false;

        for (int i = SupplementData.Count - 1; i >= 0; i--) //going backward from last object to the first one
        {
            if (SupplementData[i][0] == identifier)
            {
                SupplementData[i][(SupplementData[i].Count - 1)] = (milliseconds - main.starttime).ToString();
                found = true;
                break; // This will exit the loop immediately
            }
        }

        if (!found)
        {
            Debug.Log("Couldn't find object with ID " + identifier);
        }

    }

    #endregion

    private void saveAllData(string delimiter)
    {
        StringBuilder sb_PerFrame = new StringBuilder();
        StringBuilder sb_Supplement = new StringBuilder();
        string Line = "";

        #region Create FrameData writer
        string general_vars = "Unity_timestamp; Frames; ";
        string task_general_vars = "Trial; Correct Trials; Current_condition; Current_state; Error_state; Reward_count; ";
        // Change task_specific_vars as desired (AddFrameData() method must be changed accordingly)
        string task_specific_vars = "";
        string move_vars = "player_x_arduino; player_y_arduino; player_x;  player_y; player_z; player_x_rot; player_y_rot; player_z_rot; ";
        string eyes_vars = "pupil_timestamp; px_eye_right; py_eye_right; px_eye_left; py_eye_left; " +
                                "eye_diameter_left; eye_diameter_right";
                                //eye_confidence_left; eye_confidence_right";

        sb_PerFrame.AppendLine(general_vars + task_general_vars + task_specific_vars + move_vars + eyes_vars);

        for (int index = 0; index < PerFrameData.Count; index++)
        {
            Line = "";

            for (int counteri = 0; counteri < PerFrameData[index].Count; counteri++)
            {
                Line += PerFrameData[index][counteri];
                if (counteri != (PerFrameData[index].Count - 1)) { Line += delimiter; }
            }
            sb_PerFrame.AppendLine(Line);
        }
        #endregion

        #region Create Supplement writer
        sb_Supplement.AppendLine("Identifier; Type; x; y; z; rot_x; rot_y; rot_z; scale_x; scale_y; scale_z; TimeEntry; TimeExit");

        for (int index = 0; index < SupplementData.Count; index++)
        {
            Line = "";

            for (int counteri = 0; counteri < SupplementData[index].Count; counteri++)
            {
                Line += SupplementData[index][counteri];  //Costruzione delle righe
                if (counteri != (SupplementData[index].Count - 1)) { Line += delimiter; }
            }
            sb_Supplement.AppendLine(Line);
        }
        #endregion

        #region Add recording to the DB
        if (SAVE_CSV)
        {
            // Get time
            string new_Date = DateTime.Now.ToString("yyyy/MM/dd");

            // Get name of task
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            string new_Task = projectName;

            // Get parameters from public fields of main and movement
            string jsonMainTask = JsonUtility.ToJson(main, true);
            //string jsonMovement = JsonUtility.ToJson(player.GetComponent<Movement>(), true);
            string new_Param = "{ \"MainTask script params\": " + jsonMainTask + "}";
                //+ ", \"Movement params\": " + jsonMovement + " }";

            // Save entry to db
            int new_ID = lastIDFromDB + 1;
            DB.GetComponent<InteractWithDB>().AddRecording(new_ID, new_Date, new_Task, new_Param);

            // Save CSV
            string path_to_data_PerFrame = Path.Combine(path_to_data, "DATI", (DateTime.Now.ToString("yyyy_MM_dd") + "_ID" + new_ID.ToString() + "data.csv"));
            string path_to_data_Supplement = Path.Combine(path_to_data, "DATI", (DateTime.Now.ToString("yyyy_MM_dd") + "_ID" + new_ID.ToString() + "supplement.csv"));
            File.WriteAllText(path_to_data_PerFrame, sb_PerFrame.ToString());
            File.WriteAllText(path_to_data_Supplement, sb_Supplement.ToString());

            Debug.Log($"Data successfully saved in {Path.Combine(path_to_data, "DATI")}");
        }
        #endregion
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}

