using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bentley.GenerativeComponents.MicroStation;
using Bentley.GenerativeComponents.GeneralPurpose;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.GCScript.GCTypes;
using Bentley.GenerativeComponents.GCScript.NameScopes;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using Bentley.Geometry;
using Bentley.Interop.MicroStationDGN;
using Cerver.Functions;

namespace Cerver.Functions
{
    public class CerverFunctions
    {
        public static double Sqr(double num)
        {
            return num * num;
        }
        private static double SquaredDistance(DPoint3d pt0, DPoint3d pt1)
        {
            double dx = pt0.X - pt1.X;
            double dy = pt0.Y - pt1.Y;
            double dz = pt0.Z - pt1.Z;

            return dx * dx + dy * dy + dz * dz;
        }

        static public double ListMin(double[] list)
        {
            double min = list[0];

            foreach (double d in list)
            {
                if (d < min) min = d;
            }
            return min;
        }
        static public double ListMax(double[] list)
        {
            double max = list[0];

            foreach (double d in list)
            {
                if (d > max) max = d;
            }
            return max;
        }

        public static double DPDistance(DPoint3d ptA, DPoint3d ptB)
        {
            double dx = Sqr((ptA.X - ptB.X));
            double dy = Sqr((ptA.Y - ptB.Y));
            double dz = Sqr((ptA.Z - ptB.Z));

            double dist = Math.Sqrt((dx + dy + dz));

            return dist;
        }
        public static double Distance(IPoint ptA, IPoint ptB)
        {

            return DPDistance(ptA.DPoint3d, ptB.DPoint3d);
        }

        public static DPoint3d closestPointOnSurf(DPoint3d point, ISurface surf, double tol, out double Dist, out Point2d UV, out DVector3d normal)
        {
            Bentley.Interop.MicroStationDGN.Point3d cpRef = new Bentley.Interop.MicroStationDGN.Point3d();
            cpRef.X = point.X;
            cpRef.Y = point.Y;
            cpRef.Z = point.Z;

            Bentley.Interop.MicroStationDGN.Point3d cp = new Bentley.Interop.MicroStationDGN.Point3d();
            Bentley.Interop.MicroStationDGN.Point2d cp2d = new Bentley.Interop.MicroStationDGN.Point2d();

            Dist = GeometryTools.BSplineSurfaceComputeMinimumDistance(ref cp, ref cp2d, ref cpRef, tol, surf.com_bsplineSurface);
            UV = new Point2d();
            UV.X = cp2d.X;
            UV.Y = cp2d.Y;

            normal = NormalAtUVParameterOnSurface(surf.com_bsplineSurface, UV.X, UV.Y);

            return new DPoint3d(cp.X, cp.Y, cp.Z);

        }
        public static DPoint3d closestPointOnSurf(DPoint3d point, ISurface surf, double tol)
        {
            Bentley.Interop.MicroStationDGN.Point3d cpRef = new Bentley.Interop.MicroStationDGN.Point3d();
            cpRef.X = point.X;
            cpRef.Y = point.Y;
            cpRef.Z = point.Z;

            Bentley.Interop.MicroStationDGN.Point3d cp = new Bentley.Interop.MicroStationDGN.Point3d();
            Bentley.Interop.MicroStationDGN.Point2d cp2d = new Bentley.Interop.MicroStationDGN.Point2d();

            GeometryTools.BSplineSurfaceComputeMinimumDistance(ref cp, ref cp2d, ref cpRef, tol, surf.com_bsplineSurface);
            
            return new DPoint3d(cp.X, cp.Y, cp.Z);

        }      

        public static bool isPointInside(ISurface bs, DPoint3d testPt, double tol, out DVector3d normal, out DPoint3d ClosestPoint)
        {
            Point2d uvPoint = new Point2d();
            DVector3d norm;
            double dist = 0;

            DPoint3d cp = closestPointOnSurf(testPt, bs, tol, out dist, out uvPoint, out norm);

            normal = norm;
            var testVec = new DVector3d(ref cp, ref testPt);

            ClosestPoint = cp;

            if (norm.AngleTo(ref testVec).Degrees <= 90)
            {
                return false;
            }
            else return true;

        }
        public static bool isPointInside(ISurface bs, DPoint3d testPt, double tol)
        {
            DPoint3d cp;
            DVector3d normal;
            return isPointInside(bs, testPt, tol, out normal, out cp);

        }
       
