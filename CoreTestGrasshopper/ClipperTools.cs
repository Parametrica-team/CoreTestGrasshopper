
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using Rhino.Geometry;

namespace UrbanbotCore
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class ClipperTools
    {
        
        public ClipperTools()
        {
        }

        public static List<Curve> Offset(Curve crv1, double offset, double tolerance = 1)
        {
            if (crv1 == null) return null;

            var clipper = new ClipperOffset();
            var pts = Util.GetCurveCorners(crv1);

            // если замкнутая, нужно добавить первую точку в конец
            if (crv1.IsClosed)
                pts.Add(pts[0]);

            Path polygon = pts.Select(pt => Point3dToIntPoint(pt, tolerance)).ToList();

            clipper.AddPath(polygon, JoinType.jtMiter, EndType.etClosedLine);

            Paths solution = new Paths();
            clipper.Execute(ref solution, offset);

            var offsetedPts = solution.Select(of => of.Select(ipt => IntPointToPoint3d(ipt, tolerance)).ToList()).ToList();

            // если замкнутая, нужно добавить первую точку в конец
            if (crv1.IsClosed)
            {
                foreach (var ptGroup in offsetedPts)
                {
                    ptGroup.Add(ptGroup[0]);
                }
            }

            var polylines = offsetedPts.Select(opts => new Polyline(opts).ToNurbsCurve() as Curve).ToList();

            return polylines;
        }

        private static Point3d IntPointToPoint3d(IntPoint ipt, double tolerance)
        {
            return new Point3d(ipt.X * tolerance, ipt.Y * tolerance, 0);
        }

        private static IntPoint Point3dToIntPoint(Point3d point3d, double tolerance = 1)
        {
            return new IntPoint((long)(point3d.X / tolerance), (long)(point3d.Y / tolerance));
        }
    }
}