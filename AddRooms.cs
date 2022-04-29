using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddRoomsPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class AddRooms : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;


            try
            {
                //фильтруем все уровни в модели
                List<Level> levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .OfType<Level>()
                    .ToList();


                Transaction transaction = new Transaction(doc);
                transaction.Start("Расстановка помещений");

                List<ElementId> rooms = SetRooms(doc, levels); //расставляет помещения

                ShowDesiredTag(doc, rooms); //делает видимой нужную метку помещения

                transaction.Commit();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;

        }

        //метод расставляет помещения, изменяет номер помещения
        private static List<ElementId> SetRooms(Document doc, List<Level> levels)
        {
            List<ElementId> rooms = new List<ElementId>();
            int countLevel = 1;
            int countRoom = 1;

            //перебираем все уровни в модели  
            foreach (Level level in levels)

            {
                PhaseArray phases = doc.Phases;

                Phase createRoomsInPhase = phases.get_Item(phases.Size - 1);

                PlanTopology topology = doc.get_PlanTopology(level, createRoomsInPhase);

                PlanCircuitSet circuitSet = topology.Circuits;


                //перебираем все замкнутые контуры и создаем в них помещения
                foreach (PlanCircuit circuit in circuitSet)

                {

                    if (!circuit.IsRoomLocated)

                    {

                        UV ABC = circuit.GetPointInside();

                        Room room = doc.Create.NewRoom(null, circuit);

                        room.Number = $"{countLevel}_{countRoom}";
                        rooms.Add(room.Id);

                    }
                    countRoom++;
                }
                countRoom = 1;
                countLevel++;
            }
            return rooms;


        }
        //метод показывает желаемую метку помещения из загруженных в модель по умолчанию первую в списке
        //при необходимости изменить отображаемую метку 0 заменить 1,2,3....в newType = roomTagTypes.ToElements()[0]
        private static void ShowDesiredTag(Document doc, List<ElementId> rooms)
        {
            FilteredElementCollector roomTags = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .WhereElementIsNotElementType();
            FilteredElementCollector roomTagTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .WhereElementIsElementType();


            Element newType = roomTagTypes.ToElements()[0];


            foreach (RoomTag rt in roomTags.ToElements())
            {
                if (rooms.Contains(rt.TaggedLocalRoomId))
                {
                    rt.ChangeTypeId(newType.Id);
                }
            }
        }

    }
}
