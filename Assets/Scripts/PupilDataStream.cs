using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs
{
    public class PupilDataStream : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public Vector2 CenterRightPupilNorm;
        public Vector2 CenterRightPupilPx;
        public Vector2 AxisRightPupilPx;
        public float AngleRightPupil; // degrees
        public float DiameterRight;
        public Vector2 CenterLeftPupilNorm;
        public Vector2 CenterLeftPupilPx;
        public Vector2 AxisLeftPupilPx;
        public float AngleLeftPupil; // degrees
        public float DiameterLeft;
        public double PupilTimeStamps;
        private PupilListener listener;
       
        

        void OnEnable()
        {
            if (listener == null)
            {
                listener = new PupilListener(subsCtrl);
            }

            listener.Enable();
            listener.OnReceivePupilData += ReceivePupilData;
        }

        void OnDisable()
        {
            listener.Disable();
            listener.OnReceivePupilData -= ReceivePupilData;
        }


        void Update()
        {
           
        }

        void ReceivePupilData(PupilData pupilData)
        {    
            if (pupilData.EyeIdx == 0)
            {
                CenterRightPupilNorm = pupilData.NormPos;
                CenterRightPupilPx = pupilData.Ellipse.Center;
                AxisRightPupilPx = pupilData.Ellipse.Axis;
                AngleRightPupil = pupilData.Ellipse.Angle;
                DiameterRight = pupilData.Diameter3d;
            }
            if (pupilData.EyeIdx == 1)
            {
                CenterLeftPupilNorm = pupilData.NormPos;
                CenterLeftPupilPx = pupilData.Ellipse.Center;
                AxisLeftPupilPx = pupilData.Ellipse.Axis;
                AngleLeftPupil = pupilData.Ellipse.Angle;
                DiameterLeft = pupilData.Diameter3d;
            }
            PupilTimeStamps = pupilData.PupilTimestamp;
        }
    }
}