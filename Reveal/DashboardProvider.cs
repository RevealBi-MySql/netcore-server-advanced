using Reveal.Sdk;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/saving-dashboards/#example-implementing-save-with-irvdashboardprovider
    // The Dashboard Provider is optional.  By default, Reveal Loads & Saves dashboards to the /dashboards folder
    // Use this provider to customize your Load / Save locations, like an alternate folder on the file system
    // or to a database.  Use the UserContext information to determine the user, or other properties, that dictates 
    // where you need to load / save your requested dashboards
    // ****


    // ****
    // NOTE:  This is ignored of it is not set in the Builder in Program.cs --> //.AddDashboardProvider<DashboardProvider>()
    // ****
    public class DashboardProvider : IRVDashboardProvider
    {
        public Task<Dashboard> GetDashboardAsync(IRVUserContext userContext, string dashboardId)
        {
            // Only load dashboards from the MyDashboards folder
            var filePath = Path.Combine(Environment.CurrentDirectory, $"MyDashboards/{dashboardId}.rdash");
            var dashboard = new Dashboard(filePath);
            return Task.FromResult(dashboard);
        }

        public async Task SaveDashboardAsync(IRVUserContext userContext, string dashboardId, Dashboard dashboard)
        {
            // Only save dashboards to the MyDashboards folder.
            var filePath = Path.Combine(Environment.CurrentDirectory, $"MyDashboards/{dashboardId}.rdash");
            await dashboard.SaveToFileAsync(filePath);
        }
    }
}
