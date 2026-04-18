using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SuperpowersRevit.Services;
using SuperpowersRevit.UI;

namespace SuperpowersRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DrofusDataCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var loginWindow = new DrofusLoginWindow();
            bool? loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
                return Result.Cancelled;

            var service = new DrofusService(
                loginWindow.Host,
                loginWindow.Username,
                loginWindow.Password);

            // Verify credentials before opening the data window
            bool connected = service.TestConnectionAsync().GetAwaiter().GetResult();

            if (!connected)
            {
                TaskDialog.Show("Connection Failed",
                    "Could not connect to Drofus.\n" +
                    "Check the host URL and credentials and try again.");
                return Result.Failed;
            }

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var dataWindow = new DrofusDataWindow(service, doc);
            dataWindow.ShowDialog();

            return Result.Succeeded;
        }
    }
}
