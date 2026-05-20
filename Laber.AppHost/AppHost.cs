var builder = DistributedApplication.CreateBuilder(args);

var dataDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "data"));
Directory.CreateDirectory(dataDirectory);

var api = builder.AddProject<Projects.Laber_Api>("api")
    .WithEnvironment("MessageStore__DataDirectory", dataDirectory)
    .WithExternalHttpEndpoints();

builder.AddJavaScriptApp("frontend", "../Laber.Frontend", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
