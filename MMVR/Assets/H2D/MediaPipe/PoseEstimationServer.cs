using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace H2D.MediaPipe
{
    public enum LandmarkType : byte
    {
        Nose = 0,
        LeftEyeInner = 1,
        LeftEye = 2,
        LeftEyeOuter = 3,
        RightEyeInner = 4,
        RightEye = 5,
        RightEyeOuter = 6,
        LeftEar = 7,
        RightEar = 8,
        MouthLeft = 9,
        MouthRight = 10,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftElbow = 13,
        RightElbow = 14,
        LeftWrist = 15,
        RightWrist = 16,
        LeftPinky = 17,
        RightPinky = 18,
        LeftIndex = 19,
        RightIndex = 20,
        LeftThumb = 21,
        RightThumb = 22,
        LeftHip = 23,
        RightHip = 24,
        LeftKnee = 25,
        RightKnee = 26,
        LeftAnkle = 27,
        RightAnkle = 28,
        LeftHeel = 29,
        RightHeel = 30,
        LeftFootIndex = 31,
        RightFootIndex = 32,
    }
    
    public class PoseEstimationServer : MonoBehaviour
    {
        public static PoseEstimationServer Instance => _instance;
        public bool IsClientConnected => _client is { Connected: true };
        public const int NumLandmarks = 33;
        
        [Serializable]
        private struct JsonLandmarkData
        {
            public JsonLandmark[] landmarks;
        }

        [Serializable]
        private struct JsonLandmark
        {
            public float x;
            public float y;
            public float z;
            public float visibility;
        }
        
        private static PoseEstimationServer _instance;
        
        [SerializeField] private int pollWaitTime = 1000; // microseconds 
        [SerializeField] private int port = 34151;
        
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _networkStream;
        private BinaryReader _reader;
        private Vector3[] _landmarksInCamera;
        private float[] _visibilities;
        private float _latency;

        public delegate void OnConnect();
        public delegate void OnDisconnect();
        public delegate void OnPoseReceived();
        public event OnConnect OnConnectEvent;
        public event OnDisconnect OnDisconnectEvent;
        public event OnPoseReceived OnPoseReceivedEvent;

        public Vector3 GetLandmarkPosInCamera(LandmarkType type)
        {
            return _landmarksInCamera[(int)type];
        }

        public Vector3 GetLandmarkPosInCamera(int i)
        {
            return _landmarksInCamera[i];
        }

        public float GetLandmarkVisibility(LandmarkType type)
        {
            return _visibilities[(int)type];
        }

        public float GetLandmarkVisibility(int i)
        {
            return _visibilities[i];
        }

        public float GetLatency()
        {
            return _latency;
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Application.runInBackground = true;
                DontDestroyOnLoad(this);
            }
            else
            {
                Debug.LogError($"Found multiple {nameof(PoseEstimationServer)} scripts, deleting this");
                Destroy(this);
                return;
            }
            
            _landmarksInCamera = new Vector3[NumLandmarks];
            _visibilities = new float[NumLandmarks];
            
            Application.runInBackground = true; // Required to keep socket alive?
            
#if UNITY_EDITOR
            EditorApplication.quitting += OnApplicationQuit;
#endif         
            
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
        }

        private void OnApplicationQuit()
        {
            if (_listener != null)
            {
                _listener.Stop();
            }
        }

        private void Update()
        {
            if (!IsClientConnected)
            {
                if (_listener.Pending())
                {
                    _client = _listener.AcceptTcpClient();
                    _networkStream = _client.GetStream();
                    _reader = new BinaryReader(_networkStream);
                    if (OnConnectEvent != null) OnConnectEvent();
                    Debug.Log("PoseEstimationServer Connected");
                }
                else
                {
                    return;
                }
            }
            
            // Check connected before reading (by polling)
            if (_client.Client.Poll(pollWaitTime, SelectMode.SelectRead) 
                && _client.Available == 0)
            {
                if (OnDisconnectEvent != null) OnDisconnectEvent();
                Debug.Log("PoseEstimationServer Disconnected");
                _client = default;
                return;
            }
            
            if (!_networkStream.DataAvailable) return;

            // --- READ ---
            float latency = _reader.ReadSingle();
            int bytesLen = _reader.ReadInt32();
            byte[] jsonBytes = _reader.ReadBytes(bytesLen);
            string jsonStr = Encoding.UTF8.GetString(jsonBytes);
            JsonLandmarkData jsonLandmarkData = JsonUtility.FromJson<JsonLandmarkData>(jsonStr);

            // Store
            _latency = latency;
            JsonLandmark[] landmarks = jsonLandmarkData.landmarks;
            for (int i = 0; i < landmarks.Length; i++)
            {
                _landmarksInCamera[i] = new Vector3(landmarks[i].x, landmarks[i].y, landmarks[i].z);
                _visibilities[i] = landmarks[i].visibility;
            }

            if (OnPoseReceivedEvent != null) OnPoseReceivedEvent();
        }
    }
}
