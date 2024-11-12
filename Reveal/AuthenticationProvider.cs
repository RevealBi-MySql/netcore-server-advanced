using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.MySql;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/authentication/ 
    // The Authentication Provider is required to set the credentials used
    // in the DataSourceProvider changeDataSourceAsync to authenticate to your database
    // ****


    // ****
    // NOTE:  This must beset in the Builder in Program.cs --> .AddAuthenticationProvider<AuthenticationProvider>()
    // ****
    public class AuthenticationProvider : IRVAuthenticationProvider
    {
        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public AuthenticationProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***

        public Task<IRVDataSourceCredential> ResolveCredentialsAsync(IRVUserContext userContext,
            RVDashboardDataSource dataSource)
        {
            // Create a userCredential object
            IRVDataSourceCredential? userCredential = null;

            // Check that the incoming request is for the expected data source type
            // You can check the data source type, or any of the information in your UserContext to
            // set credentials
            if (dataSource is RVMySqlDataSource)
            {
                // for SQL Server, add a username, password and optional domain
                // note these are just properties, you can set them from configuration, a key vault, a look up to 
                // database, etc.  They are stored in UserSecrets in my example
                userCredential = new RVUsernamePasswordDataSourceCredential(_config["Server:Username"], _config["Server:Password"]);
            }
            return Task.FromResult(userCredential);
        }
    }
}
