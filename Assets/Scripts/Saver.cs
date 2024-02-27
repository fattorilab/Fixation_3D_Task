using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEngine;
using PupilLabs;
using UnityEditor;

// FIXATION TASK

public class Saver : MonoBehaviour
{
    [HideInInspector] public long starttime = 0;
    private long time = 0;

    [HideInInspector] public GameObject ARDUINO;
    FixationTask FixationTask;
    [HideInInspector] public PupilDataStream PupilDataStreamSaver;
    private string currentDate;
    private string currentTime;
    private string path_to_data_OneTime;
    [HideInInspector] public string path_to_data;
    private RequestController RequestControllerScript;
    //private string new_Param; 
    // per il salvataggioDB
    GameObject DB; 
    public bool Want2Save = false;
    private bool wantToSave = false;

    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        DB = GameObject.Find("DB");
        
        ARDUINO = GameObject.Find("ARDUINO");

        //Debug.Log("File are saved in " + path_to_data); 
        RequestControllerScript = GameObject.Find("PupilDataManagment").GetComponent<RequestController>();

        FixationTask = GetComponent<FixationTask>();

        string MEF = FixationTask.MEF;
        path_to_data = FixationTask.path_to_data;

        if (MEF == "ciuffa") { path_to_data = path_to_data + "MEF27/DATI/"; }
        else if (MEF == "lisca") { path_to_data = path_to_data + "MEF28/DATI/"; }


        //activated = true;
        //possibili timestamps

