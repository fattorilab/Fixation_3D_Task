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
 * - Define in MainTask the same public variables as in region Task general variables.
 * - Here, modify regions DEFINE FRAME DATA and Create Data Writer (method saveAllData) 
 *      to include variables specific to your task (see task_specific_vars).
 * - in region Add recording to DB (method saveAllData), add or remove components whose
 *      public fields you want to register as JSON in the DB.
 */

public class Saver : MonoBehaviour
{
    #region Time variables
    [HideInInspector] public long starttime = 0;
    [HideInInspector] public long milliseconds = 0;
    #endregion

    #region Saving variables
    [HideInInspector] public static bool wants2saveData;
    [HideInInspector] public static bool wants2saveVideos;
    [HideInInspector] public string path_to_data;
    [HideInInspector] public string path_to_MEF;
    [HideInInspector] public int lastIDFromDB;
    #endregion

    #region Task general variables
    [HideInInspector] int current_trial;
    [HideInInspector] int current_condition;
    [HideInInspector] int current_state;
    [HideInInspector] string error_state;
    #endregion

    #region GameObjects and components
    MainTask main; // Experiment main script
    Ardu ardu;
    GameObject DB;
    GameObject player;
    GameObject experiment;
    GameObject PupilData;
    PupilDataStream PupilDataStream;
    #endregion

    void Awake()
    {
        #region Choose monkey and set path

        string MEF = GetComponent<MainTask>().MEF;
        path_to_data = GetComponent<MainTask>().path_to_data;
        if (MEF.ToLower() == "ciuffa") { path_to_MEF = Path.Combine(path_to_data, "MEF27"); }
        else if (MEF.ToLower() == "lisca") { path_to_MEF = Path.Combine(path_to_data, "MEF28"); }
        else
        {
            bool ans = EditorUtility.DisplayDialog("Wrong MEF name", "Unable to find the monkey" + MEF, //don't know how to put a simple popup here (the choice is irrelevant)
                            "Close and check MEF in MainTask");
            QuitGame();
        }

        Debug.Log($"If desidered, files will be saved in {path_to_MEF}");

        #endregion

        #region Connect to DB and get last ID

        try
        {
            DB = GameObject.Find("DB");
            string path_to_DB = Path.Combine(path_to_MEF, "esperimentiVR.db");
            lastIDFromDB = DB.GetComponent<InteractWithDB>().GetLastIDfromDB(path_to_DB);
        }
        catch
        {
            bool ans = EditorUtility.DisplayDialog("Cannot interact with DB", "It is not possible to read last ID from database. You may not to be able to save data",
                            "Close and check DB", "Proceed anyway");
            if (ans) { QuitGame(); }
        }

        #endregion

    }


    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        #region Get GameObjects and Components

        main = GetComponent<MainTask>();
        ardu = GetComponent<Ardu>();
        PupilData = GameObject.Find("PupilDataManagment");
        PupilDataStream = PupilData.GetComponent<PupilDataStream>();
        DB = GameObject.Find("DB");
        player = GameObject.Find("Player");

        #endregion

        // Seed
        addObject("Seed", "Seed", main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed, main.seed);
    }

    void LateUpdate()
    {
        addDataPerFrame();

        if (Input.GetKeyDown("escape"))
        {
            QuitGame();
        }
    }

    private void OnApplicationQuit()
    {
        // Ask user if she wants to save the videos
        wants2saveVideos = WantsToSaveVideos();

        // Ask user if she wants to save the csv files
        wants2saveData = WantsToSaveData();
        if (wants2saveData)
        {
            saveAllData(";");
        }
        Application.Quit();
    }

    #region WANT TO SAVE - POPUPS

    // Ask user if she wants to save the csv files
    static bool WantsToSaveData()
    {
        bool wantsToSave = EditorUtility.DisplayDialog("SAVE DATA", "Do you want to save frame data?", "Yes", "No");
        if (!wantsToSave)
        {
            // Ask user if she's sure not to save
            string The_End = ("Everything not saved will be lost.@- Nintendo \"Quit Screen\" message.").Replace("@", System.Environment.NewLine);
            bool sure = EditorUtility.DisplayDialog("ARE YOU SURE?", The_End, "Yes", "No");
            if (!sure)
            {
                // If user not sure, ask again
                return WantsToSaveData();
            }
            return wantsToSave;
        }
        return wantsToSave;
    }

    // Ask user if she wants to save the csv files
    static bool WantsToSaveVideos()
    {
        bool wantsToSave = EditorUtility.DisplayDialog("SAVE VIDEO", "Do you want to save the video?", "Yes", "No");
        if (!wantsToSave)
        {
            // Ask user if she's sure not to save
            string The_End = ("Everything not saved will be lost.@- Nintendo \"Quit Screen\" message.").Replace("@", System.Environment.NewLine);
            bool sure = EditorUtility.DisplayDialog("ARE YOU SURE?", The_End, "Yes", "No");
            if (!sure)
            {
                // If user not sure, ask again
                return WantsToSaveVideos();
            }
            return wantsToSave;
        }
        return wantsToSave;
    }

    #endregion

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
        PerFrameData[(PerFrameData.Count - 1)].Add((main.frame_number).ToString());
        // Trials
        PerFrameData[(PerFrameData.Count - 1)].Add((main.current_trial).ToString("F5"));
        PerFrameData[(PerFrameData.Count - 1)].Add((main.trials_win).ToString("F5"));
        // Condition
        PerFrameData[(PerFrameData.Count - 1)].Add((main.current_condition).ToString("F5"));
        // State
        PerFrameData[(PerFrameData.Count - 1)].Add((main.last_state).ToString("F5"));
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
        string general_vars = "Unity_timestamp; Frame; ";
        string task_general_vars = "Trial; Correct Trials; Current_condition; Current_state; Error_type; Reward_count; ";
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

        // Get time
        string new_Date = DateTime.Now.ToString("yyyy/MM/dd");

        // Get name of task
        string[] s = Application.dataPath.Split('/');
        string projectName = s[s.Length - 2];
        string new_Task = projectName;

        // Get parameters from public fields of the scripts of the experiment
        string jsonMainTask = JsonUtility.ToJson(main, true);
        string new_Param = "{ \"MainTask script params\": " + jsonMainTask + " }";

        // Save entry to db
        string path_to_DB = Path.Combine(path_to_MEF, "esperimentiVR.db");
        int new_ID = lastIDFromDB + 1;
        DB.GetComponent<InteractWithDB>().AddRecording(path_to_DB, new_ID, new_Date, new_Task, new_Param);

        // Save CSV
        string path_to_data_PerFrame = Path.Combine(path_to_MEF, "DATI", (DateTime.Now.ToString("yyyy_MM_dd") + "_ID" + new_ID.ToString() + "data.csv"));
        string path_to_data_Supplement = Path.Combine(path_to_MEF, "DATI", (DateTime.Now.ToString("yyyy_MM_dd") + "_ID" + new_ID.ToString() + "supplement.csv"));
        File.WriteAllText(path_to_data_PerFrame, sb_PerFrame.ToString());
        File.WriteAllText(path_to_data_Supplement, sb_Supplement.ToString());

        Debug.Log($"Data successfully saved in {Path.Combine(path_to_MEF, "DATI")}");
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

