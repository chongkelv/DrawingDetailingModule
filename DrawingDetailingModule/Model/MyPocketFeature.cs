using NXOpen.Features;
using NXOpen.UF;
using NXOpen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Model
{
    public abstract class MyPocketFeature : MyFeature
    {
        protected MyPocketFeature(Feature feature) : base(feature)
        {
            this.feature = feature;
            extrureFeat = feature as Extrude;
            sketchFeat = GetSketchFeat(feature);
        }

        public Extrude extrureFeat { get; set; }
        public Feature sketchFeat { get; set; }
        public UFSession ufs { get; set; }
        public TaggedObject SelectedBody { get; set; }

        public Feature GetSketchFeat(Feature feature)
        {
            // Prefer a real SketchFeature parent first - GetParents() order is not guaranteed to put
            // the actual profile sketch ahead of other curve-producing dependencies (e.g. an internal
            // ProjectCurve), so a single pass matching whichever comes first can grab the wrong one.
            foreach (NXObject ent in feature.GetParents())
            {
                if (ent is SketchFeature)
                {
                    return (SketchFeature)ent;
                }
            }

            foreach (NXObject ent in feature.GetParents())
            {
                if (ent is WaveSketch)
                {
                    return (WaveSketch)ent;
                }
                else if (ent is ProjectCurve)
                {
                    return (ProjectCurve)ent;
                }
            }

            return null;
        }

        public Point3d calculateMidPoint(Line line)
        {
            double midX = (line.StartPoint.X + line.EndPoint.X) / 2;
            double midY = (line.StartPoint.Y + line.EndPoint.Y) / 2;
            double midZ = (line.StartPoint.Z + line.EndPoint.Z) / 2;

            return new Point3d(midX, midY, midZ);
        }

        public double CalculateLineLength(Line line)
        {
            double length = Math.Sqrt(
                Math.Pow(line.EndPoint.X - line.StartPoint.X, 2) +
                Math.Pow(line.EndPoint.Y - line.StartPoint.Y, 2) +
                Math.Pow(line.EndPoint.Z - line.StartPoint.Z, 2)
                );

            return length;
        }

        public abstract List<Point3d> GenerateLocation();



    }
}
