using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace GrassPlugin
{
    public class GhPolylineRobooffset : GH_Component
    {
        /// <inheritdoc/>
        public GhPolylineRobooffset()
          : base(
                "Polyline Offset",
                "Robooffset",
                "Offset a curve as a polyline using Clipper library",
                "Urbanbot-new",
                "Utility")
        {
        }

        /// <inheritdoc/>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to offset", GH_ParamAccess.item);

            pManager.AddNumberParameter("Distance", "D", "Offset distance", GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Tolerance",
                "T",
                "All floating point data beyond this precision will be discarded",
                GH_ParamAccess.item,
                0.01);

            //pManager.AddIntegerParameter(
            //    "Closed Fillet",
            //    "CF",
            //    "Closed fillet type:\n" +
            //    "0 - Round\n" +
            //    "1 - Square\n" +
            //    "2 - Miter",
            //    GH_ParamAccess.item);

            //pManager.AddIntegerParameter(
            //    "Open Fillet",
            //    "OF",
            //    "Open fillet type:\n" +
            //    "0 - Round\n" +
            //    "1 - Square\n" +
            //    "2 - Butt",
            //    GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Miter",
                "M",
                "If closed fillet type of \"Miter\" is selected: the maximum extension of a curve is \"Distance * Miter\"",
                GH_ParamAccess.item,
                2);

            for (int i = 2; i < pManager.ParamCount; i++)
            {
                pManager[i].Optional = true;
            }
        }

        /// <inheritdoc/>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Resulting polyline offset", GH_ParamAccess.list);
        }

        /// <inheritdoc/>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve inputCurve = null;

            double offsetDistance = 0;

            double clipperTolerance = 0;

            // В клиппере эти штуки фактически "пресеты" для типов соединений. Пока не очень понятно, как они работают.
            // Это энумы в ClipperTools/geometry/polyline3d. посмотреть на них можно в ClipperTools/geometry, метод Offset с 212 строки  
            //int closedFillet = 0;

            //int openFillet = 0;

            double miterLimit = 0;


            if (DA.GetData("Curve", ref inputCurve))
            {
                if (!inputCurve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid input curve");
                    return;
                }
            }
            

            DA.GetData("Distance", ref offsetDistance);
            DA.GetData("Tolerance", ref clipperTolerance);
            DA.GetData("Miter", ref miterLimit);
            
            

            var pt = new CoreTestGrasshopper.Class1();

            var off = UrbanbotCore.ClipperTools.Offset(
                inputCurve,
                offsetDistance,
                JoinType.jtMiter,
                EndType.etOpenButt,
                clipperTolerance: clipperTolerance,
                miterLimit: miterLimit);

            DA.SetDataList(0, off);
        }

        /// <inheritdoc/>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <inheritdoc/>
        public override Guid ComponentGuid
        {
            get { return new Guid("c2de733f-a4bc-4129-908c-a320b7756041"); }
        }
    }
}
