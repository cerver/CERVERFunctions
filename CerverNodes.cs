using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Bentley.Geometry;
//using Bentley.MicroStation;
using Bentley.Interop.MicroStationDGN;
using Bentley.GenerativeComponents;
using Bentley.GenerativeComponents.MicroStation;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.GCScript.UISupport;
using Bentley.GenerativeComponents.XceedHelpers;

using Cerver.GCExtensionMethods;
using Cerver.Functions;

namespace Bentley.GenerativeComponents.Features.Specific // Must be in this namespace.
{
  
   //  kutil feature
    //[GCNamespace("Cerver.Kangaroo")]
    public class CTools : Feature
    {

        public CTools()
        {

        }

        public CTools(Feature parentFeature)
            : base(parentFeature)
        {
        }

        #region closest point on surf
        [Technique]
        public bool ClosestPointOnSurf

            (

            FeatureUpdateContext updateContext,
            [DefaultExpression("baseCS")]                CoordinateSystem cs,
            [Replicatable]                              IPoint point,
            [Replicatable]                              BSplineSurface surf,
            [DefaultValue(0.01)]                        double tol,
            [Out]                                       ref Point CPpoint,
            [Out]                                       ref double Dist,
            [Out]                                       ref DVector3d Normal


            )
        {

            this.LetConstituentFeaturesBeDirectlyIndexible();
            this.DeleteConstituentFeatures(updateContext);

            Point2d tempPt;

            DPoint3d dp = CerverFunctions.closestPointOnSurf(point.DPoint3d, surf, tol, out Dist, out tempPt, out Normal);

            Point outPt = new Point(this);
            outPt.FromDPoint3d(updateContext, cs, dp);
            outPt.SetSuccess(true);
            AddConstituentFeature(outPt);
            
            CPpoint = outPt;

            return true;
        } 
        #endregion

        #region mesh edges
        [Technique]
        public bool MeshEdgeAsLines

            (
            FeatureUpdateContext updateContext,
            [DefaultExpression("baseCS")]                CoordinateSystem cs,
            [Replicatable]                               Mesh mesh,
            [Out]                                       ref Line[] Edges,
            [Out]                                       ref double[] Length,
            [Out]                                       ref int[] StartPointVtx,
            [Out]                                       ref int[] EndPointVtx
            )
        {

            this.LetConstituentFeaturesBeDirectlyIndexible();
            this.DeleteConstituentFeatures(updateContext);

            DSegment3d[] m_dlines = CerverFunctions.GetMeshEdges(mesh, out StartPointVtx, out EndPointVtx);
            double[] m_len = new double[m_dlines.Length];
            Line[] m_Edges = new Line[m_dlines.Length];

            for (int i=0; i< m_dlines.Length; i++)
            {
                string name = string.Format("{0}[{1}]", this.Name, i);

                m_Edges[i] = new Line(this);
                m_Edges[i].LetConstituentFeaturesBeDirectlyIndexible();
                m_Edges[i].FromDSegment3d(updateContext,cs,m_dlines[i]);
                m_Edges[i].AlignOptions(this);
                m_Edges[i].SetSuccess(true);
                m_len[i] = m_Edges[i].Length;

            }

            this.AddReplicatedChildFeatures(m_Edges);
            Edges = m_Edges;
            Length = m_len;
            return true;
        }
        #endregion

        #region Mesh Connected Verticies 
        [Technique]
        public bool MeshConnectedVerticies

            (
            FeatureUpdateContext updateContext,
            [DefaultExpression("baseCS")]                CoordinateSystem cs,
                                                                     Mesh mesh,
            [Replicatable]                                         IPoint SearchPoint,
            [DefaultValue(false)]                                    bool generatePoints,
            [Out]                                       ref int[] ConnectedVtxID,
            [Out]                                       ref Point[] ConnectedVtx,
            [Out]                                       ref DPoint3d[] DConnectedVtx

            )
        {

            this.LetConstituentFeaturesBeDirectlyIndexible();
            this.DeleteConstituentFeatures(updateContext);

            //System.Diagnostics.Stopwatch stp = new Stopwatch();

           // stp.Start();
            DPoint3d[] cntVtx = mesh.ConnectedVtx(SearchPoint.DPoint3d, out ConnectedVtxID);
            DConnectedVtx = cntVtx;
           // Feature.Print("area 1 " + stp.ElapsedMilliseconds.ToString());
           // stp.Reset();

            if (generatePoints)
            {
                List<Point> vtx = new List<Point>(cntVtx.Length);

                foreach (var v in cntVtx)
                {
                    Point p = new Point(this);
                    p.FromDPoint3d(updateContext, cs, v);
                    p.SetSuccess(true);
                    vtx.Add(p);

                }
                this.AddReplicatedChildFeatures(ConnectedVtx);
                ConnectedVtx = vtx.ToArray();
            }
           // Feature.Print("area 2 " + stp.ElapsedMilliseconds.ToString());
           // stp.Stop();

            return true;
        }
        #endregion
        
        
    }
 
}
