// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using System.Collections;
// ADDED BY EDO TO SAVE RECORD TIME/FRAME
using System.IO;
using System;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public sealed class CameraCapture : MonoBehaviour
    {
        #region Public properties

        [SerializeField] int _width = 1920;

        public int width {
            get { return _width; }
            set { _width = value; }
        }

        [SerializeField] int _height = 1080;

        public int height {
            get { return _height; }
            set { _height = value; }
        }

        [SerializeField] FFmpegPreset _preset;

        public FFmpegPreset preset {
            get { return _preset; }
            set { _preset = value; }
        }

        [SerializeField] float _frameRate = 60;

        public float frameRate {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        #endregion

        #region Private members

        FFmpegSession _session;
        RenderTexture _tempRT;
        GameObject _blitter;

        RenderTextureFormat GetTargetFormat(Camera camera)
        {
            return camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        }

        int GetAntiAliasingLevel(Camera camera)
        {
            return camera.allowMSAA ? QualitySettings.antiAliasing : 1;
        }

        #endregion

        #region Time-keeping variables

        int _frameCount;
        float _startTime;
        int _frameDropCount;

        // ADDED BY EDO
        int _frameDelay;

        float FrameTime {
            get { return _startTime + (_frameCount - 0.5f) / _frameRate; }
        }

        void WarnFrameDrop()
        {
            if (++_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        #endregion

        #region ADDED BY EDO: RECORDING FRAMES AND TIMESTAMPS

        GameObject experiment;
        private StreamWriter _streamWriter;
        private bool _isStreamWriterInitialized = false;
        public string path_to_data_RecorderFrames;

        // Movie path
        public string path_to_video;

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _width = Mathf.Max(8, _width);
            _height = Mathf.Max(8, _height);
        }

        void OnDisable()
        {
            if (_session != null)
            {
                // ADDED BY EDO: Grab video output path
                path_to_video = FFmpegSession.path_to_video;

                // Close and dispose the FFmpeg session.
                _session.Close();
                _session.Dispose();
                _session = null;
            }

            if (_tempRT != null)
            {
                // Dispose the frame texture.
                GetComponent<Camera>().targetTexture = null;
                Destroy(_tempRT);
                _tempRT = null;
            }

            if (_blitter != null)
            {
                // Destroy the blitter game object.
                Destroy(_blitter);
                _blitter = null;
            }

            #region ADDED BY EDO: Save/Delete depending on user choice

            // VIDEO

            if (!Saver.wants2saveVideos)
            {
                File.Delete(path_to_video);
            }

            // CSV of recorder frames
            if (_isStreamWriterInitialized)
            {
                _streamWriter.Close();

                // Save or delete csv file depending on user prompt
                if (!Saver.wants2saveVideos)
                {
                    File.Delete(path_to_data_RecorderFrames);
                }
            }

            #endregion

        }

        IEnumerator Start()
        {
            // Sync with FFmpeg pipe thread at the end of every frame.
            for (var eof = new WaitForEndOfFrame(); ;)
            {
                yield return eof;
                _session?.CompletePushFrames();
            }
        }

        void Update()
        {
            var camera = GetComponent<Camera>();

            #region ADDED BY EDO: SAVE RECORDER FRAMES

            GameObject experiment = GameObject.Find("Experiment");
            int main_frame_num = experiment.GetComponent<MainTask>().frame_number;
            int last_state = experiment.GetComponent<MainTask>().last_state;
            long main_start_time = experiment.GetComponent<MainTask>().starttime;
            int reward_count = experiment.GetComponent<Ardu>().reward_counter;

            // Check if the StreamWriter is not initialized
            if (!_isStreamWriterInitialized)
            {

                // Get path_to_data and lastIDFromDB from the Saver
                string path_to_MEF = experiment.GetComponent<Saver>().path_to_MEF;
                int lastIDFromDB = experiment.GetComponent<Saver>().lastIDFromDB;

                // Check if path_to_data and lastIDFromDB are not null or zero
                if (!string.IsNullOrEmpty(path_to_MEF) && lastIDFromDB != 0)
                {
                    path_to_data_RecorderFrames = Path.Combine(path_to_MEF, "VIDEO",
                        (DateTime.Now.ToString("yyyy_MM_dd") + "_ID" + (lastIDFromDB + 1).ToString() + $"_{camera.tag}" + "_recorderFrames.csv"));
                    _streamWriter = new StreamWriter(path_to_data_RecorderFrames);
                    _streamWriter.WriteLine("Timestamp,Frame_time,Rec_Frame,Droppings,Dropped_frames,Reward_count,Current_state");

                    // Set the flag to true after initializing the StreamWriter
                    _isStreamWriterInitialized = true;
                }
            }

            #endregion

            // Lazy initialization
            if (_session == null)
            {
                // Give a newly created temporary render texture to the camera
                // if it's set to render to a screen. Also create a blitter
                // object to keep frames presented on the screen.
                if (camera.targetTexture == null)
                {
                    _tempRT = new RenderTexture(_width, _height, 24, GetTargetFormat(camera));
                    _tempRT.antiAliasing = GetAntiAliasingLevel(camera);
                    camera.targetTexture = _tempRT;
                    _blitter = Blitter.CreateInstance(camera);
                }

                // Start an FFmpeg session.
                _session = FFmpegSession.Create(
                    gameObject.name,
                    camera.targetTexture.width,
                    camera.targetTexture.height,
                    _frameRate, preset
                );

                _startTime = Time.time;
                _frameCount = 0;
                _frameDropCount = 0;

                // ADDED BY EDO
                _frameDelay = 0;
            }

            var gap = Time.time - FrameTime;
            var delta = 1 / _frameRate;

            if (gap < 0)
            {
                // Update without frame data.
                _session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                _session.PushFrame(camera.targetTexture);
                _frameCount++;

                // ADDED BY EDO: After pushing the frame to FFmpeg, write the details to the CSV file.
                addRecorderFrameRow(main_start_time, reward_count, last_state);

            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                _session.PushFrame(camera.targetTexture);
                _frameCount += 1;

                // ADDED BY EDO: After pushing the frame to FFmpeg, write the details to the CSV file.
                addRecorderFrameRow(main_start_time, reward_count, last_state);

                _session.PushFrame(camera.targetTexture);
                _frameCount += 1;

                // ADDED BY EDO: After pushing the frame to FFmpeg, write the details to the CSV file.
                addRecorderFrameRow(main_start_time, reward_count, last_state);

            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                _session.PushFrame(camera.targetTexture);

                // Compensate the time delay.
                _frameCount += Mathf.FloorToInt(gap * _frameRate);

                // ADDED BY EDO: After pushing the frame to FFmpeg, write the details to the CSV file

                _frameDelay += Mathf.FloorToInt(gap * _frameRate);

                addRecorderFrameRow(main_start_time, reward_count, last_state);
            }

        }

        #endregion


        #region ADDED BY EDO: Methods

        private void addRecorderFrameRow(long main_start_time, int reward_count, int last_state)
        {
            // ADDED BY EDO: After pushing the frame to FFmpeg, write the details to the CSV file.
            if (_isStreamWriterInitialized)
            {
                long rec_time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
                _streamWriter.WriteLine($"{(rec_time - main_start_time)},{FrameTime},{_frameCount},{_frameDropCount},{_frameDelay},{reward_count},{last_state}");
            }
        }

        #endregion
    }
}
