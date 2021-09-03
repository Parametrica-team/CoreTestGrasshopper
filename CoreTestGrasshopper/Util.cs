using System;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Rhino.DocObjects;
using System.IO;

namespace UrbanbotCore
{
    /// <summary>
    /// Статический класс для вспомогательных методов.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Меняет местами буквы в строке.
        /// </summary>
        /// <param name="levelCode">Строка.</param>
        /// <param name="a">Первая буква.</param>
        /// <param name="b">Вторая буква.</param>
        /// <returns>Строка с замененными символами.</returns>
        internal static string SwapChars(string levelCode, char a, char b)
        {
            var swap = levelCode.Select(x => x == a ? b : (x == b ? a : x)).ToArray();
            return new string(swap);
        }

        

        /// <summary>
        /// Создает плоскость с осью Y ориентированной по направлению линии.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="centerAtStart">true - центр плоскости в начале линии.
        /// false - на конце линии.</param>
        /// <returns></returns>
        public static Plane GetPlane(Line line, bool centerAtStart)
        {
            Plane plane;
            Vector3d vectorx = line.Direction;
            vectorx.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            if (centerAtStart)
                plane = new Plane(line.From, vectorx, line.Direction);
            else
                plane = new Plane(line.To, vectorx, line.Direction);
            return plane;
        }

        

        

        /// <summary>
        /// Находит полное имя блока в файлах-библиотеках.
        /// </summary>
        /// <param name="libraries">Пути к файлам-библиотекам с блоками.</param>
        /// <param name="blockNameMask">Маска для поиска имени блока. Например "Facade_1_*".</param>
        /// <returns>Первое попавшееся имя блока, которое соответвует маске.</returns>
        internal static string FindBlockName(List<string> libraries, string blockNameMask)
        {
            if (libraries == null || !libraries.Any() || string.IsNullOrEmpty(blockNameMask))
                return string.Empty;

            string blockNamePattern = WildcardToRegex(blockNameMask);
            return null;
        }

        /// <summary>
        /// Получение угловых точек из кривой.
        /// </summary>
        /// <param name="crv">Кривая.</param>
        /// <returns>Список угловых точек.</returns>
        public static List<Point3d> GetCurveCorners(Curve crv)
        {
            if (crv == null) return null;

            if (crv.TryGetPolyline(out Polyline polyline))
            {
                if (crv.IsClosed)
                {
                    return polyline.ToList().Take(polyline.Count - 1).ToList();
                }
                else
                {
                    return polyline.ToList();
                }
            }
            else
            {
                var pts = new List<Point3d>();
                double t0 = 0;

                crv.Domain = new Interval(0, 1);
                while (crv.GetNextDiscontinuity(Continuity.G2_locus_continuous, t0, 1, out _) && t0 < 1)
                {
                    crv.GetNextDiscontinuity(Continuity.G2_locus_continuous, t0, 1, out double t);
                    pts.Add(crv.PointAt(t));
                    t0 = t;
                }

                return pts;
            }
        }

        

        /// <summary>
        /// Превращает "wildcards" в регулярные выражения.
        /// </summary>
        /// <param name="pattern">Строка с * или ?.</param>
        /// <returns>Regex pattern.</returns>
        public static string WildcardToRegex(string pattern)
        {
            return "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".")
                       + "$";
        }

        

    }
}
