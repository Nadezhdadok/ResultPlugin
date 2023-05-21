using Autodesk.Revit;
using Autodesk.Revit.Attributes;
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
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class Result : IExternalCommand
    {
        private static readonly Result Cancelled;
        private static readonly Result Failed;

        public static Result Succeeded { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
               
                Transaction documentTransaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Document");
                documentTransaction.Start();
                
                RoomsData data = new RoomsData(commandData);

                System.Windows.Forms.DialogResult result;

                
                using (AutoTagRoomsForm roomsTagForm = new AutoTagRoomsForm(data))
                {
                    result = roomsTagForm.ShowDialog();
                }

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    documentTransaction.Commit();
                    return Result.Succeeded;
                }
                else
                {
                    documentTransaction.RollBack();
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        Autodesk.Revit.UI.Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            throw new NotImplementedException();
        }
    }
}
