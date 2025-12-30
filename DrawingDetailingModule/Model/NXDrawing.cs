using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.UF;
using NXOpenUI;

using DrawingDetailingModule.Controller;
using NXOpen.Features;
using NXOpen.Annotations;
using System.Windows.Forms;
using NXOpen.CAE.Connections;

namespace DrawingDetailingModule.Model
{
    public enum FaceColor
    {
        WCSP = 55,
        MILL = 211,
        WC = 181,
    }
    public class NXDrawing
    {
        Session session;
        Part workPart;
        UI ui;
        UFSession ufs;
        Controller.Control control;

        List<TaggedObject> selectedBody;
        List<Point3d> locatedPoint;
        public List<TaggedObject> SelectedBody
        {
            get { return selectedBody; }
            set { selectedBody = value; }
        }
        public List<Point3d> LocatedPoint
        {
            get { return locatedPoint; }
            set { locatedPoint = value; }
        }
        public bool IsFaceSelected => selectedBody.Count > 0;
        public bool IsPointLocated => locatedPoint.Count > 0;
        public string PartName { get; }

        public NXDrawing()
        {
        }

        public NXDrawing(Controller.Control control)
        {
            session = Session.GetSession();
            ufs = UFSession.GetUFSession();
            workPart = session.Parts.Work;
            ui = UI.GetUI();

            selectedBody = new List<TaggedObject>();
            locatedPoint = new List<Point3d>();

            this.control = control;

            PartName = workPart.Name;
        }

        public List<TaggedObject> SelectBody()
        {
            Selection selManager = ui.SelectionManager;
            TaggedObject selectedObject;
            string message = "Please choose a solid to begin the Detailing process.";
            string title = "Solid Selection";
            var scope = NXOpen.Selection.SelectionScope.WorkPartAndOccurrence;
            var action = NXOpen.Selection.SelectionAction.ClearAndEnableSpecific;
            bool keepHighlighted = false;
            bool includeFeature = false;
            Point3d cursor;

            var dimMask = new Selection.MaskTriple(NXOpen.UF.UFConstants.UF_solid_type, UFConstants.UF_solid_body_subtype, UFConstants.UF_UI_SEL_FEATURE_BODY);
            Selection.MaskTriple[] maskArray = new Selection.MaskTriple[] { dimMask };
            var response = selManager.SelectTaggedObject(message, title, scope, action, includeFeature, keepHighlighted, maskArray, out selectedObject, out cursor);

            if (response == NXOpen.Selection.Response.Cancel && response == NXOpen.Selection.Response.Back)
            {
                return null;
            }

            List<TaggedObject> result = new List<TaggedObject>();
            result.Add(selectedObject);
            return result;
        }

        public List<Point3d> SelectScreenPosition()
        {
            Selection selManager = ui.SelectionManager;
            string message = "Indicate the position to place the description table.";
            NXOpen.View theView;
            Point3d pt;

            var resp = selManager.SelectScreenPosition(message, out theView, out pt);
            if (resp != Selection.DialogResponse.Pick)
            {
                return null;
            }

            List<Point3d> result = new List<Point3d>();
            result.Add(pt);
            return result;
        }

        public int GetDimensionTextSize()
        {
            PmiPreferencesBuilder pmiPreferencesBuilder;
            pmiPreferencesBuilder = workPart.PmiSettingsManager.CreatePreferencesBuilder();
            int textSize = (int)pmiPreferencesBuilder.AnnotationStyle.LetteringStyle.DimensionTextSize;
            pmiPreferencesBuilder.Destroy();
            return textSize;
        }

