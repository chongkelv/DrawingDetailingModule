using NXOpen;
using NXOpen.UF;
using NXOpen.Features;
using System;
using System.Collections.Generic;
using System.Text;
using NXOpen.GeometricUtilities;

namespace DrawingDetailingModule.Model
{
    public class WCPocketFeature : MyPocketFeature
    {
        // Distance the WC start point is inset from the profile boundary, back toward the profile center.
        private const double WC_START_POINT_OFFSET_MM = 3.0;

        // Number of points sampled along non-straight profile curves (arcs, circles, splines) when
        // approximating them as a polyline for the boundary-crossing search. Lines only ever need 2.
        private const int CURVE_SAMPLE_COUNT = 32;

        List<Point3d> wcspBasePoint;

        public double WCStartPointDiameter { get; set; }
        public double Depth { get; set; }
        public bool IsThru { get; set; }

        public WCPocketFeature(Feature feature) : base(feature) { }

        public override string GetProcessAbbrevate() => FeatureFactory.WC;

        /// <summary>
        /// Computes the WC start point by: finding the profile's local bounding-box center (in the
        /// sketch's own plane, so it works for sketches on angled/tilted planes), casting a ray from
        /// that center along the sketch's local +X axis, finding the nearest profile-boundary crossing,
        /// then offsetting that crossing back toward the center by <see cref="WC_START_POINT_OFFSET_MM"/>.
        /// Works for any curve type (line/arc/circle/spline) since the profile is approximated as a
        /// polyline via generic curve evaluation, rather than requiring a straight line segment.
        /// </summary>
        public override List<Point3d> GenerateLocation()
        {
            if (sketchFeat == null)
            {
                throw new Exception("Wirecut Feature Error: The system cannot process the wirecut operation. Please create the Wirecut Profile using a sketch first!");
            }

            SketchFeature realSketchFeat = sketchFeat as SketchFeature;
            if (realSketchFeat == null)
            {
                throw new Exception($"Wirecut Feature Error: The WC profile's parent \"{sketchFeat.GetFeatureName()}\" is not a sketch (found {sketchFeat.GetType().Name}). The WC start point algorithm requires a true sketch profile.");
            }

            string sketchName = sketchFeat.GetFeatureName();
            NXDrawing.WriteToListingWindow($"WCPocketFeature: resolved profile sketch = \"{sketchName}\"");

            Sketch sketch = realSketchFeat.Sketch;
            Point3d origin = sketch.Origin;
            Matrix3x3 orientation = sketch.Orientation.Element;
            double[] xAxis = { orientation.Xx, orientation.Xy, orientation.Xz };
            double[] yAxis = { orientation.Yx, orientation.Yy, orientation.Yz };

            List<double[]> segmentsUV = new List<double[]>();
            double minU = double.MaxValue, maxU = double.MinValue;
            double minV = double.MaxValue, maxV = double.MinValue;

            foreach (NXObject ent in sketchFeat.GetEntities())
            {
                if (!(ent is Curve))
                {
                    continue;
                }

                Curve curve = (Curve)ent;
                List<double[]> uvPoints = SampleCurveUV(curve, origin, xAxis, yAxis);

                foreach (double[] uv in uvPoints)
                {
                    if (uv[0] < minU) minU = uv[0];
                    if (uv[0] > maxU) maxU = uv[0];
                    if (uv[1] < minV) minV = uv[1];
                    if (uv[1] > maxV) maxV = uv[1];
                }

                for (int i = 0; i < uvPoints.Count - 1; i++)
                {
                    segmentsUV.Add(new double[] { uvPoints[i][0], uvPoints[i][1], uvPoints[i + 1][0], uvPoints[i + 1][1] });
                }
            }

            if (segmentsUV.Count == 0)
            {
                throw new Exception($"Wirecut Feature Error: Sketch \"{sketchName}\" has no usable profile curves.");
            }

            double centerU = (minU + maxU) / 2.0;
            double centerV = (minV + maxV) / 2.0;

            NXDrawing.WriteToListingWindow($"WCPocketFeature: profile center (local uv) = ({centerU:F3}, {centerV:F3})");

            double nearestU = double.MaxValue;
            bool found = false;

            foreach (double[] seg in segmentsUV)
            {
                double u1 = seg[0], v1 = seg[1], u2 = seg[2], v2 = seg[3];

                bool crosses = (v1 - centerV > 0) != (v2 - centerV > 0);
                if (!crosses)
                {
                    continue;
                }

                double t = (centerV - v1) / (v2 - v1);
                double uCross = u1 + t * (u2 - u1);

                if (uCross > centerU && uCross < nearestU)
                {
                    nearestU = uCross;
                    found = true;
                }
            }

            if (!found)
            {
                throw new Exception($"Wirecut Feature Error: Could not find a profile boundary crossing to the right of the profile center in sketch \"{sketchName}\". Please check the profile geometry.");
            }

            double crossingDistance = nearestU - centerU;
            if (crossingDistance < WC_START_POINT_OFFSET_MM)
            {
                throw new Exception($"Wirecut Feature Error: Sketch \"{sketchName}\" is narrower ({crossingDistance:F2}mm) than the required {WC_START_POINT_OFFSET_MM}mm WC start point offset at its center. Please widen the profile or adjust its geometry.");
            }

            double finalU = nearestU - WC_START_POINT_OFFSET_MM;
            double finalV = centerV;

            Point3d finalPoint = new Point3d(
                origin.X + finalU * xAxis[0] + finalV * yAxis[0],
                origin.Y + finalU * xAxis[1] + finalV * yAxis[1],
                origin.Z + finalU * xAxis[2] + finalV * yAxis[2]
            );

            NXDrawing.WriteToListingWindow($"WCPocketFeature: WC start point = ({finalPoint.X:F3}, {finalPoint.Y:F3}, {finalPoint.Z:F3})");

            // A valid WC start point sits in the pocket cavity the wirecut removes, not in solid
            // material - so it must land OUTSIDE the part body, not inside it.
            AskBoundingBox askBounding = new AskBoundingBox(ufs, SelectedBody.Tag);
            if (askBounding.IsPointContainInBoundary(finalPoint, SelectedBody.Tag))
            {
                throw new Exception($"Wirecut Feature Error: Computed WC start point for sketch \"{sketchName}\" falls inside solid material rather than the wirecut pocket cavity. Please verify the profile geometry.");
            }

            List<Point3d> points = new List<Point3d> { finalPoint };
            wcspBasePoint = points;

            return points;
        }

