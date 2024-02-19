using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public abstract class BaseListener
    {
        public bool IsListening { get; private set; }

        protected SubscriptionsController subsCtrl;

        public BaseListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
        }

        ~BaseListener()
        {
            if (subsCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            if (!subsCtrl.IsConnected)
            {
                Debug.LogWarning($"{this.GetType().Name}:PupilLab No connected. Waiting for connection.");
                subsCtrl.requestCtrl.OnConnected += EnableOnConnect;
                return;
            }

            if (IsListening)
            {
                Debug.Log("PupilLab Already listening.");
                return;
            }


            CustomEnable();

            IsListening = true;
        }

        public void EnableOnConnect()
        {
            subsCtrl.requestCtrl.OnConnected -= EnableOnConnect;
            Enable();
        }

        protected abstract void CustomEnable();

        public void Disable()
        {
            if (!subsCtrl.IsConnected)
            {
                Debug.LogWarning("PupilLab Not connected!");
                IsListening = false;
                return;
            }

            if (!IsListening)
            {
                Debug.Log("PupilLab Not running.");
                return;
            }

            CustomDisable();
            IsListening = false;
        }

        protected abstract void CustomDisable();
    }
}
