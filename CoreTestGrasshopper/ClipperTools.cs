using System;
using System.Collections.Generic;
using System.Linq;
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

        // вынести толеранс, митер и джоин тайп? в компонент
        public static List<Curve> Offset(
            Curve inputCrv,
            double offsetDistance,
            JoinType joinType = JoinType.jtMiter,
            EndType endType = EndType.etOpenButt,
            double clipperTolerance = 1,
            double miterLimit = 2)
        {
            if (inputCrv == null) return null;

            var offsettedCrvs = new List<Curve>();

            // вернёт исходую кривую, если ничего не нужно офсетить
            if (offsetDistance == 0)
            {
                offsettedCrvs.Add(inputCrv);
                return offsettedCrvs;
            }

            var clipper = new ClipperOffset(miterLimit: miterLimit);

            var cornerPts = Util.GetCurveCorners(inputCrv);

            // Каждая точка вычислится с точностью n, но любые арифметические операции увеличат погрешность
            // интпоинты тоже, скорее всего влияют на точность
            // с этим что-то нужно сделать. Наверное, толеранс для сравнения нужно внутренний делать
            var comparedTolerance = clipperTolerance * 1.5;

            // если замкнутая, то нужно использовать etcClosedPolygon
            // так сделает только одну кривую
            if (inputCrv.IsClosed)
            {
                endType = EndType.etClosedPolygon;
            }


            Path polygon = cornerPts.Select(pt => Point3dToIntPoint(pt, clipperTolerance)).ToList();

            clipper.AddPath(polygon, joinType, endType);

            PolyTree solution = new PolyTree();


            // Клиппер, видимо, проверяет кривую на замкнутость и "внутрь" кривой -
            // для замкнутой отрицательный оффсет случится, а для незамкнутой просто не построится.
            // Tolerance: Cutoff point. Eg. point {1.245; 9.244351; 19.3214} with precision {0.1} will be cut
            // off to {1.2; 9.2; 19.3}
            // С делением офсета на точность как будто есть какой-то глубинный смысл в методе DoOffset в clipper tools.
            // Полученное значение не просто офсет, а delta. Но я пока не понял. (Немножко понял)
            if (inputCrv.IsClosed)
            {
                clipper.Execute(ref solution, offsetDistance / clipperTolerance);
            }
            else
            {
                clipper.Execute(ref solution, Math.Abs(offsetDistance / clipperTolerance));
            }

            var offsetedPolylines = solution.Childs
                .Select(ch => ch.Contour
                    .Select(ipt => IntPointToPoint3d(ipt, clipperTolerance)))
                .Select(opts => new Polyline(opts))
                .ToList();

            // если не замкнутая, нужно разомкнуть
            if (!inputCrv.IsClosed)
            {
                foreach (var poly in offsetedPolylines)
                {
                    var polylines = new List<List<Point3d>>();
                    var currentPolyline = new List<Point3d>();

                    for (int i = 0; i < poly.Count; i++)
                    {
                        currentPolyline.Add(poly[i]);

                        // находим центр отрезка
                        // позволяет в конце посмотреть последний и первый элемент
                        var nextPtIndex = (i + 1) % poly.Count;
                        var centerPt = (poly[i] + poly[nextPtIndex]) / 2;

                        var distanceToStart = centerPt.DistanceTo(inputCrv.PointAtStart);
                        var distanceToEnd = centerPt.DistanceTo(inputCrv.PointAtEnd);

                        // если центр отрезка совпадает с началом или концом кривой, то создаем новую currentPolyline
                        if (distanceToStart < comparedTolerance || distanceToEnd < comparedTolerance)
                        {
                            polylines.Add(currentPolyline);
                            currentPolyline = new List<Point3d>();
                        }
                    }

                    // добавляем оставшиеся точки
                    if (currentPolyline.Any())
                        polylines.Add(currentPolyline);

                    // если 3 полилинии, то разрыв был где-то посередине и нужно соединить первую и третью
                    if (polylines.Count > 2)
                    {
                        polylines[2].AddRange(polylines[0]);
                        polylines.RemoveAt(0);
                    }

                    // отсортировать направление кривых
                    polylines[0] = SortPolyline(polylines[0], cornerPts, offsetDistance, comparedTolerance);
                    polylines[1] = SortPolyline(polylines[1], cornerPts, offsetDistance, comparedTolerance);

                    // из двух полилиний нужно выбрать одну, например правее от исходной кривой
                    var originalVec = cornerPts[1] - cornerPts[0];
                    var testVec = polylines[0][0] - cornerPts[0];
                    var angle = Vector3d.VectorAngle(originalVec, testVec, Plane.WorldXY);
                    if (angle < Math.PI)
                    {
                        polylines.Reverse();
                    }

                    // Теперь справа polylines[0], а слева polylines[1]
                    if (offsetDistance < 0)
                    {
                        // офсет отрицательный, берем левую
                        offsettedCrvs.Add(new Polyline(polylines[1]).ToPolylineCurve());
                    }
                    else
                    {
                        // офсет положительный, берем правую
                        offsettedCrvs.Add(new Polyline(polylines[0]).ToPolylineCurve());
                    }
                }
            }
            else // closed
            {
                for (int i = 0; i < offsetedPolylines.Count; i++)
                {
                    var offsetedPolyline = offsetedPolylines[i];

                    var inputCrvOrientation = inputCrv.ClosedCurveOrientation();
                    var offsetedPolyOrientation = offsetedPolyline.ToNurbsCurve().ClosedCurveOrientation();

                    if (inputCrvOrientation != offsetedPolyOrientation)
                    {
                        offsetedPolyline.Reverse();
                    }

                    // Тут ещё не замкнутая кривая. Так её проще перестроить.
                    var adjustedPolyline = AdjustPolylineStart(inputCrv, offsetedPolyline, 1);

                    adjustedPolyline.Add(adjustedPolyline[0]);

                    offsettedCrvs.Add(adjustedPolyline.ToNurbsCurve());
                }
            }

            // закомментить для теста
            //offsettedCrvs = offsetedPolylines.Select(opts => new Polyline(opts).ToNurbsCurve() as Curve).ToList();

            return offsettedCrvs;
        }

        private static Polyline AdjustPolylineStart(Curve sourceCrv, Polyline targetPolyline, double angleDegTolerance)
        {
            var rebuildedPts = new List<Point3d>();

            Polyline sourcePolyline;

            sourceCrv.TryGetPolyline(out sourcePolyline);

            // Вектор старта исходной кривой
            var sourceStartVect = sourcePolyline[1] - sourcePolyline[0];
            sourceStartVect.Unitize();

            var targetPolylinePtCnt = targetPolyline.Count;

            // Из формулы основания равнобедренного треугольника.
            // Единичные вектора - его равные стороны.
            var vectorDeviationTolerance = 2 * Math.Cos((180 - angleDegTolerance) / 2);

            while (rebuildedPts.Count != targetPolylinePtCnt)
            {
                for (int i = 0; i < targetPolylinePtCnt; i++)
                {
                    if (rebuildedPts.Count == targetPolylinePtCnt)
                    {
                        break;
                    }

                    if (rebuildedPts.Count > 0)
                    {
                        rebuildedPts.Add(targetPolyline[i]);
                    }
                    else
                    {
                        // позволяет в конце посмотреть последний и первый элемент
                        var nextPtInd = (i + 1) % targetPolylinePtCnt;

                        var targetVector = targetPolyline[nextPtInd] - targetPolyline[i];
                        targetVector.Unitize();

                        var vectorDeviation = (sourceStartVect - targetVector).Length;

                        if (vectorDeviation < vectorDeviationTolerance)
                        {
                            rebuildedPts.Add(targetPolyline[i]);
                        }
                    }
                }
            }

            return new Polyline(rebuildedPts);
        }

        private static List<Point3d> SortPolyline(List<Point3d> targetPts, List<Point3d> sourcePts, double offset, double comparedTolerance)
        {
            // сравнение по точке закрывает случаи, при которых кривая вообще корректно отофсетилась
            // иные сравнения имеют смысл, если нужно флипать вообще любые кривые.
            var lengthFromStart = sourcePts[0].DistanceTo(targetPts[0]);

            var difference = lengthFromStart - Math.Abs(offset);

            if (difference > comparedTolerance)
            {
                targetPts.Reverse();
            }

            return targetPts;
        }

        private static Point3d IntPointToPoint3d(IntPoint ipt, double tolerance)
        {
            return new Point3d(ipt.X * tolerance, ipt.Y * tolerance, 0);
        }

        private static IntPoint Point3dToIntPoint(Point3d point3d, double tolerance = 1)
        {
            return new IntPoint((long)(point3d.X / tolerance), (long)(point3d.Y / tolerance));
        }

        /// <summary>
        /// Тестирует точку на попадание внутрь полигона.
        /// </summary>
        /// <param name="crv">Замкнутый контур. Если контур содержит кривые участки, они будут преобразованы в прямые.</param>
        /// <param name="point">Тестируемая точка.</param>
        /// <param name="tolerance">Точность подсчета. Например, 0.01 - округлять до 2 знаков после запятой.</param>
        /// <returns>0 - снаружи, 1 - на линии контура, 2 - внутри контура.</returns>
        public static int IsPointInside(Curve crv, Point3d point, double tolerance = 1)
        {
            var points = Util.GetCurveCorners(crv);
            var path = points.Select(pt => Point3dToIntPoint(pt, tolerance)).ToList();
            int result = Clipper.PointInPolygon(Point3dToIntPoint(point, tolerance), path);

            // clipper returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            switch (result)
            {
                case -1:
                    return 1;
                case 0:
                    return 0;
                case 1:
                    return 2;
                default:
                    throw new Exception($"Clipper returned {result}");
            }
        }
    }
}