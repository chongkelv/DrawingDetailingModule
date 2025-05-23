﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.Features;
using NXOpen.UF;

namespace DrawingDetailingModule.Model
{
    public class Counterbore2 : MyHoleFeature
    {
        public double CounterboreDiamter { get; set; }
        public double CounterDepth { get; set; }
        public List<SymbolicThread> SymbolicThreads { get; set; }
        public Counterbore2(Feature feature) : base(feature)
        {
            SymbolicThreads = new List<SymbolicThread>();
        }

        public Counterbore2()
        {
            SymbolicThreads = new List<SymbolicThread>();
        }

        private bool AskThruHole(HolePackage hole)
        {
            int edit = 0;
            string diameter1;
            string diameter2;
            string depth1;
            string depth2;
            string tip_angle;
            int thru_flag;
            TaggedObject taggedObject = NXOpen.Utilities.NXObjectManager.Get(hole.Tag);
            ufs.Modl.AskCBoreHoleParms(taggedObject.Tag, edit, out diameter1, out diameter2, out depth1, out depth2, out tip_angle, out thru_flag);
            
            return thru_flag == 1;
        }

        public override string ToString()
        {                        
            string cboreCondition = GetHoleCondition(feature);
            string depth = IsThruHole ? "THRU" : $"{HoleDepth:F1}";
            string description = "";
            if (cboreCondition == "")
            {
                description = $"{GetProcessAbbrevate()} <o>{CounterboreDiamter:F1} {FeatureFactory.DP} {CounterDepth:F1}, " +
                $"{FeatureFactory.DR} <o>{HoleDiameter:F1} {depth}";
            }
            else
            {
                description = $"{GetProcessAbbrevate()} <o>{CounterboreDiamter:F2} {FeatureFactory.DP} {CounterDepth:F2}, {cboreCondition}, " +
                $"{FeatureFactory.DR} <o>{HoleDiameter:F1} {depth}";
            }

            // Append the description
            if (SymbolicThreads.Count > 0)
            {                
                foreach (SymbolicThread t in SymbolicThreads)
                {
                    description += $", TAP M{t.MajorDiameter}x{t.Pitch} DP {t.Length:F1}";
                }
                
            }

            return description;
        }

        public override string GetProcessAbbrevate() => "C'BORE";

        public override void GetFeatureDetailInformation(Feature feature)
        {
            HolePackage holePackage = feature as HolePackage;
            HolePackageBuilder hpBuilder = workPart.Features.CreateHolePackageBuilder(holePackage);
            CounterboreDiamter = hpBuilder.ScrewClearanceCounterboreDiameter.Value;
            HoleDiameter = hpBuilder.ScrewClearanceHoleDiameter.Value;
            CounterDepth = hpBuilder.ScrewClearanceCounterboreDepth.Value;
            HoleDepth = hpBuilder.ScrewClearanceHoleDepth.Value;
            Quantity = points.Count;
            TaggedObject taggedObject = NXOpen.Utilities.NXObjectManager.Get(holePackage.Tag);
            IsThruHole = AskThruHole(holePackage);

            Feature[] allChilds = holePackage.GetAllChildren();
            SymbolicThreads = GetSymbolicThread(allChilds, SymbolicThreads);
        }        
    }

}
