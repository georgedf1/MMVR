using System;
using System.IO;
using UnityEditor.Rendering;
using UnityEngine;

namespace H2D
{
    public class HipMetrics : MonoBehaviour
    {
        [SerializeField] private bool write;
        [SerializeField] private string writePath;
        [SerializeField] private VRCharacterController vrCharCtrl;
        [SerializeField] private PoseProvider poseProvider;
    
        private StreamWriter _writer;
        private bool _isRecording;
    
        private void Start()
        {
            poseProvider.OnRecordingEnded += RecordingEnded;
            poseProvider.OnNewPoseReady += NewPoseReady;
            if (write)
            {
                _writer = new StreamWriter(writePath);
            }
            _isRecording = true;
        }

        private void RecordingEnded()
        {
            _isRecording = false;
            poseProvider.OnNewPoseReady -= NewPoseReady;
            poseProvider.OnRecordingEnded -= RecordingEnded;
            if (write)
            {
                _writer.Close();
            }
        }

        private void OnDestroy()
        {
            if (write)
            {
                _writer.Close();
            }
        }

        private void NewPoseReady()
        {
            if (_isRecording)
            {
                Vector3 charHipDir = vrCharCtrl.GetHipDirection() * Vector3.forward;
                charHipDir.y = 0f;
                
                Vector3 gtHipDir = poseProvider.GetTrackerPose().rotation * Vector3.forward;
                gtHipDir.y = 0f;

                float absAngle = Math.Abs(Vector3.Angle(charHipDir, gtHipDir));
                
                Debug.Log("absAngle; " + absAngle);
                
                if (write)
                {
                    _writer.WriteLine(Time.time);
                    _writer.WriteLine(absAngle);
                }
            }
        }
    }
}