        public TableSection CreateTable(Point3d insertionPoint, List<MachiningDescriptionModel> descriptionModels)
        {
            int numOfColumns = 3, numOfRows = descriptionModels.Count + 1;
                        
            PmiTableSection nullPmiTableSection = null;
            PmiTableBuilder pmiTableBuilder;
            pmiTableBuilder = workPart.Annotations.PmiTableSections.CreatePmiTableBuilder(nullPmiTableSection);

            pmiTableBuilder.NumberOfColumns = numOfColumns;
            pmiTableBuilder.NumberOfRows = numOfRows;
            pmiTableBuilder.ColumnWidth = 200.0;

            pmiTableBuilder.Origin.OriginPoint = insertionPoint;

            NXObject tableObj = pmiTableBuilder.Commit(); 

            TableSection table = tableObj as TableSection;

            double textSize = GetCurrentTextSize();
            double columnWidth1 = Math.Ceiling(textSize * 5);
            double columnWidth2 = Math.Ceiling(textSize * 20);

            table.SetName("Machining Table");

            Tag cell = Tag.Null;
            Tag row = Tag.Null;
            Tag column = Tag.Null;
            Tag tabNote = Tag.Null;

            ufs.Tabnot.AskTabularNoteOfSection(table.Tag, out tabNote);
            ufs.Tabnot.AskNthRow(tabNote, 0, out row);
            ufs.Tabnot.AskNthColumn(tabNote, 0, out column);

            ufs.Tabnot.AskCellAtRowCol(row, column, out cell);
            ufs.Tabnot.SetCellText(cell, "HOLE");
            ufs.Tabnot.SetColumnWidth(column, columnWidth1);

            ufs.Tabnot.AskNthColumn(tabNote, 1, out column);
            ufs.Tabnot.AskCellAtRowCol(row, column, out cell);
            ufs.Tabnot.SetCellText(cell, "DESCRIPTION");
            ufs.Tabnot.SetColumnWidth(column, columnWidth2);

            ufs.Tabnot.AskNthColumn(tabNote, 2, out column);
            ufs.Tabnot.AskCellAtRowCol(row, column, out cell);
            ufs.Tabnot.SetCellText(cell, "QTY");
            ufs.Tabnot.SetColumnWidth(column, columnWidth1);

            int numOfColumn = 3;        

            for (int i = 0; i < descriptionModels.Count; i++)
            {
                ufs.Tabnot.AskNthRow(tabNote, i + 1, out row);

                PlaceAnnotation(descriptionModels[i].Points, NumberToAlphabet(i));
            
                for (int j = 0; j < numOfColumn; j++)
                {
                    ufs.Tabnot.AskNthColumn(tabNote, j, out column);
                    ufs.Tabnot.AskCellAtRowCol(row, column, out cell);
                    if (j == 0)
                    {
                        ufs.Tabnot.SetCellText(cell, NumberToAlphabet(i));
                    }
                    else if (j == 1)
                    {
                        ufs.Tabnot.SetCellText(cell, descriptionModels[i].Description);
                    }
                    else
                    {
                        ufs.Tabnot.SetCellText(cell, descriptionModels[i].Quantity.ToString());
                    }
                }
            }

            pmiTableBuilder.Destroy();
            return table;
        }

        private void PlaceAnnotation(List<Point3d> points, string alphabet)
        {
            string[] text_string = new string[1];
            text_string[0] = alphabet;

            foreach (Point3d point in points)
            {
                AnnotationManager manager = workPart.Annotations;
                Note note = manager.CreateNote(text_string, point, AxisOrientation.Horizontal, null, null);
            }
        }        

        public string NumberToAlphabet (int number)
        {
            StringBuilder result = new StringBuilder();
            int baseValue = 24; // 26 letters - 2 skipped letters (I and O)
            while (number >= 0)
            {
                int remainder = number % baseValue;
                char letter = (char)(remainder + 'A');

                // Adjust for skipped letters
                if (letter >= 'I') letter++;
                if (letter >= 'O') letter++;

                result.Insert(0, letter);
                number = (number / baseValue) - 1;
            }
            return result.ToString();
        }

        public void SetTextSize(double currentTextSize)
        {
            PmiPreferencesBuilder pmiPreferencesBuilder;
            pmiPreferencesBuilder = workPart.PmiSettingsManager.CreatePreferencesBuilder();
            pmiPreferencesBuilder.AnnotationStyle.LetteringStyle.GeneralTextSize = currentTextSize;
            pmiPreferencesBuilder.Commit();
        }

        public double GetCurrentTextSize()
        {
            PmiPreferencesBuilder pmiPreferencesBuilder;
            pmiPreferencesBuilder = workPart.PmiSettingsManager.CreatePreferencesBuilder();
            return pmiPreferencesBuilder.AnnotationStyle.LetteringStyle.GeneralTextSize;
        }

