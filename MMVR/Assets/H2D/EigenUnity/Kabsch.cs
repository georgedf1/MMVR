using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace H2D.EigenUnity
{
    public static class Kabsch
    {
        public static Quaternion Compute(Vector3[] p, Vector3[] q)
        {
            int numPoints = p.Length;
            Assert.IsTrue(numPoints > 0);
            Assert.IsTrue(numPoints == q.Length);

            IntPtr pUnmanaged = Marshal.AllocCoTaskMem(sizeof(float) * numPoints * 3);
            {
                float[] pFlat = new float[numPoints * 3];
                for (int i = 0; i < numPoints; i++) 
                {
                    pFlat[3 * i] = p[i].x;
                    pFlat[3 * i + 1] = p[i].y;
                    pFlat[3 * i + 2] = p[i].z;
                }
                Marshal.Copy(pFlat, 0, pUnmanaged, numPoints * 3);
            }

            IntPtr qUnmanaged = Marshal.AllocCoTaskMem(sizeof(float) * numPoints * 3);
            {
                float[] qFlat = new float[numPoints * 3];
                for(int i = 0;i < numPoints; i++)
                {
                    qFlat[3 * i] = q[i].x;
                    qFlat[3 * i + 1] = q[i].y;
                    qFlat[3 * i + 2] = q[i].z;
                }
                Marshal.Copy(qFlat, 0, qUnmanaged, numPoints * 3);
            }

            IntPtr outRotQuatUnmanaged = Marshal.AllocCoTaskMem(sizeof(float) * 4);
            computeZeroedCentroidKabsch(pUnmanaged, qUnmanaged, numPoints, outRotQuatUnmanaged);

            Quaternion outRotQuat;
            {
                float[] outRotQuatFlat = new float[4];
                Marshal.Copy(outRotQuatUnmanaged, outRotQuatFlat, 0, 4);
                outRotQuat.x = outRotQuatFlat[0];
                outRotQuat.y = outRotQuatFlat[1];
                outRotQuat.z = outRotQuatFlat[2];
                outRotQuat.w = outRotQuatFlat[3];
            }

            Marshal.FreeCoTaskMem(pUnmanaged);
            Marshal.FreeCoTaskMem(qUnmanaged);
            Marshal.FreeCoTaskMem(outRotQuatUnmanaged);

            return outRotQuat;
        }

        [DllImport("EigenUnity")]
        private extern static void computeZeroedCentroidKabsch(IntPtr p, IntPtr q, int numPts, IntPtr outRotQuat);
    }
}