        public static DPoint3d MeshCP(Mesh m, DPoint3d p)
        {
            double cdist = m.Vertices[0].DPoint3d.Distance(ref p);
            double dist;

            DPoint3d cp = m.Vertices[0].DPoint3d;

            foreach (Point mp in m.Vertices)
            {
                dist = mp.DPoint3d.Distance(ref p);
                if (dist < cdist)
                {
                    cp = mp.DPoint3d;
                }

            }

            return cp;

        }
        public static DSegment3d[] GetMeshEdges(Mesh m, out int[] startVtx, out int[] endVtx)
        {
            Dictionary<string, DSegment3d> edgeDic = new Dictionary<string, DSegment3d>(m.Vertices.Length);

            string key, keyr;

            DPoint3d p0, p1;

            List<int> svtx = new List<int>(m.Vertices.Length * 2);
            List<int> evtx = new List<int>(m.Vertices.Length * 2);
            string hash1, hash2;            

            foreach (var f in m.Indices)
            {
                for (int i = 0; i < f.Length; i++)
                {
          
                    hash2 = m.Vertices[f[i] - 1].GetHashCode().ToString();
                    if (i < f.Length - 1)
                    {
                        hash1 = m.Vertices[f[i + 1] - 1].GetHashCode().ToString();
                        
                        key = hash2 + "|" + hash1;
                        keyr = hash1 + "|" + hash2;
                    }
                    else
                    {
                        hash1 = m.Vertices[f[0] - 1].GetHashCode().ToString();

                        key = hash2 + "|" + hash1;
                        keyr = hash1 + "|" + hash2;
                    }

                    if (!edgeDic.ContainsKey(key) && !edgeDic.ContainsKey(keyr))
                    {
                        if (i < f.Length - 1)
                        {
                            p0 = m.Vertices[f[i] - 1].DPoint3d;
                            p1 = m.Vertices[f[i + 1] - 1].DPoint3d;

                            svtx.Add(f[i] - 1);
                            evtx.Add(f[i + 1] - 1);
                        }
                        else
                        {
                            p0 = m.Vertices[f[i] - 1].DPoint3d;
                            p1 = m.Vertices[f[0] - 1].DPoint3d;
                            svtx.Add(f[i] - 1);
                            evtx.Add(f[0] - 1);
                        }
                        try
                        {
                            edgeDic.Add(key, new DSegment3d(ref p0, ref p1));
                        }catch (ArgumentException)
                        {
                            Feature.Print("key exits");
                        }

                    }

                }

            }
            startVtx = svtx.ToArray();
            endVtx = evtx.ToArray();

            return edgeDic.Values.ToArray(); 


        }
        public static int[] GetConnectedPointID(Mesh m, DPoint3d centerPoint)
        {

            int ptid = -1;
            for (int i = 0; i < m.Vertices.Length; i++)
            {
                if (m.Vertices[i].X == centerPoint.X && m.Vertices[i].Y == centerPoint.Y && m.Vertices[i].Z == centerPoint.Z)
                {
                    ptid = i;
                    break;
                }
                
            }
    
            return GetConnectedPointID(m, ptid);

        }
        public static int[] GetConnectedPointID(Mesh m, int vtxId)
        {

            int ptid = vtxId;

            int[] svtx, evtx;
            DSegment3d[] edges = GetMeshEdges(m, out svtx, out evtx);
            Dictionary<int, int> connectedID = new Dictionary<int, int>(6);

            for (int i = 0; i < svtx.Length; i++)
            {
                if (svtx[i] == ptid) connectedID.Add(evtx[i], evtx[i]);
                if (evtx[i] == ptid) connectedID.Add(svtx[i], svtx[i]);
            }

            return connectedID.Values.ToArray();

        }

        public static DVector3d NormalAtUVParameterOnSurface(BsplineSurface com_bsplineSurface, double U, double V)
        {
            if (com_bsplineSurface != null)
            {
                int arg_0C_0 = com_bsplineSurface.UPolesCount;
                int arg_13_0 = com_bsplineSurface.VPolesCount;
                
                if (U >= 1.0) U = 0.99999;
                if (U <= 0.0) U = 1E-05;
                if (V >= 1.0) V = 0.99999;

                if (V <= 0.0) V = 1E-05;

                if (com_bsplineSurface.BoundsCount > 0  && !Translation.IsPointUVparametersWithinSurfaceBounds(com_bsplineSurface, U, V))
                {
                    return DVector3d.Zero;
                }
                Point2d point2d = DgnTools.ToPoint2d(new DPoint2d(U, V));
                FirstPartials3d firstPartials3d = default(FirstPartials3d);
                Point3d source = default(Point3d);
                Point3d source2 = DgnTools.ToPoint3d(DPoint3d.Zero);
                try
                {
                    source2 = com_bsplineSurface.EvaluatePointDerivatives1(ref firstPartials3d, ref source, ref point2d);
                }
                catch
                {
       
                }
                DPoint3d origin = DgnTools.ToDPoint3d(source2);
                DVector3d dVector3d = DgnTools.ToDVector3d(source);

                return dVector3d;
            }
            return DVector3d.Zero;
        }
    }
}

namespace Cerver.GCExtensionMethods
{

    public static class MeshExtensions
    {

        public static DSegment3d[] Edges(this Mesh m)
        {
            int[] svtx, evtx;

            return CerverFunctions.GetMeshEdges(m, out svtx, out evtx);

        }

        public static int[] ConnectedVtxID(this Mesh m, DPoint3d searchPoint)
        {
            return CerverFunctions.GetConnectedPointID(m, searchPoint);
        }
        public static int[] ConnectedVtxID(this Mesh m, int vtxIndex)
        {
            return CerverFunctions.GetConnectedPointID(m,vtxIndex );
        }

        public static DPoint3d[] ConnectedVtx(this Mesh m, DPoint3d searchPoint)
        {

            int[] conectedVtxID = CerverFunctions.GetConnectedPointID(m, searchPoint);

            List<DPoint3d> connectedVtx = new List<DPoint3d>(conectedVtxID.Length);

            foreach (var id in conectedVtxID)
            {

                connectedVtx.Add(m.Vertices[id].DPoint3d);
            }

            return connectedVtx.ToArray();

        }
        public static DPoint3d[] ConnectedVtx(this Mesh m, DPoint3d searchPoint, out int[] conectedVtxIDs)
        {

            int[] conectedVtxID = CerverFunctions.GetConnectedPointID(m, searchPoint);
            conectedVtxIDs = conectedVtxID;

            List<DPoint3d> connectedVtx = new List<DPoint3d>(conectedVtxID.Length);

            foreach (var id in conectedVtxID)
            {

                connectedVtx.Add(m.Vertices[id].DPoint3d);
            }

            return connectedVtx.ToArray();

        }
    }


}