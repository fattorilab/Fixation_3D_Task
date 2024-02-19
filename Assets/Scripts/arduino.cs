using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Threading;


public class arduino
{
    SerialPort sp;
    Queue Xque = new Queue();
    Queue Yque = new Queue();
    Queue stop = new Queue();
    int COMspeed;
    string COM;
    int JSdeadzone;
    Thread thread;
    string serialread = "";

    


    public arduino(string _COM, int _COMspeed, int _JSdeadzone){
        COMspeed = _COMspeed;
        COM = _COM;
        JSdeadzone = _JSdeadzone;

        sp = new SerialPort("\\\\.\\" + COM, COMspeed);
        if (!sp.IsOpen)
        {
            Debug.Log("Opening " + COM + ", baud " + COMspeed);
            sp.Open();
            sp.ReadTimeout = 100;
            sp.Handshake = Handshake.None;
            if (sp.IsOpen) { Debug.Log("Open"); }
        }
        thread = new Thread (JoySample);
        thread.Start();
    }


    public void JoySample(){
        int vai = 1;
        while (vai == 1){
            if (!sp.IsOpen)
            {
                sp.Open();
                Debug.Log("riapro seriale in lettura");
            }
            if (sp.IsOpen)
            {
                // keep queues small
                while (Xque.Count > 10) {Xque.Dequeue();}
                while (Yque.Count > 10) {Yque.Dequeue();}

                //sp.DiscardInBuffer();
                try
                {
                    serialread = sp.ReadLine();    
                }
                catch (TimeoutException)
                {
                    Debug.Log("timeout lettura seriale");
                }
                //Debug.Log(serialread);

                if (serialread.Length == 14){
                    if (serialread.Contains("AX1") && serialread.Contains("AX2"))
                    {
                        Xque.Enqueue(float.Parse(serialread.Substring(3, 4))-511 );
                        Yque.Enqueue(float.Parse(serialread.Substring(10, 4))-511 );
                    }
                }

            }

            if (stop.Count >= 1){
                vai = 0;
                sp.Close();
            }

            //Thread.Sleep(50);

        }
    }

    public float getX(){
        float xavg = 0;
        if (Xque.Count != 0)
        {
            try
            {
                foreach (float elem in Xque.ToArray())
                {
                    xavg += elem;
                }
            }
            catch //(Exception e)
            {
                //Debug.Log(e);
            }
        }  
        if (Mathf.Abs(xavg / Xque.Count) > JSdeadzone){return xavg / Xque.Count;}
        else {return 0f;}
        
    }

    public float getY(){
        float yavg = 0;
        if (Yque.Count != 0)
        {
            try
            {
                foreach (float elem in Yque.ToArray())
                {
                    yavg += elem;
                }
            }
            catch //(Exception e)
            {
                //Debug.Log(e);
            }
        }
        if (Mathf.Abs(yavg / Yque.Count) > JSdeadzone){return yavg / Yque.Count;}
        else {return 0f;}
    }

    public void stopserial (){
        stop.Enqueue(1);
    }

    public void sendSerial(string cosa){
        if (!sp.IsOpen)
        {
            sp.Open();
            Debug.Log("riapro seriale in scrittura");
        }
        if (sp.IsOpen)
        {
            sp.WriteLine(cosa);
        }
    }

}
