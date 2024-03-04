using System;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Linq;
 public class arduino
{
    SerialPort sp;
    List<float> lastXValues = new List<float>(); // List to store last X values
    List<float> lastYValues = new List<float>();// 
    int COMspeed;
    string COM;
    public int JSdeadzone;
    Thread thread;
    bool stopThread;
    float lastSampleTime;

     public arduino(string _COM, int _COMspeed, int _JSdeadzone)

    {
        COMspeed = _COMspeed;
        COM = _COM;
        JSdeadzone = _JSdeadzone;
        sp = new SerialPort("\\\\.\\" + COM, COMspeed);
        lastSampleTime = Time.time;
        if (!sp.IsOpen)
        {
            Debug.Log("Apertura " + COM + ", baud " + COMspeed);
            sp.Open();
            if (sp.IsOpen) { Debug.Log("Seriale comunicazione Arduino aperto correttamente"); }
        }
        thread = new Thread(JoySample);
        thread.Start();
    }

    public void JoySample()
    {
        StringBuilder buffer = new StringBuilder();


        while (!stopThread)
        {
            if (!sp.IsOpen)
            {
                sp.Open();
                Debug.Log("Riapertura seriale in lettura");
            }

            if (sp.IsOpen)
            {
                string receivedData = sp.ReadExisting();
                buffer.Append(receivedData);

                // Process data in the buffer
                int newLineIndex;
                while ((newLineIndex = buffer.ToString().IndexOf('\n')) >= 0)
                {
                    string line = buffer.ToString(0, newLineIndex - 1);
                    buffer.Remove(0, newLineIndex +1);
                    
                    
                    // Ensure the line contains the expected length and keywords
                    if (line.Length == 14 && line.Contains("AX1") && line.Contains("AX2"))
                    {
                        
                        // Extract values without using Substring and Parse
                        float xValue, yValue;
                        if (float.TryParse(line.Substring(3, 4), out xValue) &&
                            float.TryParse(line.Substring(10, 4), out yValue))
                        {
                            xValue -= 511;
                            yValue -= 511;

                            // Add the new values to the queues
                            lastXValues.Add(xValue);
                            lastYValues.Add(yValue);

                            // track the timestamp of the last sample
                            lastSampleTime = Time.time;

                        }
                    }

                }

                // Remove the oldest values if the queues exceed 10 elements
                while (lastXValues.Count > 10)
                {
                    lastXValues.RemoveAt(0); // Remove oldest value if the list exceeds 10 elements
                }
                while (lastYValues.Count > 10)
                {
                    lastYValues.RemoveAt(0); // Remove oldest value if the list exceeds 10 elements
                }
            }
        }
        sp.Close();
        Debug.Log("Chiuso Arduino");
    }

    public bool isWorkingCorrectly() // return true if it's less than 1 sec since the last correct line was parsed
    {
        bool working = false;
        if ((Time.time - lastSampleTime) < 1) 
        {
            working = true;
        }
        return working;
    }

    public float getX()
    {
        float xavg = 0;
            if (lastXValues.Count > 0)
            {   try{
                    xavg = lastXValues.Average();}
                catch{}
            }
            if (Mathf.Abs(xavg) > JSdeadzone) { return xavg; }
            else { return 0f; }
    }

    public float getY()
    {
        float yavg = 0;
        if (lastYValues.Count > 0)
        {   
            try{
                yavg = lastYValues.Average();}
            catch{}
        }
        if (Mathf.Abs(yavg) > JSdeadzone) { return yavg; }
        else { return 0f; }
    
    }

     public void stopserial()
    {
        stopThread = true;
    }

     public void sendSerial(string cosa)
    {
        if (!sp.IsOpen)
        {
            sp.Open();
            Debug.Log("Riapertura seriale in scrittura");
        }
        if (sp.IsOpen)
        {
            sp.WriteLine(cosa);
        }
    }
}