using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.MySql;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/user-context/#using-the-user-context-in-the-objectfilterprovider
    // ObjectFilter Provider is optional.
    // The Filter functions allow you to control the data sources dialog  on the client.
    // ****


    // ****
    // NOTE:  This is ignored of it is not set in the Builder in Program.cs --> //.AddObjectFilter<ObjectFilterProvider>()
    // ****
    public class ObjectFilterProvider : IRVObjectFilter
    {
        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public ObjectFilterProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***

        public Task<bool> Filter(IRVUserContext userContext, RVDashboardDataSource dataSource) // this is a filter that goes through all databases on the server
        {
            // ****
            // To ensure there is no rogue request for a database that I don't want, you can 
            // use the Filter on the dataSource to validate only the database you expect is being accessed
            // is actually being accessed
            // ****
            var allowedList = new List<string>() { _config["""Server:Database"""] }; //here we indicate a list of databases with which we want to work

            if (dataSource != null)
            {
                if (dataSource is RVMySqlDataSource dataSQL) // we consult if it is a SQL DB and cast the generic data source to SQL to be able to access its attributes
                {
                    if (allowedList.Contains(dataSQL.Database)) return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        public Task<bool> Filter(IRVUserContext userContext, RVDataSourceItem dataSourceItem)
        {
            // ****
            // In the scenario I am using the Roles that are set up in the UserContext Provider to check the 
            // Role property to restrict what is displayed in the Data Sources.
            // If the logged in user is an Admin role, they see all the Tables, Views, Sprocs, if not, 
            // they will only see the 'All Orders' and 'Invoices' tables.
            // ****
            if (userContext?.Properties != null && dataSourceItem is RVMySqlDataSourceItem dataSQLItem)
            {
                if (userContext.Properties.TryGetValue("Role", out var roleObj) &&
                    roleObj?.ToString()?.ToLower() == "user")
                {
                    // ****
                    // Hardcoding these, however, you can pull these in from JSON, configuration or a database
                    // ****
                    var allowedItems = new HashSet<string> { "customer_orders", "customer_orders_details"};

                    if ((dataSQLItem.Table != null && !allowedItems.Contains(dataSQLItem.Table)) ||
                        (dataSQLItem.Procedure != null && !allowedItems.Contains(dataSQLItem.Procedure)))
                    {
                        return Task.FromResult(false);
                    }
                }
            }
            return Task.FromResult(true);
        }
    }
}
