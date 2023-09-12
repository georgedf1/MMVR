using System;
using System.Collections.Generic;
using UnityEngine;

namespace H2D.MediaPipe
{
    public class PoseDebugSkeleton : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Min(float.Epsilon)] private float jointDiameter;
        [SerializeField,Min(float.Epsilon)] private float boneDiameter;
        [SerializeField] private List<int> disabledBones;

        [Header("Wiring")] 
        [SerializeField] private Material skeletonMaterial;
        
        private int NumLandmarks => PoseEstimationServer.NumLandmarks;
        private int NumBones => BoneConnectivity.Length;

        private struct IndexPair
        {
            public int I;
            public int J;

            public IndexPair(int i, int j)
            {
                I = i;
                J = j;
            }
        }
        
        private static readonly IndexPair[] BoneConnectivity =
        {
            // Head
            new (0, 2),
            new (0, 5),
            new (5, 8),
            new (2, 7),
            new (9, 10),
            
            // Torso
            new (11, 12),
            new (12, 24),
            new (24, 23),
            new (23, 11),
            
            // Left Arm
            new (11, 13),
            new (13, 15),
            new (15, 17),
            new (17, 19),
            new (19, 15),
            new (15, 21),
            
            // Right Arm
            new (12, 14),
            new (14, 16),
            new (16, 18),
            new (18, 20),
            new (20, 16),
            new (16, 22),
            
            // Left Leg
            new (23, 25),
            new (25, 27),
            new (27, 29),
            new (29, 31),
            new (31, 27),
            
            // Right Leg
            new (24, 26),
            new (26, 28),
            new (28, 30),
            new (30, 32),
            new (32, 28),
        };
        
        private enum DebugPrimitiveType
        {
            Joint,
            Bone
        }

        private GameObject[] _joints;
        private GameObject[] _bones;

        public void UpdatePositions(Vector3[] positions)
        {
            // Joints
            for (int i = 0; i < NumLandmarks; i++)
            {
                _joints[i].transform.localPosition = positions[i];
            }
            
            // Bones
            for (int i = 0; i < NumBones; i++)
            {
                Vector3 posI = positions[BoneConnectivity[i].I];
                Vector3 posJ = positions[BoneConnectivity[i].J];
                _bones[i].transform.localPosition = (posI + posJ) / 2.0f;
                Vector3 direction = posJ - posI;
                _bones[i].transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
                Vector3 localScale = _bones[i].transform.localScale;
                localScale.y = 0.5f * direction.magnitude; // Cylinder y scale is half of length
                _bones[i].transform.localScale = localScale;
            }
        }

        private void Start()
        {
            _joints = new GameObject[NumLandmarks];
            _bones = new GameObject[NumBones];
            
            for (int i = 0; i < NumLandmarks; i++)
            {
                _joints[i] = CreateDebugPrimitive(DebugPrimitiveType.Joint);
                _joints[i].transform.parent = transform;
                if (disabledBones.Contains(i))
                {
                    _joints[i].SetActive(false);
                }
            }

            for (int i = 0; i < NumBones; i++)
            {
                _bones[i] = CreateDebugPrimitive(DebugPrimitiveType.Bone);
                _bones[i].transform.parent = transform;
                if (disabledBones.Contains(BoneConnectivity[i].I) || disabledBones.Contains(BoneConnectivity[i].J))
                {
                    _bones[i].SetActive(false);
                }
            }
        }

        private GameObject CreateDebugPrimitive(DebugPrimitiveType type)
        {
            GameObject go;
            
            switch (type)
            {
                case DebugPrimitiveType.Joint:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(jointDiameter, jointDiameter, jointDiameter);
                    break;
                case DebugPrimitiveType.Bone:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(boneDiameter, 0.5f, boneDiameter);
                    break;
                default:
                    throw new NotImplementedException("Unimplemented DebugPrimitiveType");
            }

            go.GetComponent<MeshRenderer>().material = skeletonMaterial;
            
            Destroy(go.GetComponent<Collider>());
            
            return go;
        }
    }
}