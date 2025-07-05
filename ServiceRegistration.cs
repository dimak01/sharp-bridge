using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Serilog;
using System.IO;
using SharpBridge.Repositories;

namespace SharpBridge
{
    /// <summary>
    /// Handles registration of services for dependency injection
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services with the DI container with consolidated configuration
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <param name="configDirectory">Config directory path</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSharpBridgeServices(
            this IServiceCollection services,
            string configDirectory)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(configDirectory))
                throw new ArgumentException("Config directory cannot be null or empty", nameof(configDirectory));

            // Register config manager
            services.AddSingleton<ConfigManager>(provider =>
                new ConfigManager(configDirectory));

            // Register configurations
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadPCConfigAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadPhoneConfigAsync().GetAwaiter().GetResult();
            });

            // Register GeneralSettingsConfig
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadGeneralSettingsConfigAsync().GetAwaiter().GetResult();
            });

            // Register ApplicationConfig (full consolidated config)
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadApplicationConfigAsync().GetAwaiter().GetResult();
            });

            // Register TransformationEngineConfig
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadTransformationConfigAsync().GetAwaiter().GetResult();
            });

            // Register UserPreferences
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<ConfigManager>();
                return configManager.LoadUserPreferencesAsync().GetAwaiter().GetResult();
            });

            // Register clients
            services.AddSingleton<IWebSocketWrapper, WebSocketWrapper>();

            // Register UDP client factory
            services.AddSingleton<IUdpClientWrapperFactory, UdpClientWrapperFactory>();

            // Register core services
            services.AddTransient<IVTubeStudioPhoneClient>(provider =>
            {
                var factory = provider.GetRequiredService<IUdpClientWrapperFactory>();
                var appConfigWatcher = provider.GetKeyedService<IFileChangeWatcher>("ApplicationConfig");
                return new VTubeStudioPhoneClient(
                    factory.CreateForPhoneClient(),
                    provider.GetRequiredService<VTubeStudioPhoneClientConfig>(),
                    provider.GetRequiredService<IAppLogger>(),
                    appConfigWatcher
                );
            });

            // Register file system watcher factory
            services.AddSingleton<IFileSystemWatcherFactory, FileSystemWatcherFactory>();

            // Register file change watchers - multiple instances for different config files
            services.AddKeyedSingleton<IFileChangeWatcher>("TransformationRules", (provider, key) =>
                new SharpBridge.Utilities.FileSystemChangeWatcher(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IFileSystemWatcherFactory>()));

            services.AddKeyedSingleton<IFileChangeWatcher>("ApplicationConfig", (provider, key) =>
                new SharpBridge.Utilities.FileSystemChangeWatcher(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IFileSystemWatcherFactory>()));

            // Register transformation rules repository
            services.AddSingleton<ITransformationRulesRepository>(provider =>
                new SharpBridge.Repositories.FileBasedTransformationRulesRepository(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetKeyedService<IFileChangeWatcher>("TransformationRules")!));


            services.AddTransient<ITransformationEngine, TransformationEngine>();

            // Register VTubeStudioPCClient as a singleton
            services.AddSingleton<VTubeStudioPCClient>(provider =>
            {
                var appConfigWatcher = provider.GetKeyedService<IFileChangeWatcher>("ApplicationConfig");
                return new VTubeStudioPCClient(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<VTubeStudioPCConfig>(),
                    provider.GetRequiredService<IWebSocketWrapper>(),
                    provider.GetRequiredService<IPortDiscoveryService>(),
                    appConfigWatcher
                );
            });
            services.AddSingleton<IVTubeStudioPCClient>(provider => provider.GetRequiredService<VTubeStudioPCClient>());

            // Register VTubeStudioPCParameterManager
            services.AddSingleton<IVTubeStudioPCParameterManager, VTubeStudioPCParameterManager>();

            // Register port discovery service
            services.AddTransient<IPortDiscoveryService>(provider =>
            {
                var factory = provider.GetRequiredService<IUdpClientWrapperFactory>();
                return new PortDiscoveryService(
                    provider.GetRequiredService<IAppLogger>(),
                    factory.CreateForPortDiscovery()
                );
            });

            // Register console abstraction
            services.AddSingleton<IConsole, SystemConsole>();

            // Register console window manager
            services.AddSingleton<IConsoleWindowManager, ConsoleWindowManager>();

            // Register logging services
            services.AddSingleton(ConfigureSerilog());

            // Register the Serilog logger as a singleton
            services.AddSingleton<IAppLogger, SerilogAppLogger>();

            // Register keyboard input handler
            services.AddSingleton<IKeyboardInputHandler, KeyboardInputHandler>();

            // Register table formatter
            services.AddSingleton<ITableFormatter, TableFormatter>();

            // Register parameter color service
            services.AddSingleton<IParameterColorService, ParameterColorService>();

            // Register process launcher
            services.AddSingleton<IProcessLauncher, ProcessLauncher>();

            // Register external editor service
            services.AddSingleton<IExternalEditorService, ExternalEditorService>();

            // Register shortcut services
            services.AddSingleton<IShortcutParser, ShortcutParser>();
            services.AddSingleton<IShortcutConfigurationManager, ShortcutConfigurationManager>();
            services.AddSingleton<ISystemHelpRenderer, SystemHelpRenderer>();

            // Register formatters
            services.AddSingleton<PhoneTrackingInfoFormatter>();
            services.AddSingleton<PCTrackingInfoFormatter>();
            services.AddSingleton<TransformationEngineInfoFormatter>();

            // Register console renderer - dependencies will be resolved automatically
            services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();

            // Register recovery policy
            services.AddSingleton<IRecoveryPolicy>(provider =>
            {
                var pcConfig = provider.GetRequiredService<VTubeStudioPCConfig>();
                return new SimpleRecoveryPolicy(TimeSpan.FromSeconds(pcConfig.RecoveryIntervalSeconds));
            });

            // Register the orchestrator - scoped to ensure one instance per execution context
            services.AddScoped<IApplicationOrchestrator, ApplicationOrchestrator>();

            return services;
        }

        /// <summary>
        /// Configures Serilog for the application
        /// </summary>
        private static ILogger ConfigureSerilog()
        {
            // Ensure the logs directory exists
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            // Configure Serilog with file-only output to avoid interfering with console GUI
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "sharp-bridge-.log"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 1024 * 1024, // 1MB size limit per file
                    retainedFileCountLimit: 31,      // Keep 31 files (for a month of logs)
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

        }
    }
}