        public List<MachiningDescriptionModel> IterateFeatures()
        {
            var featureCollection = workPart.Features;
            FeatureFactory factory = new FeatureFactory();

            List<MachiningDescriptionModel> descModels = new List<MachiningDescriptionModel>();
            MachiningDescriptionModel descModel;
            MachiningDescriptionModel tmpDescModel;
            foreach (Feature feature in featureCollection)
            {
                if (feature.GetType() == typeof(NXOpen.Features.HolePackage))
                {
                    descModel = ProcessHolePackage(factory, feature);
                    tmpDescModel = ProcessMachiningDescription(descModels, descModel);
                    if (tmpDescModel != null)
                    {
                        descModels.Add(tmpDescModel);
                    }

                }
                else if (feature.GetType() == typeof(NXOpen.Features.Extrude))
                {
                    AttributeIterator machiningAttIterator = workPart.CreateAttributeIterator();
                    machiningAttIterator.SetIncludeOnlyTitle(FeatureFactory.TYPE);

                    if (!feature.HasUserAttribute(machiningAttIterator))
                    {
                        continue;
                    }

                    if (GetTypeFromAttr(machiningAttIterator, feature).Equals(FeatureFactory.WC))
                    {
                        descModel = ProcessWCFeat(factory, feature);
                        tmpDescModel = ProcessMachiningDescription(descModels, descModel);
                        if (tmpDescModel != null)
                        {
                            descModels.Add(tmpDescModel);
                        }
                    }
                    else if (GetTypeFromAttr(machiningAttIterator, feature).Equals(FeatureFactory.MILL))
                    {
                        descModel = ProcessMillFeat(factory, feature);
                        tmpDescModel = ProcessMachiningDescription(descModels, descModel);
                        if (tmpDescModel != null)
                        {
                            descModels.Add(tmpDescModel);
                        }
                    }
                }
            }

            return descModels;
        }

        private MachiningDescriptionModel ProcessMachiningDescription(List<MachiningDescriptionModel> descModels, MachiningDescriptionModel descModel)
        {
            if (MachiningDescriptionModel.IsDescriptionSame(descModels, descModel))
            {
                MachiningDescriptionModel.SumUpModelQuantity(descModels, descModel);
                MachiningDescriptionModel.AppendModelPoints(descModels, descModel);
            }
            else
            {
                return descModel;
            }
            return null;
        }

        private MachiningDescriptionModel ProcessMillFeat(FeatureFactory factory, Feature feature)
        {
            MillPocketFeature millFeat = factory.GetFeature(feature) as MillPocketFeature;
            millFeat.ufs = ufs;
            millFeat.GetFeatureDetailInformation(feature);
            millFeat.SelectedBody = selectedBody[0];

            string description = millFeat.ToString();

            List<Point3d> points = millFeat.GenerateLocation();

            AskBoundingBox boundingBox = new AskBoundingBox(ufs, selectedBody[0].Tag);
            double[] point = new double[3] { points[0].X, points[0].Y, points[0].Z };
            string height = boundingBox.GetThickness();
            double[] direction = boundingBox.AskDirection(point, AXIS.Z);

            return new MachiningDescriptionModel(description, points.Count, points, millFeat.GetProcessAbbrevate(), direction, height);
        }

        private MachiningDescriptionModel ProcessWCFeat(FeatureFactory factory, Feature feature)
        {
            WCPocketFeature wcFeat = factory.GetFeature(feature) as WCPocketFeature;
            wcFeat.ufs = ufs;
            wcFeat.GetFeatureDetailInformation(feature);
            wcFeat.SelectedBody = selectedBody[0];

            string description = wcFeat.ToString();

            List<Point3d> points = wcFeat.GenerateLocation();                        

            AskBoundingBox boundingBox = new AskBoundingBox(ufs, selectedBody[0].Tag);
            double[] point = new double[3] { points[0].X, points[0].Y, points[0].Z };
            string height = boundingBox.GetThickness();
            double[] direction = boundingBox.AskDirection(point, AXIS.Z);

            return new MachiningDescriptionModel(description, points.Count, points, wcFeat.GetProcessAbbrevate(), direction, height);
        }