        starttime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        addDataOneTime();
    }

    void LateUpdate()
    {
        time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - starttime;
        
        if (!FixationTask.first)
        {
            addDataPerFrame();
        }

        if (Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
    }

    private void OnApplicationQuit()
    {
        wantToSave = EditorUtility.DisplayDialog("Salvare i dati?", "Vuoi salvare i dati prima di uscire?", "Sì", "No");

        if (wantToSave)
        {
            saveAllData(";");
        }
        else
        {
            Debug.Log("Data not Saved");
        }
    }

    List<List<string>> PerFrameData = new List<List<string>>();
    List<List<string>> GameObjectData = new List<List<string>>();
    List<List<string>> SupplementData = new List<List<string>>();

    private void addDataPerFrame()
    {
        long milliseconds = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (starttime == 0) { starttime = milliseconds; }
        PerFrameData.Add(new List<string>()); //Adds new sub List
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.frame_number).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((milliseconds - starttime).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.trials_tot).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.TargetCurrentLabel).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.TargetCurrentPosition[0]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.TargetCurrentPosition[1]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.TargetCurrentPosition[2]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.state).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((FixationTask.error_state).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.PupilTimeStamps).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.CenterRightPupilPx[0]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.CenterRightPupilPx[1]).ToString()); 
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.CenterLeftPupilPx[0]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.CenterLeftPupilPx[1]).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.DiameterRight).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((PupilDataStreamSaver.DiameterLeft).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((ARDUINO.GetComponent<Ardu>().ax1).ToString());
        PerFrameData[(PerFrameData.Count - 1)].Add((ARDUINO.GetComponent<Ardu>().ax2).ToString()); 

    }

    public void addObject(string identifier, float x_pos, float y_pos, float z_pos, float x_scale, float y_scale, float z_scale, float x_rot, float y_rot, float z_rot)
    {
        //go.GetInstanceID() int to str
        //long time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - starttime;
        GameObjectData.Add(new List<string>()); //Adds new sub List
        GameObjectData[(GameObjectData.Count - 1)].Add(identifier); //.ToString()
        GameObjectData[(GameObjectData.Count - 1)].Add((x_pos).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((y_pos).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((z_pos).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((x_scale).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((y_scale).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((z_scale).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((x_rot).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((y_rot).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((z_rot).ToString("F5"));
        GameObjectData[(GameObjectData.Count - 1)].Add((time).ToString());
        GameObjectData[(GameObjectData.Count - 1)].Add("-1"); //time of object end is initialized -1
    }


    public void addObjectEnd(string identifier) //add the time of object end
    {
        //Debug.Log("Trying to remove " + identifier);
        // Più veloce se si passa l'elenco di quelli da eliminare e poi si scorre sempre l'elenco e si elimina quando viene trovato.
        bool found = false;

        List<int> matchingIndices = new List<int>();

        for (int i = 0; i < GameObjectData.Count; i++)
        {
            if (GameObjectData[i][0] == identifier)
            {
                matchingIndices.Add(i);
                found = true;
            }
        }

        if (found)
        {
            int lastIndex = matchingIndices.Max();
            GameObjectData[lastIndex][GameObjectData[GameObjectData.Count - 1].Count-1] = (time).ToString();
        }


        if (!found)
        {
            Debug.Log("Couldn't find object with ID " + identifier);
        }

    }

    private void addDataOneTime()
    {
           for (int i = 0; i < FixationTask.target_label.Count; i++)
            
            {
                SupplementData.Add(new List<string>()); //Adds new sub List
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.target_label[i].ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.positions[i][0].ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.positions[i][1].ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.positions[i][2].ToString());
                //SupplementData[(SupplementData.Count - 1)].Add(ARDUINO.GetComponent<Ardu>().dead_zone.ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.RewardLength.ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.TargetSize[0].ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.TargetSize[1].ToString());
                SupplementData[(SupplementData.Count - 1)].Add(FixationTask.TargetSize[2].ToString());
            }
    }

    private void saveAllData(string delimiter)
    {
        string Line = "";
        StringBuilder sb_PerFrame = new StringBuilder();
        StringBuilder sb_GameObject = new StringBuilder();
        StringBuilder sb_Supplement = new StringBuilder();

        // creo la struttura che contiene i dati frame per frame
        sb_PerFrame.AppendLine("Frame; Unity_timestamps; trial; condition; target_x; target_y; target_z; epoch; error ; Eyes_timestamps; px_eye_right; py_eye_right; px_eye_left; py_eye_left; DiameterRight; DiameterLeft; ArduX; ArduY");

        for (int index = 0; index < PerFrameData.Count; index++)
        {
            Line = "";

            for (int counteri = 0; counteri < PerFrameData[index].Count; counteri++)
            {
                Line += PerFrameData[index][counteri];  //row structure
                if (counteri != (PerFrameData[index].Count - 1)) { Line += delimiter; }
            }
            sb_PerFrame.AppendLine(Line);
        }

        //Create Ontime writer
        sb_GameObject.AppendLine("Name; x; y; z; scale_x; scale_y; scale_z; rot_x; rot_y; rot_z; TimeEntry; TimeExit"); //Id x y type 

        for (int index = 0; index < GameObjectData.Count; index++)
        {
            Line = "";

            for (int counteri = 0; counteri < GameObjectData[index].Count; counteri++)
            {
                Line += GameObjectData[index][counteri];  //Costruzione delle righe
                if (counteri != (GameObjectData[index].Count - 1)) { Line += delimiter; }
            }
            sb_GameObject.AppendLine(Line);
        }
       
        //Create Sypplement
        sb_Supplement.AppendLine("Label; Target_X; Target_Y; Target_Z; RewardLength; SizeX; SizeY; SizeZ");//DeadZone; RewardLength; SizeX; SizeY; SizeZ");
        for (int index = 0; index < SupplementData.Count; index++)
        {
            Line = "";

            for (int counteri = 0; counteri < SupplementData[index].Count; counteri++)
            {
                Line += SupplementData[index][counteri];  //row structure
                if (counteri != (SupplementData[index].Count - 1)) { Line += delimiter; }
            }
            Debug.Log(Line);
            sb_Supplement.AppendLine(Line);
        }
        if (Want2Save)
        {
            string new_Date = DateTime.Now.ToString("yyyy/MM/dd");
            string new_Task = "CalibrationTask";
            string new_Param = "";

            if (RequestControllerScript.connectOnEnable)
            {
                 new_Param = new_Param + "EyeTracker=ON";
            }
            else
            {
                 new_Param = new_Param + "EyeTracker=OFF";
            }

            int lastIDFromDB = DB.GetComponent<InteractWithDB>().GetLastIDfromDB();
            int new_ID = lastIDFromDB + 1;

            

            DB.GetComponent<InteractWithDB>().AddRecording(new_ID, new_Date, new_Task, new_Param);

            string path_to_data_PerFrameData = path_to_data + DateTime.Now.ToString("yyyy_MM_dd") + "_PerFrameData_ID" + new_ID.ToString() + ".csv";
            string path_to_data_GameObjectData = path_to_data + DateTime.Now.ToString("yyyy_MM_dd") + "_GameObjectData_ID" + new_ID.ToString() + ".csv";
            string path_to_data_SupplementData = path_to_data + DateTime.Now.ToString("yyyy_MM_dd") + "_SupplementData_ID" + new_ID.ToString() + ".csv";
            
            File.WriteAllText(path_to_data_PerFrameData, sb_PerFrame.ToString());
            File.WriteAllText(path_to_data_GameObjectData, sb_GameObject.ToString());
            File.WriteAllText(path_to_data_SupplementData, sb_Supplement.ToString());
            Debug.Log("Saved all data");
        }

    }

    
}
