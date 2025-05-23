﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.Features;
using NXOpen.UF;

namespace DrawingDetailingModule.Model
{
    public abstract class MyFeature : IFeature
    {
        protected Part workPart;
        protected UFSession ufs;
        protected List<Point3d> points;
        public Feature feature { get; set; }

        public int Quantity { get; set; }

        public MyFeature(Feature feature)
        {
            workPart = Session.GetSession().Parts.Work;
            points = new List<Point3d>();
            ufs = UFSession.GetUFSession();
            this.feature = feature;
        }

        public MyFeature()
        {
        }

        public abstract string GetProcessAbbrevate();

        public abstract void GetFeatureDetailInformation(Feature feature);

        public static string GetProcessType(Feature feature)
        {
            string input = feature.JournalIdentifier;
            var match = Regex.Match(input, @"^(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "";
        }

        public string ToString(List<Point2d> points)
        {
            StringBuilder stringBuilder = new StringBuilder();
            points.ForEach(x => stringBuilder.Append($"{x.X:F3},{x.Y:F3}\n"));
            return stringBuilder.ToString();
        }



        public string GetWCCondition(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();

            iterator.SetIncludeOnlyTitle(FeatureFactory.CUT_CONDITION);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.CUT_CONDITION, 0);
            }
            return @"S/C";
        }

        public string GetWCSlantCutAngle(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();

            iterator.SetIncludeOnlyTitle(FeatureFactory.SLANT_CUT);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.SLANT_CUT, 0);
            }
            return "1";
        }
        
        public string GetMILLCondition(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();

            iterator.SetIncludeOnlyTitle(FeatureFactory.CUT_CONDITION);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.CUT_CONDITION, 0);
            }
            return "TO SIZE";
        }
        public string GetTAPECondition(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();

            iterator.SetIncludeOnlyTitle(FeatureFactory.CUT_CONDITION);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.CUT_CONDITION, 0);
            }
            return "";
        }
        
        public string GetHoleCondition(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();

            iterator.SetIncludeOnlyTitle(FeatureFactory.CUT_CONDITION);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.CUT_CONDITION, 0);
            }
            return "";
        }

        public string GetWCOffset(Feature feature)
        {
            AttributeIterator iterator = workPart.CreateAttributeIterator();
            iterator.SetIncludeOnlyTitle(FeatureFactory.WC_OFFSET);
            if (feature.HasUserAttribute(iterator))
            {
                return feature.GetStringUserAttribute(FeatureFactory.WC_OFFSET, 0);
            }
            if (GetProcessType(feature).Equals(FeatureFactory.EXTRUDE))
            {
                return @"TO SIZE";
            }
            return "H7";
        }

        public double GetWCHoleSize(double holeDiameter)
        {
            if (holeDiameter >= 8.0)
            {
                return 5.2;
            }
            if (holeDiameter > 4.0)
            {
                return 3.0;
            }
            if (holeDiameter > 3.0)
            {
                return 2.0;
            }
            if (holeDiameter > 2.3)
            {
                return 1.5;
            }
            if (holeDiameter > 1.4)
            {
                return 1.0;
            }
            if (holeDiameter > 0.7)
            {
                return 0.5;
            }
            throw new ArgumentOutOfRangeException("The wire cut hole diamater is too small. Please verify!!");
        }
    }
}
