using System;
using System.Collections.Generic;
using H2D.MediaPipe;
using UnityEngine;

namespace H2D
{
    public class PoseProvider : MonoBehaviour
    {
        public Action OnRecordingEnded;
        public Action OnNewPoseReady;
        
        private enum Mode
        {
            Live,
            Recording
        }
        
        private struct PoseRecordingFrame // Fetched from H2D project; do not edit!
        {
            public float Time;
            public float LandmarkLatency;
            public Vector3[] LandmarkPositions;
            public float[] LandmarkVisibilities;
            public Vector3 HeadPos;
            public Vector3 LeftHandPos;
            public Vector3 RightHandPos;
            public Quaternion HeadRot;
            public Quaternion LeftHandRot;
            public Quaternion RightHandRot;
            public Vector3 TrackerPos;
            public Quaternion TrackerRot;
        }

        [Header("Config")]
        [SerializeField] private Mode mode;
        [SerializeField] private TextAsset recording;
        [SerializeField] private bool loop;
        [SerializeField] private Quaternion simHmdCorrection = Quaternion.identity;
        [SerializeField] private Quaternion simLeftCtrlCorrection = Quaternion.identity;
        [SerializeField] private Quaternion simRightCtrlCorrection = Quaternion.identity;
        [Header("Wiring")]
        [SerializeField] private Transform simHmdTm;
        [SerializeField] private Transform liveHmdTm;
        [SerializeField] private Transform simLeftCtrlTm;
        [SerializeField] private Transform liveLeftCtrlTm;
        [SerializeField] private Transform simRightCtrlTm;
        [SerializeField] private Transform liveRightCtrlTm;
        [SerializeField] private Transform simHipTrackerTm;

        private readonly List<PoseRecordingFrame> _data = new();
        private float _time;
        private int _numFrames;
        private int _curIdx;
        private int _nextIdx;
        private Vector3[] _landmarksInCamera;

        private int NumLandmarks => PoseEstimationServer.NumLandmarks;
        
        public Vector3[] GetLandmarksInCamera()
        {
            return _landmarksInCamera;
        }

        public Pose GetHmdPose()
        {
            switch (mode)
            {
                case Mode.Live:
                    return new Pose(liveHmdTm.position, liveHmdTm.rotation);
                case Mode.Recording:
                    return new Pose(simHmdTm.position, simHmdTm.rotation);
                default:
                    throw new NotImplementedException("Unimplemented mode!");
            }
        }

        public Pose GetLeftCtrlPose()
        {
            switch (mode)
            {
                case Mode.Live:
                    return new Pose(liveLeftCtrlTm.position, liveLeftCtrlTm.rotation);
                case Mode.Recording:
                    return new Pose(simLeftCtrlTm.position, simLeftCtrlTm.rotation);
                default:
                    throw new NotImplementedException("Unimplemented mode!");
            }
        }

        public Pose GetRightCtrlPose()
        {
            switch (mode)
            {
                case Mode.Live:
                    return new Pose(liveRightCtrlTm.position, liveRightCtrlTm.rotation);
                case Mode.Recording:
                    return new Pose(simRightCtrlTm.position, simRightCtrlTm.rotation);
                default:
                    throw new NotImplementedException("Unimplemented mode!");
            }
        }

        public Pose GetTrackerPose()
        {
            switch (mode)
            {
                case Mode.Recording:
                    return new Pose(simHipTrackerTm.position, simHipTrackerTm.rotation);
                case Mode.Live:
                    throw new NotImplementedException("Tracker not supported in Live mode!");
                default:
                    throw new NotImplementedException("Unimplemented mode!");
            }
        }
        
        private void Start()
        {
            _landmarksInCamera = new Vector3[NumLandmarks];

            switch (mode)
            { 
                case Mode.Recording:
                    string[] lines = recording.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        _data.Add(JsonUtility.FromJson<PoseRecordingFrame>(line));
                    }
                    _numFrames = _data.Count;
                    _nextIdx = 1;
                    _time = 0f;
                    break;
                
                case Mode.Live:
                    PoseEstimationServer.Instance.OnPoseReceivedEvent += OnPoseReceived;
                    break;
                
                default:
                    Debug.LogError("Unimplemented mode!");
                    break;
            }
        }

        private void Update()
        {
            if (mode == Mode.Recording)
            {
                if (_nextIdx == _numFrames)
                {
                    if (OnRecordingEnded != null)
                    {
                        OnRecordingEnded();
                    }
                    if (!loop) { return; }
                    _curIdx = 0;
                    _nextIdx = 1;
                    _time = 0f;
                }
                
                if (_time > _data[_nextIdx].Time)
                {
                    _nextIdx++;
                }

                if (_nextIdx > _curIdx)
                {
                    var curData = _data[_nextIdx - 1];
                    Array.Copy(curData.LandmarkPositions, _landmarksInCamera, NumLandmarks);

                    _curIdx = _nextIdx;
                    
                    simHmdTm.position = curData.HeadPos;
                    simHmdTm.rotation = simHmdCorrection * curData.HeadRot;
                    simLeftCtrlTm.position = curData.LeftHandPos;
                    simLeftCtrlTm.rotation = curData.LeftHandRot * simLeftCtrlCorrection;
                    simRightCtrlTm.position = curData.RightHandPos;
                    simRightCtrlTm.rotation = curData.RightHandRot * simRightCtrlCorrection;
                    simHipTrackerTm.position = curData.TrackerPos;
                    simHipTrackerTm.rotation = curData.TrackerRot;
                    
                    if (OnNewPoseReady != null)
                    {
                        OnNewPoseReady();
                    }
                }
                
                _time += Time.deltaTime;
            }
        }

        private void OnPoseReceived() // Only to be called when "mode == Mode.Live"
        {
            for (int i = 0; i < NumLandmarks; i++)
            {
                _landmarksInCamera[i] = PoseEstimationServer.Instance.GetLandmarkPosInCamera(i);
            }
            
            if (OnNewPoseReady != null)
            {
                OnNewPoseReady();
            }
        }
    }
}