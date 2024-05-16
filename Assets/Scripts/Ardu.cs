using UnityEngine;
using UnityEditor;
using System;

public class Ardu : MonoBehaviour
{
    #region Variables Declaration

    // GameObject
    arduino ardu;

    // Connection bools
    private bool ans = false;
    bool ardu_working = true;
    bool testing = false;

    // Port
    public string COM = "COM10";

    // Axes
    public float ax1 = float.NaN;
    public float ax2 = float.NaN;

    // Reward counter
    public int reward_counter;

    #endregion

    void Start()
    {
        #region Connect to Arduino

        // If not testing mode
        if (!testing)
        {
            try
            {
                // Establish connection
                ardu = new arduino(COM, 57600, 80);
            }
            catch
            {
                // Notify
                ans = EditorUtility.DisplayDialog("Arduino Connection Error", "Unable to read correctly from the Arduino",
                    "Go ahead in testing mode (no arduino)", "Exit game");

                // Set testing mode or quit game
                if (ans) { testing = true; ardu_working = false; }
                else { QuitGame(); }
            }
        }

        #endregion
    }

    void Update()
    {
        // If not testing mode
        if (!testing)
        {
            // Get coordinates from Arduino
            ax1 = ardu.getX();
            ax2 = -ardu.getY();
        }
        else
        {
            ax1 = float.NaN;
            ax2 = float.NaN;
        }

        // Manual quit
        if (Input.GetKey("escape"))
        {
            if (!testing && ardu_working)
            {
                ardu.stopserial();
            }

            QuitGame();
        }
    }


    #region Methods 

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    // Send signal to deliver reward
    public void SendReward(int rewardTime)
    {
        if (ardu_working)
        {
            ardu.sendSerial("R" + rewardTime.ToString());
            Debug.Log("R" + rewardTime.ToString());
        }
        reward_counter += 1;
    }

    // TRIGGER 1 BNC - Activate OpenEphys (neural recording)
    public void SendStartRecordingOE()
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("TRIG1_ON");
            Debug.Log("REC OE ON");
        }
    }
    // TRIGGER 1 BNC - Deactivate OpenEphys (neural recording)
    public void SendStopRecordingOE()
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("TRIG1_OFF");
            Debug.Log("REC OE OFF");
        }
    }

    // Send eye data to arduino 
    public void SendPupilLabData(float RightPupilPixel_x, float RightPupilPixel_y, float LeftPupilPixel_x, float LeftPupilPixel_y)
    {
        if (ardu_working && !testing)
        {
            ardu.sendSerial("Rx" + RightPupilPixel_x.ToString() + "Ry" + RightPupilPixel_y.ToString() + "Lx" + LeftPupilPixel_x.ToString() + "Ly" + LeftPupilPixel_y.ToString());

        }
    }

    #endregion
}
