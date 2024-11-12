using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.MySql;
using System.Text.RegularExpressions;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/datasources/
    // https://help.revealbi.io/web/adding-data-sources/mysql/       
    // The DataSource Provider is required.  
    // Set you connection details in the ChangeDataSource, like Host & Database.  
    // If you are using data source items on the client, or you need to set specific queries based 
    // on incoming table requests, you will handle those requests in the ChangeDataSourceItem.
    // ****


    // ****
    // NOTE:  This must beset in the Builder in Program.cs --> .AddDataSourceProvider<DataSourceProvider>()
    // ****
    internal class DataSourceProvider : IRVDataSourceProvider
    {

        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public DataSourceProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***


        public Task<RVDashboardDataSource> ChangeDataSourceAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)
        {
            // *****
            // Check the request for the incoming data source
            // In a multi-tenant environment, you can use the user context properties to determine who is logged in
            // and what their connection information should be
            // you can also check the incoming dataSource type or id to set connection properties
            // *****

            if (dataSource is RVMySqlDataSource SqlDs)
            {
                SqlDs.Host = _config["Server:Host"];
                SqlDs.Database = _config["Server:Database"];
            }
            return Task.FromResult(dataSource);
        }

        public async Task<RVDataSourceItem> ChangeDataSourceItemAsync(IRVUserContext userContext, string dashboardId, RVDataSourceItem dataSourceItem)
        {
            if (dataSourceItem is not RVMySqlDataSourceItem sqlDsi) return dataSourceItem;

            // Update the data source
            await ChangeDataSourceAsync(userContext, sqlDsi.DataSource).ConfigureAwait(false);

            // ****
            // Check the UserContextProvider to see how "Role" is being created
            // ****
            bool isAdmin = userContext.Properties["Role"]?.ToString() == "Admin";

            // I am pulling in a list of tables that a user is allowed to see.
            // when looking at the DataSources dialog, I only want the 'USER' role to 
            // see specific tables
            var allowedTables = TableInfo.GetAllowedTables()
                                 .Where(t => t.COLUMN_NAME.Equals("customer_id", StringComparison.OrdinalIgnoreCase))
                                 .Select(t => t.TABLE_NAME)
                                 .ToHashSet(); 

            // store the userContext.UserId in a customerId variable
            int? customerId = GetValidCustomerId(userContext.UserId);

            switch (sqlDsi.Id)
            {
                // ****
                // Example of an ad-hoc query with a customerId parameter from the userContext
                // ****
                case "sp_customer_orders":
                    sqlDsi.Procedure = sqlDsi.Id;
                    sqlDsi.ProcedureParameters = new Dictionary<string, object> { { "customer", customerId ?? throw InvalidCustomerIdException() } };
                    break;
                // ****
                // Example of an ad-hoc query with a customerId parameter from the userContext
                // ****
                case "customer_orders":
                    sqlDsi.CustomQuery = GenerateSelectQuery(sqlDsi.Id, "customer_id", customerId);
                    break;
                // ****
                // Example of a parameterized stored procedure with a orderId parameter from the userContext
                // ****
                case "customer_orders_details":
                    string orderId = GetValidOrderId(userContext.Properties["OrderId"]?.ToString());
                    sqlDsi.CustomQuery = GenerateSelectQuery(sqlDsi.Id, "order_id", orderId);
                    break;
                // ****
                // This assumes the Data Sources Dialog table / object is selected
                // ****
                default:
                    if (allowedTables.Contains(sqlDsi.Table) && !isAdmin)
                    {
                        sqlDsi.CustomQuery = GenerateSelectQuery(sqlDsi.Table, "customer_id", customerId);
                    }
                    break;
            }

            return dataSourceItem;
        }


        // ****
        // Modify any of the code below to meet your specific needs
        // The code below is not part of the Reveal SDK, these are helpers to clean / validate parameters
        // specific to this sample code.  For example, ensuring the customerId & orderId are well formed, 
        // and ensuring that no invalid / illegal statements are passed in the header to the custom query
        // ****


        // Helper methods for common tasks

        // In my case, I know my customerId values range from 1 to 30 in the Northwind MySql database, I reject anything else
        private static int? GetValidCustomerId(string userId)
        {
            if (int.TryParse(userId, out int id) && id >= 1 && id <= 30) return id;
            return null;
        }

        // In my case, I know my order values range from 1 to 90 in the Northwind MySql database, I reject anything else
        private static string GetValidOrderId(string orderId)
        {
            if (!Regex.IsMatch(orderId, @"^\d{2}$")) throw InvalidOrderIdException();
            return EscapeSqlInput(orderId);
        }

        // ****
        // This a generic way to create a simple ad-hoc select statement with checks on the parameters and that it 
        // is only using the Select keyword for a read-only query
        // ****
        // In your case, you may have more complex queries, more parameters, you can create .customQueries however you'd like
        // ****
        private static string GenerateSelectQuery(string tableName, string columnName, object value)
        {
            string query = $"SELECT * FROM {tableName} WHERE {columnName} = '{value}'";
            if (!IsSelectOnly(query)) throw InvalidSqlQueryException();
            return query;
        }

        // Custom Exceptions for error handling / checking values
        private static Exception InvalidCustomerIdException() => new ArgumentException("Invalid CustomerID format. CustomerID must be an integer between 1 and 30.");
        private static Exception InvalidOrderIdException() => new ArgumentException("Invalid OrderId format. OrderId must be a 2-digit numeric value.");
        private static Exception InvalidSqlQueryException() => new ArgumentException("Invalid SQL query.");
        private static bool IsValidOrderId(string orderId) => Regex.IsMatch(orderId, @"^\d{2}$");
        private static string EscapeSqlInput(string input) => input.Replace("'", "''");

        // ****
        // This is usig the TransactSql.ScriptDom to parse & check the SQL Statement
        // ****
        public static bool IsSelectOnly(string sql)
        {
            TSql150Parser parser = new TSql150Parser(true);
            IList<ParseError> errors;
            TSqlFragment fragment;

            using (TextReader reader = new StringReader(sql))
            {
                fragment = parser.Parse(reader, out errors);
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.WriteLine($"Error: {error.Message}");
                }
                return false;
            }

            var visitor = new ReadOnlySelectVisitor();
            fragment.Accept(visitor);
            return visitor.IsReadOnly;
        }
    }
}