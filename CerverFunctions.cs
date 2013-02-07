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
       
        public static List<DPoint3d> RemoveDupPts(List<DPoint3d> mypoints, double tolerance)
        {
            List<DPoint3d> nodups = new List<DPoint3d>(mypoints.Count / 4); // Preallocate some. Just a guess

            if (mypoints.Count > 0)
            {
                nodups.Add(mypoints[0]);

                double squaredTolerance = tolerance * tolerance; // This also avoids negative tolerances

                for (int i = 1; i < mypoints.Count; i++) // The first one is already in
                {
                    bool unique = true;
                    DPoint3d mpi = mypoints[i];

                    for (int j = 0; j < nodups.Count; j++)
                    {
                        if (SquaredDistance(mpi, nodups[j]) <= squaredTolerance)
                        {
                            unique = false;
                            break; // Once not unique, look no further
                        }
                    }

                    if (unique)
                    {
                        nodups.Add(mpi);
                    }
                }
            }

            return nodups;
        }
        public static List<Line> RemoveDupLn(List<Line> lines, double tolerance)
        {
            List<Line> nodups = new List<Line>();

            for (int i = 0; i <= lines.Count - 1; i++)
            {
                if (lines[i].GetSuccess())
                {
                    nodups.Add(lines[i]);
                }
                if (nodups.Count > 0)
                    break;
            }

            bool dup = false;


            for (int i = 0; i <= lines.Count - 1; i++)
            {
                dup = false;


                for (int j = 0; j <= nodups.Count - 1; j++)
                {
 
                    if ((Distance(lines[i].StartPoint, lines[i].EndPoint) <= tolerance & Distance(nodups[j].EndPoint, lines[i].EndPoint) <= tolerance) | (Distance(nodups[j].EndPoint, lines[i].StartPoint) <= tolerance & Distance(nodups[j].StartPoint, lines[i].EndPoint) <= tolerance))
                    {
                        dup = true;
                    }
                    else
                    {
                    }
                }

                if (dup == false & lines[i].GetSuccess())
                {
                    nodups.Add(lines[i]);
                }

            }
            return nodups;

        }
        public static List<Line> InterConnect(FeatureUpdateContext updateCtx, List<Point> pts)
        {
            int ct = (pts.Count - 1) * pts.Count;

            List<Line> inter = new List<Line>(ct);
            Line temp = new Line();


            for (int i = 0; i < pts.Count - 1; i++)
            {
                for (int j = i + 1; j < pts.Count; j++)
                {
                    temp.ByPoints(updateCtx, pts[i], pts[j]);
                    inter.Add(temp);

                }
            }

            return inter;
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

            foreach (var f in m.Indices)
            {
                for (int i = 0; i < f.Length; i++)
                {
                    if (i < f.Length - 1)
                    {
                        key = string.Format("{0}{1}{2}-{3}{4}{5}", m.Vertices[f[i] - 1].X, m.Vertices[f[i] - 1].Y, m.Vertices[f[i] - 1].Z, m.Vertices[f[i + 1] - 1].X, m.Vertices[f[i + 1] - 1].Y, m.Vertices[f[i + 1] - 1].Z);
                        keyr = string.Format("{3}{4}{5}-{0}{1}{2}", m.Vertices[f[i] - 1].X, m.Vertices[f[i] - 1].Y, m.Vertices[f[i] - 1].Z, m.Vertices[f[i + 1] - 1].X, m.Vertices[f[i + 1] - 1].Y, m.Vertices[f[i + 1] - 1].Z);

                    }
                    else
                    {
                        key = string.Format("{0}{1}{2}-{3}{4}{5}", m.Vertices[f[i] - 1].X, m.Vertices[f[i] - 1].Y, m.Vertices[f[i] - 1].Z, m.Vertices[f[0] - 1].X, m.Vertices[f[0] - 1].Y, m.Vertices[f[0] - 1].Z);
                        keyr = string.Format("{3}{4}{5}-{0}{1}{2}", m.Vertices[f[i] - 1].X, m.Vertices[f[i] - 1].Y, m.Vertices[f[i] - 1].Z, m.Vertices[f[0] - 1].X, m.Vertices[f[0] - 1].Y, m.Vertices[f[0] - 1].Z);
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

                        edgeDic.Add(key, new DSegment3d(ref p0, ref p1));
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
            int[] svtx, evtx;
            DSegment3d[] edges = GetMeshEdges(m, out svtx, out evtx);
            Dictionary<int, int> connectedID = new Dictionary<int, int>(6);

            for (int i = 0; i <svtx.Length; i++)
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