using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Dom;
using RevealSdk.Server.Reveal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddReveal(builder =>
{
    builder
        // ****
        // Set your license here or in a file in your home directory
        // https://help.revealbi.io/web/adding-license-key/
        //
        //.AddSettings(settings =>
        //{
        //    settings.License = "eyJhbGciOicCI6IkpXVCJ9.e";
        //})

        // ***
        // required 
        .AddAuthenticationProvider<AuthenticationProvider>()
        .AddDataSourceProvider<DataSourceProvider>()
        // optional 
        .AddUserContextProvider<UserContextProvider>()
        // optional
        .AddObjectFilter<ObjectFilterProvider>()
        // optional 
        //.AddDashboardProvider<DashboardProvider>()

        // Required.  Register any data source connector you are using.
        .DataSources.RegisterMySql();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
      builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
});

var app = builder.Build();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ****
// Optional endpoint to get the thumbnail of a dashboard
// ****
app.MapGet("/dashboards/{name}/thumbnail", async (string name) =>
{
    var path = "dashboards/" + name + ".rdash";
    if (File.Exists(path))
    {
        var dashboard = new Dashboard(path);
        var info = await dashboard.GetInfoAsync(Path.GetFileNameWithoutExtension(path));
        return TypedResults.Ok(info);
    }
    else
    {
        return Results.NotFound();
    }
});

// ****
// Optional endpoint to get the list of dashboard names
// ****
app.MapGet("/dashboards/names", () =>
{
    try
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Dashboards");
        var files = Directory.GetFiles(folderPath);
        Random rand = new();

        var fileNames = files.Select(file =>
        {
            try
            {
                return new DashboardNames
                {
                    DashboardFileName = Path.GetFileNameWithoutExtension(file),
                    DashboardTitle = RdashDocument.Load(file).Title
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Reading FileData {file}: {ex.Message}");
                return null;
            }
        }).Where(fileData => fileData != null).ToList();

        return Results.Ok(fileNames);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error Reading Directory : {ex.Message}");
        return Results.Problem("An unexpected error occurred while processing the request.");
    }

}).Produces<IEnumerable<DashboardNames>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// ***
// This is a helper class to store the dashboard names
// ***
public class DashboardNames
{
    public string? DashboardFileName { get; set; }
    public string? DashboardTitle { get; set; }
}