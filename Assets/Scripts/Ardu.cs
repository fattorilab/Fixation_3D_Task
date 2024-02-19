//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
//using System.IO;

public class Ardu : MonoBehaviour
{
    arduino ardu;
    bool ardu_working = false;
    public bool testing;
    public string COM = "COM10";
    //public string rewardTime = "500";
    public float ax1 = 0;
    public float ax2 = 0;
    // Start is called before the first frame update

    public int reward_counter;
    //public int dead_zone;

    void Start()
    {
        try
        {
            ardu = new arduino(COM, 57600, 80);
            ardu_working = true;
        }
        catch // (IOException ioex)
        {
            //Debug.Log($"{Time.frameCount}. exception: {ioex.Message}");
            Debug.Log("Please connect all cables!");
            ardu_working = false;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (ardu_working)
        {
            ax1 = ardu.getX();
            ax2 = -ardu.getY();
            //Debug.Log("X " + ax1 + " Y " + ax2);
        }

        if (Input.GetKey("escape"))
        {
            if (ardu_working)
            {
                ardu.stopserial();
            }
            Application.Quit();
        }
    }

    public void SendReward(int rewardTime)
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("R" + rewardTime.ToString());
           
        }
        Debug.Log("R" + rewardTime.ToString());
        reward_counter += 1;
    }

    // TRIGGER 1 BNC 8(T1)
    public void SendStartRecordingOE()
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("TRIG1_ON");
            Debug.Log("REC OE ON");
        }
    }
    // TRIGGER 1 BNC 8(T1)
    public void SendStopRecordingOE()
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("TRIG1_OFF");
            Debug.Log("REC OE OFF");
        }
    }

    public void SendPupilLabData(float RightPupilPixel_x, float RightPupilPixel_y, float LeftPupilPixel_x, float LeftPupilPixel_y)
    {
        if (ardu_working && !testing)
        {
           ardu.sendSerial("Rx" + RightPupilPixel_x.ToString() + "Ry" + RightPupilPixel_y.ToString() + "Lx" + LeftPupilPixel_x.ToString() + "Ly" + LeftPupilPixel_y.ToString());
            
        }
    }
}