        private MachiningDescriptionModel ProcessHolePackage(FeatureFactory factory, Feature feature)
        {
            MachiningDescriptionModel descModel;
            NXOpen.Features.HolePackage holePackage = feature as NXOpen.Features.HolePackage;
            MyHoleFeature holeFeat = factory.GetFeature(feature) as MyHoleFeature;
            holeFeat.GetFeatureDetailInformation(feature);
            string description = holeFeat.ToString();           

            AskBoundingBox boundingBox = new AskBoundingBox(ufs, selectedBody[0].Tag);

            List<Point3d> points = holeFeat.GetLocation();
            List<Point3d> outPoints = new List<Point3d>();
            double[] point = new double[3] { points[0].X, points[0].Y, points[0].Z };
            string height = boundingBox.GetThickness();

            descModel = new MachiningDescriptionModel(description, points.Count, points, holeFeat.GetProcessAbbrevate(), boundingBox.AskDirection(point, AXIS.Z), height);

            return descModel;
        }       

        public void GenerateWCStartPoints(List<MachiningDescriptionModel> descriptionModels)
        {
            foreach (MachiningDescriptionModel descriptionModel in descriptionModels)
            {
                string abbr = descriptionModel.Abbrevate;
                double wcspDiameter = descriptionModel.GetWCStartPointDiameter(descriptionModel.Description);
                double wcDiameter = descriptionModel.GetWCHoleDiameter(descriptionModel.Description);
                string height = descriptionModel.Height;
                double[] direction = descriptionModel.Direction;
                double ratio = wcDiameter / wcspDiameter;
                if (abbr != FeatureFactory.WC)
                {
                    continue;
                }

                foreach (var pt in descriptionModel.Points)
                {
                    Point3d point = pt;
                    if (ratio >= 3.0)
                    {
                        double moveDistance = (wcDiameter / 2) - Math.Abs(wcspDiameter);
                        point = new Point3d(pt.X - moveDistance, pt.Y, pt.Z);
                    }
                    CreateWCStartPoint(workPart, point, height, wcspDiameter, direction);
                }
            }
        }

        public Cylinder CreateWCStartPoint(Part workPart, Point3d basePoint, string height, double diameter, double[] direction)
        {
            if (diameter <= 0)
            {
                throw new ArgumentNullException("The diameter can not <= zero in CreateWCStartPoint method.");
            }

            NXOpen.Features.CylinderBuilder cylinderBuilder = workPart.Features.CreateCylinderBuilder(null);
            cylinderBuilder.Origin = basePoint;
            cylinderBuilder.Height.RightHandSide = height;
            cylinderBuilder.Diameter.RightHandSide = diameter.ToString();
            Vector3d cylinderAxis = new Vector3d(direction[0], direction[1], direction[2]);
            cylinderBuilder.Direction = cylinderAxis;

            Cylinder createdCylinder = cylinderBuilder.CommitFeature() as Cylinder;

            DisplayableObject[] displayableObjects = createdCylinder.GetBodies();
            DisplayModification displayModification = session.DisplayManager.NewDisplayModification();
            displayModification.ApplyToAllFaces = true;
            displayModification.NewColor = 55;
            displayModification.Apply(displayableObjects);

            displayModification.Dispose();

            return createdCylinder;
        }

        public bool IsDrawingOpen()
        {
            string title = "No active drawing";
            string message = "You accidentally launched the BOM command by mistake and ";
            NXMessageBox.DialogType msgboxType = NXMessageBox.DialogType.Warning;

            Part displayPart = session.Parts.Display;
            bool isNoCompnent = displayPart == null;

            if (isNoCompnent)
            {
                message += " you are not open any drawings yet! ;-)";
                ShowMessageBox(message, title, msgboxType);
                return false;
            }

            return true;
        }

        public static void ShowMessageBox(string message, string title, NXMessageBox.DialogType msgboxType)
        {
            UI ui = UI.GetUI();
            ui.NXMessageBox.Show(title, msgboxType, message);
        }

        private string GetTypeFromAttr(AttributeIterator attItr, Feature feature)
        {
            if (feature.HasUserAttribute(attItr))
            {
                return feature.GetStringUserAttribute(FeatureFactory.TYPE, 0);
            }
            return "";
        }

    }
}
