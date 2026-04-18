using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SuperpowersRevit.Services;
using SuperpowersRevit.UI;

namespace SuperpowersRevit.Commands
{
    // No Revit model changes are made here — the transaction lives inside DrofusDataWindow.
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DrofusDataCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument? uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc is null)
            {
                message = "No active document.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;

            // Step 1 — collect credentials
            var loginWindow = new DrofusLoginWindow();
            if (loginWindow.ShowDialog() != true)
                return Result.Cancelled;

            // Step 2 — verify the connection before opening the heavy data window
            using var service = new DrofusService(
                loginWindow.Host,
                loginWindow.Username,
                loginWindow.Password);

            bool connected;
            try
            {
                connected = service.TestConnectionAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Connection Error", ex.Message);
                return Result.Failed;
            }

            if (!connected)
            {
                TaskDialog.Show("Connection Failed",
                    "Could not authenticate with Drofus.\n\n" +
                    "Check the host URL and your credentials, then try again.");
                return Result.Failed;
            }

            // Step 3 — show data window; the service is disposed when ShowDialog returns
            var dataWindow = new DrofusDataWindow(service, doc);
            dataWindow.ShowDialog();

            return Result.Succeeded;
        }
    }
}
