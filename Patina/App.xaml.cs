using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Patina.Core;
using Patina.Core.Queries;
using Patina.Core.Storage;
using Patina.Services;
using Uno.Resizetizer;

namespace Patina;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure((host, window) => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>())
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    services.TryAddSingleton<IClock, SystemClock>();
                    services.TryAddSingleton<IDocumentFile, LocalFolderDocumentFile>();
                    services.TryAddSingleton<PatinaStore>();
                    services.TryAddSingleton<IAppSettings, AppSettings>();
                    services.TryAddSingleton<IPhotoService>(_ => new PhotoService(window));
                    services.TryAddSingleton<IDataTransferService>(sp => new DataTransferService(window, sp.GetRequiredService<IClock>()));
                })
                .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes));

        MainWindow = builder.Window;

#if DEBUG
        // Headless verification runs set APP_NO_HOTDESIGN=1: UseStudio() blocks window creation when no DevServer is reachable.
        if (Environment.GetEnvironmentVariable("APP_NO_HOTDESIGN") != "1")
        {
            MainWindow.UseStudio();
        }
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>(initialNavigate: async (services, navigator) =>
        {
            Motion.ReduceMotion = services.GetRequiredService<IAppSettings>().ReduceMotion;
            await navigator.NavigateViewModelAsync<MainModel>(this);
        });
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<MainPage, MainModel>(),
            new ViewMap<CollectionPage, CollectionModel>(),
            new ViewMap<QueuePage, QueueModel>(),
            new ViewMap<SettingsPage, SettingsModel>(),
            new ViewMap<MapPage, MapModel>(),
            new DataViewMap<ArtworkPage, ArtworkModel, ArtworkSummary>(),
            new DataViewMap<SurveyPage, SurveyModel, SurveyStart>(),
            new DataViewMap<TreatmentPage, TreatmentModel, TreatmentSummary>());

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new("Main", View: views.FindByViewModel<MainModel>(), IsDefault: true,
                        Nested:
                        [
                            new("Collection", View: views.FindByViewModel<CollectionModel>(), IsDefault: true),
                            new("Queue", View: views.FindByViewModel<QueueModel>()),
                            new("Settings", View: views.FindByViewModel<SettingsModel>()),
                        ]),
                    new("Map", View: views.FindByViewModel<MapModel>()),
                    new("Artwork", View: views.FindByViewModel<ArtworkModel>()),
                    new("Survey", View: views.FindByViewModel<SurveyModel>()),
                    new("Treatment", View: views.FindByViewModel<TreatmentModel>()),
                ]));
    }
}
