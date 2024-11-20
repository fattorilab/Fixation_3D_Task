using System;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Threading;
using System.Text;
using System.Linq;

public class arduino
{
    #region Variables Declaration

    // Declare a SerialPort object to communicate with the Arduino
    SerialPort sp;

    // List to store the last X values received from the Arduino
    List<float> lastXValues = new List<float>();

    // List to store the last Y values received from the Arduino
    List<float> lastYValues = new List<float>();

    // The speed (in baud rate) of the COM port communication
    int COMspeed;

    // The COM port to use for communication
    string COM;

    // The deadzone for the joystick
    public int JSdeadzone;

    // A separate thread to handle the joystick sampling
    Thread thread;

    // A flag to indicate when to stop the thread
    bool stopThread;

    // The time of the last sample
    float lastSampleTime;

    #endregion

    public arduino(string _COM, int _COMspeed, int _JSdeadzone)
    {
        COMspeed = _COMspeed; // Set the COM port speed
        COM = _COM; // Set the COM port to use
        JSdeadzone = _JSdeadzone; // Set the joystick deadzone
        sp = new SerialPort("\\\\.\\" + COM, COMspeed); // Initialize the SerialPort object
        lastSampleTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(); // Record the current time
        if (!sp.IsOpen) // If the SerialPort is not open...
        {
            Debug.Log("Apertura " + COM + ", baud " + COMspeed); // Log the COM port and baud rate
            sp.Open(); // Open the SerialPort
            if (sp.IsOpen) { Debug.Log("Seriale comunicazione Arduino aperto correttamente"); } // If the SerialPort opened successfully, log a success message
        }
        thread = new Thread(JoySample); // Initialize the thread with the JoySample method
        thread.Start(); // Start the thread
    }

    /**
     * The `JoySample` function is responsible for reading and processing data from a serial port.
     * It continuously reads data until `stopThread` is set to true.
     * The function is designed to work with a specific data format that includes "AX1" and "AX2" keywords.
     * The data read from the serial port is processed and stored in two queues: `lastXValues` and `lastYValues`.
     * The function also tracks the timestamp of the last sample read.
     * If the queues exceed 10 elements, the oldest values are removed.
     */
    public void JoySample()
    {
        // Buffer to store the incoming data
        StringBuilder buffer = new StringBuilder();

        // Continue reading and processing data until stopThread is set to true
        while (!stopThread)
        {
            // If the serial port is not open, open it
            if (!sp.IsOpen)
            {
                sp.Open();
                Debug.Log("Riapertura seriale in lettura"); // Log message indicating the serial port is reopened for reading
            }

            // If the serial port is open, read and process the data
            if (sp.IsOpen)
            {
                // Read the existing data from the serial port and append it to the buffer
                string receivedData = sp.ReadExisting();
                buffer.Append(receivedData);

                // Process data in the buffer
                int newLineIndex;
                while ((newLineIndex = buffer.ToString().IndexOf('\n')) >= 0) // Find the newline character
                {
                    // Extract the line and remove it from the buffer
                    string line = buffer.ToString(0, newLineIndex - 1);
                    buffer.Remove(0, newLineIndex + 1);

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

                            // Track the timestamp of the last sample
                            lastSampleTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
        // Close the serial port when done
        sp.Close();
        Debug.Log("Arduino port closed."); // Log message indicating the Arduino port is closed
    }

    // Checks if the system is working correctly based on the time of the last sample
    public bool isWorkingCorrectly()
    {
        bool working = false;
        // If the time since the last sample is less than 1 second, set working to true
        if ((System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastSampleTime) < 1000)
        {
            working = true;
        }
        // Return the status of working
        return working;
    }

    // Returns the average of lastXValues if it's greater than JSdeadzone, otherwise returns 0
    public float getX()
    {
        float xavg = 0;
        // If there are values in lastXValues
        if (lastXValues.Count > 0)
        {
            try
            {
                // Calculate the average of lastXValues
                xavg = lastXValues.Average();
            }
            catch { }
        }
        // If the absolute value of the average is greater than JSdeadzone, return the average
        if (Mathf.Abs(xavg) > JSdeadzone) { return xavg; }
        // Otherwise, return 0
        else { return 0f; }
    }

    // Returns the average of lastYValues if it's greater than JSdeadzone, otherwise returns 0
    public float getY()
    {
        float yavg = 0;
        // If there are values in lastYValues
        if (lastYValues.Count > 0)
        {
            try
            {
                // Calculate the average of lastYValues
                yavg = lastYValues.Average();
            }
            catch { }
        }
        // If the absolute value of the average is greater than JSdeadzone, return the average
        if (Mathf.Abs(yavg) > JSdeadzone) { return yavg; }
        // Otherwise, return 0
        else { return 0f; }
    }

    // Stops the running thread by setting stopThread to true
    public void stopserial()
    {
        stopThread = true;
    }

    // Sends a string over a serial port
    public void sendSerial(string s)
    {
        // If the serial port is not open, open the port and log a message
        if (!sp.IsOpen)
        {
            sp.Open();
            Debug.Log("Riapertura seriale in scrittura");
        }

        // If the port is open, write the string to the port
        if (sp.IsOpen)
        {
            sp.WriteLine(s);
        }
    }
}