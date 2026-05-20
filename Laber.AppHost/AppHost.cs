var builder = DistributedApplication.CreateBuilder(args);

var dataDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "data"));
Directory.CreateDirectory(dataDirectory);
var databasePath = Path.Combine(dataDirectory, "laber.db");
var connectionString = $"Data Source={databasePath}";

var bouncer = builder.AddProject<Projects.Laber_Bouncer>("bouncer")
    .WithEnvironment("ConnectionStrings__laber", connectionString);

var api = builder.AddProject<Projects.Laber_Api>("api")
    .WithEnvironment("ConnectionStrings__laber", connectionString)
    .WithExternalHttpEndpoints();

builder.AddJavaScriptApp("frontend", "../Laber.Frontend", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
