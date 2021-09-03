
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

        public static List<Curve> Offset(Curve crv1, double offset, JoinType joinType = JoinType.jtMiter, EndType endType = EndType.etOpenButt, double tolerance = 1)
        {
            if (crv1 == null) return null;

            var clipper = new ClipperOffset();
            var pts = Util.GetCurveCorners(crv1);

            // если замкнутая, то нужно использовать etcClosedPolygon
            // так сделает только одну кривую
            if (crv1.IsClosed)
                endType = EndType.etClosedPolygon;

            Path polygon = pts.Select(pt => Point3dToIntPoint(pt, tolerance)).ToList();

            clipper.AddPath(polygon, joinType, endType);

            PolyTree solution = new PolyTree();
            clipper.Execute(ref solution, offset);

            var offsetedPolylines = solution.Childs
                .Select(ch => ch.Contour
                    .Select(ipt => IntPointToPoint3d(ipt, tolerance)))
                .Select(opts => new Polyline(opts))
                .ToList();

            var offsettedCrvs = new List<Curve>();

            // если не замкнутая, нужно разомкнуть
            if (!crv1.IsClosed)
            {
                foreach (var poly in offsetedPolylines)
                {
                    for (int i = 0; i < poly.Count; i++)
                    {
                        // находим центр отрезка
                        // если центр отрезка не совпадает с началом или концом кривой, то добавляем следующую точку к полилинии
                        // в противном случае добавляем точку к новой полилинии
                        
                        // первая точка может быть где-то в середине кривой, тогда в итоге будет 3 полилинии
                    }

                    // из двух полилиний нужно выбрать одну, например правее от исходной кривой
                }

                // временно, чтобы посмотреть что происходит
                offsettedCrvs = offsetedPolylines.Select(p => p.ToNurbsCurve() as Curve).ToList();
            }
            else
            {
                foreach (var poly in offsetedPolylines)
                {
                    poly.Add(poly[0]);
                }
                offsettedCrvs = offsetedPolylines.Select(p => p.ToNurbsCurve() as Curve).ToList();
            }

            //var polylines = offsetedPts.Select(opts => new Polyline(opts).ToNurbsCurve() as Curve).ToList();

            return offsettedCrvs;
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