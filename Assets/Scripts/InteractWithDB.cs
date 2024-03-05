using System.Data;
using UnityEngine;
using IDbCommand = System.Data.IDbCommand;
using IDbConnection = System.Data.IDbConnection;
using Mono.Data.Sqlite;
using System;

public class InteractWithDB : MonoBehaviour
{
    string conn;
    string sqlQuery;
    IDbConnection dbconn;
    IDbCommand dbcmd;
    IDataReader dbreader;
    string path_to_data;
    GameObject experiment;

    void Start()
    {
        experiment = GameObject.Find("Experiment");
        string MEF = experiment.GetComponent<MainTask>().MEF;
        path_to_data = experiment.GetComponent<MainTask>().path_to_data;

        if (MEF == "ciuffa") { path_to_data = path_to_data + "MEF27/esperimentiVR.db"; }
        else if (MEF == "lisca") { path_to_data = path_to_data + "MEF28/esperimentiVR.db"; }
    }

    public int GetLastIDfromDB()
    {
        int lastID = -1; // Initialize lastID with a default value in case no records are found

        Debug.Log("Connecting to DB " + $"DB_filepath={path_to_data} for reading last ID");
        conn = "URI=file:" + path_to_data;

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();

            dbcmd = dbconn.CreateCommand();
            sqlQuery = "SELECT * FROM Recordings ORDER BY ID DESC LIMIT 1;";
            dbcmd.CommandText = sqlQuery;

            dbreader = dbcmd.ExecuteReader();

            // Check if there's a record in the result
            if (dbreader.Read())
            {
                // Get the value of "ID" from the current record
                lastID = dbreader.GetInt32(dbreader.GetOrdinal("ID"));
            }

            dbreader.Close();
            dbconn.Close();

            // Return the lastID value
            Debug.Log("Last ID from DB " + lastID);
            return lastID;
        }
    }


    public void AddRecording(int new_ID, string new_Date, string new_Task, string new_Param)
    {
        Debug.Log("Connecting to DB " + $"DB_filepath={path_to_data} to add new recording");
        conn = "URI=file:" + path_to_data;

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();

            dbcmd = dbconn.CreateCommand();
            sqlQuery = "INSERT INTO Recordings (ID, Date, Task, Param) VALUES ('" + new_ID + "','" + new_Date + "','" + new_Task + "','" + new_Param + "')";
            dbcmd.CommandText = sqlQuery;

            dbcmd.ExecuteNonQuery();

            dbconn.Close();

            Debug.Log("New rec. added to DB with ID " + new_ID);
        }
    }
}