        /// <summary>
        /// Samples points along a curve at evenly-spaced parameter steps via the generic UF curve
        /// evaluator, then projects each into the sketch's local (u, v) plane coordinates. Works
        /// uniformly for lines, arcs, circles, and splines without per-type geometry math.
        /// </summary>
        private List<double[]> SampleCurveUV(Curve curve, Point3d origin, double[] xAxis, double[] yAxis)
        {
            List<double[]> result = new List<double[]>();

            IntPtr evaluator;
            ufs.Eval.Initialize(curve.Tag, out evaluator);
            try
            {
                bool isLine;
                ufs.Eval.IsLine(evaluator, out isLine);

                double[] limits = new double[2];
                ufs.Eval.AskLimits(evaluator, limits);

                int sampleCount = isLine ? 2 : CURVE_SAMPLE_COUNT;

                for (int i = 0; i < sampleCount; i++)
                {
                    double parm = limits[0] + (limits[1] - limits[0]) * i / (sampleCount - 1);

                    double[] pt = new double[3];
                    double[] deriv = new double[3];
                    ufs.Eval.Evaluate(evaluator, 0, parm, pt, deriv);

                    double dx = pt[0] - origin.X;
                    double dy = pt[1] - origin.Y;
                    double dz = pt[2] - origin.Z;

                    double u = dx * xAxis[0] + dy * xAxis[1] + dz * xAxis[2];
                    double v = dx * yAxis[0] + dy * yAxis[1] + dz * yAxis[2];

                    result.Add(new double[] { u, v });
                }
            }
            finally
            {
                ufs.Eval.Free(evaluator);
            }

            return result;
        }

        public override string ToString()
        {
            string wcType = GetWCCondition(feature);
            string slantCutAnlge = GetWCSlantCutAngle(feature);
            wcType = ProcessWCType(wcType, slantCutAnlge);
            string wcOffset = GetWCOffset(feature);
            WCStartPointDiameter = GetWCStartPointDiam(35.0);

            string description = $"PROF {GetProcessAbbrevate()} {wcOffset} {wcType} (<o>{WCStartPointDiameter:F1} {FeatureFactory.WC_SP})";

            return description;
        }

        private string ProcessWCType(string wcType, string slantCutAngle)
        {
            if(!wcType.Equals("T/C", StringComparison.OrdinalIgnoreCase))
            {
                return wcType;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(wcType);
            sb.Append($" (L={Depth}, T={slantCutAngle}<$s>)");
            return sb.ToString();
        }

        public double GetWCStartPointDiam(double plateThickness)
        {
            if (plateThickness < 50.0)
            {
                return 3.0;
            }
            return 5.2;
        }

        public override void GetFeatureDetailInformation(Feature feature)
        {
            ExtrudeBuilder extrudeBuilder = workPart.Features.CreateExtrudeBuilder(feature);
            var trimType = extrudeBuilder.Limits.EndExtend.TrimType;
            if (trimType is NXOpen.GeometricUtilities.Extend.ExtendType.ThroughAll)
            {
                IsThru = true;
            }
            else if (trimType is Extend.ExtendType.Value)
            {
                Depth = extrudeBuilder.Limits.EndExtend.Value.Value;
            }
            Quantity = 1;
        }
    }
}
