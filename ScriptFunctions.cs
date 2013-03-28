using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Bentley.Geometry;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using Bentley.GenerativeComponents.GeneralPurpose;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.GCScript.GCTypes;
using Bentley.GenerativeComponents.GCScript.NameScopes;
using Bentley.Interop.MicroStationDGN;
using Bentley.Wrapper;

using Cerver.Functions;
using Cerver.GCExtensionMethods;

namespace SampleAddIn
{
    static internal class ScriptFunctions
    {
        static internal void Load()
        {
            // This method is called from within the constructor of the class, Initializer (within this project).
            NamespaceCatalogSnapshot nscs = NameScopeTools.GetStandardNamespaceCatalogForUser();
            NameCatalog nameCatalog = nscs.GetNameCatalog();


            nameCatalog.AddNamespaceLevelFunction("ListRemap", "double[] function(double[] NumberList, double Start, double End)", ListRemap);
            nameCatalog.AddNamespaceLevelFunction("ListMin", "double function(double[] NumberList)", ListMin);
            nameCatalog.AddNamespaceLevelFunction("ListMax", "double function(double[] NumberList)", ListMax);
            nameCatalog.AddNamespaceLevelFunction("ClosestPointArray", "Point function(Point[] PtSetA, Point[] PtSetB)", ClosestPoint2Sets);
            nameCatalog.AddNamespaceLevelFunction("ClosestPoint", "Point function(Point SearchPt, Point[] PtsToSearch)", ClosestPoint);
            nameCatalog.AddNamespaceLevelFunction("ClosestPointOnSurface", "IPoint function(ISurface surface, IPoint point)", closestPointOnSurf);
            nameCatalog.AddNamespaceLevelFunction("MeshConnectedVertex", "DPoint3d function(Mesh mesh, IPoint SearchPoint)", MeshConnectedVertex);
            nameCatalog.AddNamespaceLevelFunction("MeshConnectedVertices", "DPoint3d function(Mesh mesh)", MeshConnectedVertices);
            nameCatalog.AddNamespaceLevelFunction("MeshEdges", "Mesh function(Mesh mesh)", MeshEdges);
        }


        /// <summary>Compares the Points in PtSetA with all the points in PtSetB, return the Point in PtSetA that is closest to each Point in PtSetB</summary>
        static private void ClosestPoint2Sets(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.
            
            Point[] ptsA = frame.UnboxArgument<Point[]>(0);
            Point[] ptsB = frame.UnboxArgument<Point[]>(1);

            Point[] result = new Point[ptsB.Length];

            double dist = double.MaxValue;
            double tDist;
            DPoint3d cpt;
            int i = 0;

            foreach (Point ptB in ptsB)
            {
                cpt = ptB.DPoint3d;
                foreach(Point ptA in ptsA)
                {
                    tDist = ptA.DPoint3d.Distance(ref cpt);
                    if (tDist < dist)
                    {
                        dist = tDist;
                        result[i] = ptB;
                    }
                }

                i++;
            }


            CPU.SetFunctionResult(Boxer.Box(result));
        }

        /// <summary>Fidens the closest point from a search point and a setof points to search</summary>
        static private void ClosestPoint(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            Point ptsA = frame.UnboxArgument<Point>(0);
            Point[] ptsB = frame.UnboxArgument<Point[]>(1);

            Point result = new Point();

            double dist = double.MaxValue;
            double tDist;
            DPoint3d cpt;
            int i = 0;

            foreach (Point ptB in ptsB)
            {
                cpt = ptB.DPoint3d;

                tDist = ptsA.DPoint3d.Distance(ref cpt);
                if (tDist < dist)
                {
                    dist = tDist;
                    result = ptB;
                }
                
            }


            CPU.SetFunctionResult(Boxer.Box(result));
        }

        /// <summary>Takes a list of numbers and rescales the values to be between the start and end values</summary>
        static private void ListRemap(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            double[] list = frame.UnboxArgument<double[]>(0);    
            double start = frame.UnboxArgument<double>(1);
            double end = frame.UnboxArgument<double>(2);   
            
            // Here's the main body of our function.
            
            double[] result = new double[list.Length];

            double min = CerverFunctions.ListMin(list);
            double max = CerverFunctions.ListMax(list);
            double range = max - min;

            double temp;
            

            for (int i = 0; i < list.Length; i++)
            {
                temp = (((list[i] - min)/ range)*(end-start))+start; 
                result[i] = temp;
            }


            CPU.SetFunctionResult(Boxer.Box(result));
        }

        /// <summary>Returns the smallest number in a list</summary>
        static private void ListMin(CallFrame frame)
        {
            double[] list = frame.UnboxArgument<double[]>(0);

            double min = CerverFunctions.ListMin(list);

            CPU.SetFunctionResult(Boxer.Box(min));
        }
        /// <summary>Returns the largest number in a list</summary>
        static private void ListMax(CallFrame frame)
        {
            double[] list = frame.UnboxArgument<double[]>(0);

            double max = CerverFunctions.ListMax(list);

            CPU.SetFunctionResult(Boxer.Box(max));
        }

        /// <summary>Get the closest point to a surface</summary>
        static private void closestPointOnSurf(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            ISurface surface = frame.UnboxArgument<ISurface>(0);    // Get the first argument (i.e., the argument at index 0) 
            IPoint point = frame.UnboxArgument<IPoint>(1);

            Point3d cp = new Point3d();
            Point2d uv = new Point2d();
            Point3d fromPt = new Point3d();
            fromPt.X = point.X; fromPt.Y = point.Y; fromPt.Z = point.Z;

            // Here's the main body of our function.

            Point result;
            surface.com_bsplineSurface.ComputeMinimumDistance(ref cp, ref uv, ref fromPt);
            result = new Point();



            CPU.SetFunctionResult(Boxer.Box(result));
        }

        /// <summary>Gets all the vertices connected to a single vertex on a mesh</summary>
        static private void MeshConnectedVertex(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            Mesh mesh = frame.UnboxArgument<Mesh>(0);    // Get the first argument (i.e., the argument at index 0) 
            IPoint SearchPoint = frame.UnboxArgument<IPoint>(1);

            int[] ConnectedVtxID;

            DPoint3d[] cntVtx = mesh.ConnectedVtx(SearchPoint.DPoint3d, out ConnectedVtxID);



            CPU.SetFunctionResult(Boxer.Box(cntVtx));
        }

        /// <summary>Gets all the vertices connected to a all the vertices on a mesh</summary>
        static private void MeshConnectedVertices(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            Mesh mesh = frame.UnboxArgument<Mesh>(0);    // Get the first argument (i.e., the argument at index 0) 

            int[] ConnectedVtxID;
            List<DPoint3d[]> result = new List<DPoint3d[]>(mesh.Vertices.Length);

            foreach (var vtx in mesh.Vertices)
            {
                result.Add(mesh.ConnectedVtx(vtx.DPoint3d, out ConnectedVtxID));
            }



            CPU.SetFunctionResult(Boxer.Box(result.ToArray()));
        }

        /// <summary>Return the edges of a mesh as a DSegment3d array</summary>
        static private void MeshEdges(CallFrame frame)
        {
            // Use the following technique to get the "native" .NET values of the given arguments.

            Mesh mesh = frame.UnboxArgument<Mesh>(0);    // Get the first argument (i.e., the argument at index 0) 
            CPU.SetFunctionResult(Boxer.Box(mesh.Edges()));
        }

        static private void UDPsend(CallFrame frame)
        {
            //dat
            
        }
        
    }
}
