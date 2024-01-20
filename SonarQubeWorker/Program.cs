using SonarQubeWorker;
using SonarQubeWorker.DataAccess;
using SonarQubeWorker.Interface;
using SonarQubeWorker.Mapper;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IAzureSQLDataAccess, AzureSQLDataAccess>();
        services.AddSingleton<ISonarQubeDataAccess, SonarQubeDataAccess>();
        services.AddSingleton<IAzureBlobDataAccess, AzureBlobDataAccess>();
        services.AddSingleton<IMapper, Mapper>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
