using UnityEngine;
using UnityEditor;
using System;


public class Ardu : MonoBehaviour
{
    arduino ardu;
    public bool ardu_working = false;
    public bool testing = false;
    public string COM = "COM10";
    public float ax1 = 0;
    public float ax2 = 0;
    public int reward_counter;

    private bool ans = false;
    //public int dead_zone;


    void Start() 
    {
        if (!testing)
        {
            try 
            {
                ardu = new arduino(COM, 57600, 80);
                ardu_working = ardu.isWorkingCorrectly();
            }
            catch
            {
                ans = EditorUtility.DisplayDialog("Arduino Connection Error", "Unable to read correctly from the Arduino", 
                    "Go ahead in testing mode (no arduino)", "Exit game");
                // You can add a delay here if you want
                if (ans) { testing = true; }
                else { QuitGame(); }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (!testing)
        {
            ardu_working = ardu.isWorkingCorrectly();
            if (ardu_working)
            {
                ax1 = ardu.getX();
                ax2 = -ardu.getY();
            }
            else
            {
                ans = EditorUtility.DisplayDialog("Arduino Connection Error", "Unable to read correctly from the Arduino",
                                                        "Go ahead in testing mode (no arduino)", "Exit game");
                // You can add a delay here if you want
                if (ans) { testing = true; }
                else { QuitGame(); }
            }
        }

        if (Input.GetKey("escape"))
        {
            if (!testing && ardu_working)
            {
                ardu.stopserial();
            }

            QuitGame();
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }

    public void SendReward(int rewardTime)
    {
        if (ardu_working )
        {
            ardu.sendSerial("R" + rewardTime.ToString());
           
        }
        Debug.Log("R" + rewardTime.ToString());
        reward_counter += 1;
    }

    // TRIGGER 1 BNC 
    public void SendStartRecordingOE()
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("TRIG1_ON");
            Debug.Log("REC OE ON");
        }
    }
    // TRIGGER 1 BNC
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
