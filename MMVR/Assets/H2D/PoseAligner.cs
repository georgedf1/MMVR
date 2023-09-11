using System;
using UnityEngine;
using H2D.MediaPipe;
using H2D.EigenUnity;

namespace H2D
{
    public class PoseAligner : MonoBehaviour
    {
        [SerializeField] private float poseScaleFactor = 1f;
        [SerializeField] private Quaternion poseCameraRotation;
        [SerializeField] private Vector3 hmdToHeadTrackerOffset;
        [SerializeField] private Vector3 leftCtrlToLeftHandOffset;
        [SerializeField] private Vector3 rightCtrlToRightHandOffset;

        [Header("Wiring")]
        [SerializeField] private Transform hmdTm;
        [SerializeField] private Transform leftHandTm;
        [SerializeField] private Transform rightHandTm;
        [SerializeField] private PoseDebugSkeleton originalSkeleton;
        [SerializeField] private PoseDebugSkeleton alignedSkeleton;

        private Vector3[] _originalLandmarkPositions;
        private Vector3[] _alignedLandmarkPositions;

        public struct AlignmentProblem
        {
            public Vector3[] LandmarksInCameraToAlign;
            public Quaternion PoseCameraRotation;
            public float PoseScaleFactor;
            public Pose HmdPose;
            public Pose LeftHandPose;
            public Pose RightHandPose;
            public Vector3 HmdToHeadTrackerOffset;
            public Vector3 LeftCtrlToLeftHandOffset;
            public Vector3 RightCtrlToRightHandOffset;
        }

        public static void Align(out Quaternion rotation, out Vector3 translation, AlignmentProblem alignmentProblem)
        {
            Vector3[] landmarks = alignmentProblem.LandmarksInCameraToAlign;
            int numLandmarks = PoseEstimationServer.NumLandmarks;

            if (alignmentProblem.PoseScaleFactor == 0f)
            {
                alignmentProblem.PoseScaleFactor = 1f;
            }
            
            // Fix chirality and rotation
            for (int i = 0; i < numLandmarks; i++)
            {
                Vector3 pos = alignmentProblem.PoseScaleFactor * landmarks[i];
                pos.y = -pos.y;
                pos = alignmentProblem.PoseCameraRotation * pos;
                landmarks[i] = pos;
            }
            
            // Shift corrected landmark positions so lowest point is on ground.
            {
                float lowestY = float.MaxValue;
                for (int i = 0; i < landmarks.Length; i++)
                {
                    float y = landmarks[i].y;
                    if (y < lowestY)
                    {
                        lowestY = y;
                    }
                }
                for (int i = 0; i < landmarks.Length; i++)
                {
                    landmarks[i].y -= lowestY;
                }
            }
            
            Vector3[] srcPositions =
            {
                landmarks[(int)LandmarkType.Nose],
                landmarks[(int)LandmarkType.LeftWrist],
                landmarks[(int)LandmarkType.RightWrist]
            };
            
            Vector3[] refPositions =
            {
                alignmentProblem.HmdPose.position + alignmentProblem.HmdPose.rotation * alignmentProblem.HmdToHeadTrackerOffset,
                alignmentProblem.LeftHandPose.position + alignmentProblem.LeftHandPose.rotation * alignmentProblem.LeftCtrlToLeftHandOffset,
                alignmentProblem.RightHandPose.position + alignmentProblem.RightHandPose.rotation * alignmentProblem.RightCtrlToRightHandOffset
            };

            Vector3 srcCentroid = (srcPositions[0] + srcPositions[1] + srcPositions[2]) / 3;
            Vector3 refCentroid = (refPositions[0] + refPositions[1] + refPositions[2]) / 3;

            // Centre before Kabsch
            for (int i = 0; i < 3; i++) 
            {
                srcPositions[i] -= srcCentroid;
                refPositions[i] -= refCentroid;
            }

            rotation = Kabsch.Compute(srcPositions, refPositions);
            
            // Yaw only
            rotation.ToAngleAxis(out float angle, out Vector3 axis);
            axis.x = 0f;
            axis.z = 0f;
            axis = axis.normalized;
            rotation = Quaternion.AngleAxis(angle, axis);

            refCentroid.y = srcCentroid.y; // no vertical translation permitted
            translation = refCentroid - srcCentroid;

            for (int i = 0; i < PoseEstimationServer.NumLandmarks; i++) 
            {
                landmarks[i] = refCentroid + rotation * (landmarks[i] - srcCentroid);
            }
        }

        public Vector3[] GetAlignedLandmarkPositions()
        {
            return _alignedLandmarkPositions;
        }
        
        void Start()
        {
            _originalLandmarkPositions = new Vector3[PoseEstimationServer.NumLandmarks];
            _alignedLandmarkPositions = new Vector3[PoseEstimationServer.NumLandmarks];
        }

        private void Update()
        {            
            // Get original landmark positions
            for (int i = 0; i < PoseEstimationServer.NumLandmarks; i++)
            {
                _originalLandmarkPositions[i] = PoseEstimationServer.Instance.GetLandmarkPosInCamera(i);
            }
            
            Array.Copy(_originalLandmarkPositions, _alignedLandmarkPositions, PoseEstimationServer.NumLandmarks);

            AlignmentProblem alignmentProblem = new()
            {
                LandmarksInCameraToAlign = _alignedLandmarkPositions,
                PoseCameraRotation = poseCameraRotation,
                PoseScaleFactor = poseScaleFactor,
                HmdPose = new() { position = hmdTm.position, rotation = hmdTm.rotation },
                LeftHandPose = new() { position = leftHandTm.position, rotation = leftHandTm.rotation },
                RightHandPose = new() { position = rightHandTm.position, rotation = rightHandTm.rotation },
                HmdToHeadTrackerOffset = hmdToHeadTrackerOffset,
                LeftCtrlToLeftHandOffset = leftCtrlToLeftHandOffset,
                RightCtrlToRightHandOffset = rightCtrlToRightHandOffset
            };
            
            Align(out Quaternion _, out Vector3 _, alignmentProblem);

            originalSkeleton.UpdatePositions(_originalLandmarkPositions);
            alignedSkeleton.UpdatePositions(_alignedLandmarkPositions);
        }
    }
}
