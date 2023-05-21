using Autodesk.Revit;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResultPlugin
{
   
    public class RoomsData
    {
        UIApplication m_revit;
        List<Level> m_levels = new List<Level>();
        List<Room> m_rooms = new List<Room>();
        List<RoomTagType> m_roomTagTypes = new List<RoomTagType>();
        Dictionary<ElementId, List<RoomTag>> m_roomWithTags = new Dictionary<ElementId, List<RoomTag>>();

        
        public RoomsData(ExternalCommandData commandData)
        {
            m_revit = commandData.Application;
            GetRooms();
            GetRoomTagTypes();
            GetRoomWithTags();
        }
        public ReadOnlyCollection<Room> Rooms
        {
            get
            {
                return new ReadOnlyCollection<Room>(m_rooms);
            }
        }
        public ReadOnlyCollection<Level> Levels
        {
            get
            {
                return new ReadOnlyCollection<Level>(m_levels);
            }
        }
        public ReadOnlyCollection<RoomTagType> RoomTagTypes
        {
            get
            {
                return new ReadOnlyCollection<RoomTagType>(m_roomTagTypes);
            }
        }
        private void GetRooms()
        {
            Document document = m_revit.ActiveUIDocument.Document;
            foreach (PlanTopology planTopology in document.PlanTopologies)
            {
                if (planTopology.GetRoomIds().Count != 0 && planTopology.Level != null)
                {
                    m_levels.Add(planTopology.Level);
                    foreach (ElementId eid in planTopology.GetRoomIds())
                    {
                        Room tmpRoom = document.GetElement(eid) as Room;

                        if (document.GetElement(tmpRoom.LevelId) != null && m_roomWithTags.ContainsKey(tmpRoom.Id) == false)
                        {
                            m_rooms.Add(tmpRoom);
                            m_roomWithTags.Add(tmpRoom.Id, new List<RoomTag>());
                        }
                    }
                }
            }
        }
        private void GetRoomTagTypes()
        {
            FilteredElementCollector filteredElementCollector = new FilteredElementCollector(m_revit.ActiveUIDocument.Document);
            filteredElementCollector.OfClass(typeof(FamilySymbol));
            filteredElementCollector.OfCategory(BuiltInCategory.OST_RoomTags);
            m_roomTagTypes = filteredElementCollector.Cast<RoomTagType>().ToList<RoomTagType>();
        }
        private void GetRoomWithTags()
        {
            Document document = m_revit.ActiveUIDocument.Document;
            IEnumerable<RoomTag> roomTags = from elem in ((new FilteredElementCollector(document)).WherePasses(new RoomTagFilter()).ToElements())
                                            let roomTag = elem as RoomTag
                                            where (roomTag != null) && (roomTag.Room != null)
                                            select roomTag;

            foreach (RoomTag roomTag in roomTags)
            {
                if (m_roomWithTags.ContainsKey(roomTag.Room.Id))
                {
                    List<RoomTag> tmpList = m_roomWithTags[roomTag.Room.Id];
                    tmpList.Add(roomTag);
                }
            }
        }
        public void AutoTagRooms(Level level, RoomTagType tagType)
        {
            PlanTopology planTopology = m_revit.ActiveUIDocument.Document.get_PlanTopology(level);

            SubTransaction subTransaction = new SubTransaction(m_revit.ActiveUIDocument.Document);
            subTransaction.Start();
            foreach (ElementId eid in planTopology.GetRoomIds())
            {
                Room tmpRoom = m_revit.ActiveUIDocument.Document.GetElement(eid) as Room;

                if (m_revit.ActiveUIDocument.Document.GetElement(tmpRoom.LevelId) != null && tmpRoom.Location != null)
                {
                    LocationPoint locationPoint = tmpRoom.Location as LocationPoint;
                    UV point = new UV(locationPoint.Point.X, locationPoint.Point.Y);
                    RoomTag newTag = m_revit.ActiveUIDocument.Document.Create.NewRoomTag(new LinkElementId(tmpRoom.Id), point, null);
                    newTag.RoomTagType = tagType;

                    List<RoomTag> tagListInTheRoom = m_roomWithTags[newTag.Room.Id];
                    tagListInTheRoom.Add(newTag);
                }

            }
            subTransaction.Commit();
        }
        public int GetTagNumber(Room room, RoomTagType tagType)
        {
            int count = 0;
            List<RoomTag> tagListInTheRoom = m_roomWithTags[room.Id];
            foreach (RoomTag roomTag in tagListInTheRoom)
            {
                if (roomTag.RoomTagType.Id == tagType.Id)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
