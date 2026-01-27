// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Core.Services;

using System;
using Serilog;
using System.IO;
using SharpBridge.Infrastructure.Repositories;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Infrastructure.Services;
using SharpBridge.Infrastructure.Wrappers;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Interfaces.UI.Managers;
using SharpBridge.UI.Managers;
using SharpBridge.Configuration.Extractors;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Configuration.Factories;
using SharpBridge.Configuration.Utilities;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.UI.Components;
using SharpBridge.Configuration.Services.Validators;
using SharpBridge.Configuration.Services.Remediation;
using SharpBridge.Interfaces.Configuration.Services.Remediation;
using SharpBridge.Configuration.Services;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Configuration.Managers;
using SharpBridge.Models.Configuration;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Interfaces.Infrastructure.Factories;
using SharpBridge.Infrastructure.Factories;
using SharpBridge.Interfaces.Core.Clients;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Core.Clients;
using SharpBridge.Interfaces.Domain;
using SharpBridge.Interfaces.Core.Engines;
using SharpBridge.Core.Engines;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.Interfaces.Core.Managers;
using SharpBridge.Core.Managers;
using SharpBridge.Interfaces.UI.Formatters;
using SharpBridge.UI.Formatters;
using SharpBridge.Interfaces.Infrastructure.Interop;
using SharpBridge.Utilities.ComInterop;
using SharpBridge.Interfaces.Infrastructure.Providers;
using SharpBridge.Infrastructure.Providers;
using SharpBridge.UI.Providers;
using SharpBridge.Interfaces.UI.Providers;
using SharpBridge.Domain.Services;
using SharpBridge.Interfaces.Core.Orchestrators;
using SharpBridge.Core.Orchestrators;
using SharpBridge.UI.Services;

namespace SharpBridge
{
    /// <summary>
    /// Handles registration of services for dependency injection
    /// </summary>
    public static class ServiceRegistration
    {
        private const string APPLICATION_CONFIG_KEY = "ApplicationConfig";
        private const string TRANSFORMATION_ENGINE_CONFIG_KEY = "TransformationRules";



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

            // Register logging services
            services.AddSingleton(ConfigureSerilog());

            // Register the Serilog logger as a singleton
            services.AddSingleton<IAppLogger, SerilogAppLogger>();

            // Register console abstraction
            services.AddSingleton<IConsole, SystemConsole>();

            // Register console window manager
            services.AddSingleton<IConsoleWindowManager, ConsoleWindowManager>();

            // Register field extractors
            services.AddTransient<ConfigSectionFieldExtractor>();

            // Register field extractors factory
            services.AddSingleton<IConfigSectionFieldExtractorsFactory, ConfigSectionFieldExtractorsFactory>();

            // Register field validator
            services.AddSingleton<IConfigFieldValidator, ConfigFieldValidator>();

            // Register shortcut parser (needed by remediation services)
            services.AddSingleton<IShortcutParser, ShortcutParser>();

            // Register validators and remediation services (needed by ConfigRemediationService)
            services.AddTransient<VTubeStudioPCConfigValidator>();
            services.AddTransient<VTubeStudioPhoneClientConfigValidator>();
            services.AddTransient<GeneralSettingsConfigValidator>();
            services.AddTransient<TransformationEngineConfigValidator>();

            services.AddTransient<VTubeStudioPCConfigRemediationService>();
            services.AddTransient<VTubeStudioPhoneClientConfigRemediationService>();
            services.AddTransient<GeneralSettingsConfigRemediationService>();
            services.AddTransient<TransformationEngineConfigRemediationService>();

            services.AddSingleton<IConfigSectionValidatorsFactory, ConfigSectionValidatorsFactory>();
            services.AddSingleton<IConfigSectionRemediationServiceFactory, ConfigSectionRemediationServiceFactory>();

            services.AddSingleton<IConfigRemediationService, ConfigRemediationService>();

            // Register config manager
            services.AddSingleton<IConfigManager>(provider =>
                new ConfigManager(configDirectory,
                    provider.GetRequiredService<IConfigSectionFieldExtractorsFactory>(),
                    provider.GetRequiredService<IAppLogger>()));

            // Build temporary container to get core services for immediate configuration remediation
            var tempServiceProvider = services.BuildServiceProvider();

            // Run configuration remediation immediately to ensure valid config before service initialization
            var configRemediationService = tempServiceProvider.GetRequiredService<IConfigRemediationService>();
            var remediationSuccess = configRemediationService.RemediateConfigurationAsync().GetAwaiter().GetResult();

            if (!remediationSuccess)
            {
                throw new InvalidOperationException("Configuration remediation failed during startup");
            }

            tempServiceProvider.Dispose();

            // Register configurations
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadSectionAsync<VTubeStudioPCConfig>().GetAwaiter().GetResult();
            });

            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadSectionAsync<VTubeStudioPhoneClientConfig>().GetAwaiter().GetResult();
            });

            // Register GeneralSettingsConfig
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadSectionAsync<GeneralSettingsConfig>().GetAwaiter().GetResult();
            });

            // Register ApplicationConfig (full consolidated config)
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadApplicationConfigAsync().GetAwaiter().GetResult();
            });

            // Register TransformationEngineConfig
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadSectionAsync<TransformationEngineConfig>().GetAwaiter().GetResult();
            });

            // Register UserPreferences
            services.AddSingleton(provider =>
            {
                var configManager = provider.GetRequiredService<IConfigManager>();
                return configManager.LoadUserPreferencesAsync().GetAwaiter().GetResult();
            });

            // Register clients
            services.AddSingleton<IWebSocketWrapper, WebSocketWrapper>();

            // Register UDP client factory
            services.AddSingleton<IUdpClientWrapperFactory, UdpClientWrapperFactory>();

            // Register version service
            services.AddSingleton<IVersionService, VersionService>();

            // Register core services
            services.AddSingleton<IVTubeStudioPhoneClient>(provider =>
            {
                var factory = provider.GetRequiredService<IUdpClientWrapperFactory>();
                var appConfigWatcher = provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY);
                return new VTubeStudioPhoneClient(
                    factory.CreateForPhoneClient(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetRequiredService<IAppLogger>(),
                    appConfigWatcher
                );
            });

            // Register file system watcher factory
            services.AddSingleton<IFileSystemWatcherFactory, FileSystemWatcherFactory>();

            // Register file change watchers - multiple instances for different config files
            services.AddKeyedSingleton<IFileChangeWatcher>(TRANSFORMATION_ENGINE_CONFIG_KEY, (provider, key) =>
                new FileSystemChangeWatcher(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IFileSystemWatcherFactory>()));

            services.AddKeyedSingleton<IFileChangeWatcher>(APPLICATION_CONFIG_KEY, (provider, key) =>
                new FileSystemChangeWatcher(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IFileSystemWatcherFactory>()));

            // Register transformation rules repository
            services.AddSingleton<ITransformationRulesRepository>(provider =>
                new FileBasedTransformationRulesRepository(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetKeyedService<IFileChangeWatcher>(TRANSFORMATION_ENGINE_CONFIG_KEY)!,
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)!,
                    provider.GetRequiredService<IConfigManager>()));


            services.AddSingleton<ITransformationEngine>(provider =>
                new TransformationEngine(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<ITransformationRulesRepository>(),
                    provider.GetRequiredService<TransformationEngineConfig>(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)!
                ));

            // Register VTubeStudioPCClient as a singleton
            services.AddSingleton<VTubeStudioPCClient>(provider =>
            {
                var appConfigWatcher = provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY);
                return new VTubeStudioPCClient(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IConfigManager>(),
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

            // Register keyboard input handler
            services.AddSingleton<IKeyboardInputHandler, KeyboardInputHandler>();

            // Register table formatter
            services.AddSingleton<ITableFormatter, TableFormatter>();

            // Register parameter color service
            services.AddSingleton<IParameterColorService, ParameterColorService>();

            // Register process launcher
            services.AddSingleton<IProcessLauncher, ProcessLauncher>();

            // Register external editor service
            services.AddSingleton<IExternalEditorService>(provider =>
                new ExternalEditorService(
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IProcessLauncher>(),
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)!
                ));

            // Register Windows-specific dependencies for firewall engine (unified facade only)
            services.AddSingleton<IWindowsInterop, WindowsInterop>();
            services.AddSingleton<IProcessInfo, ProcessInfo>();

            // Register network monitoring services
            services.AddSingleton<IFirewallEngine, WindowsFirewallEngine>();
            services.AddSingleton<IFirewallAnalyzer, WindowsFirewallAnalyzer>();
            services.AddSingleton<INetworkCommandProvider, WindowsNetworkCommandProvider>();
            services.AddSingleton<INetworkStatusFormatter>(provider =>
                new NetworkStatusFormatter(
                    provider.GetRequiredService<INetworkCommandProvider>(),
                    provider.GetRequiredService<ITableFormatter>()
                ));
            services.AddSingleton<IPortStatusMonitorService, PortStatusMonitorService>();

            // Register shortcut services
            services.AddSingleton<IShortcutConfigurationManager>(provider =>
                new ShortcutConfigurationManager(
                    provider.GetRequiredService<IShortcutParser>(),
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)
                ));
            // Register SystemHelpRenderer both as interface and concrete class
            services.AddSingleton<SystemHelpContentProvider>(provider =>
                new SystemHelpContentProvider(
                    provider.GetRequiredService<IShortcutConfigurationManager>(),
                    provider.GetRequiredService<IParameterTableConfigurationManager>(),
                    provider.GetRequiredService<ITableFormatter>(),
                    provider.GetRequiredService<INetworkStatusFormatter>(),
                    provider.GetRequiredService<IExternalEditorService>(),
                    provider.GetRequiredService<IVersionService>()
                ));
            services.AddSingleton<ISystemHelpRenderer>(provider => provider.GetRequiredService<SystemHelpContentProvider>());

            // Register parameter table configuration manager
            services.AddSingleton<IParameterTableConfigurationManager, ParameterTableConfigurationManager>();

            // Register formatters
            services.AddSingleton<PhoneTrackingInfoFormatter>();
            services.AddSingleton<PCTrackingInfoFormatter>(provider =>
                new PCTrackingInfoFormatter(
                    provider.GetRequiredService<IConsole>(),
                    provider.GetRequiredService<ITableFormatter>(),
                    provider.GetRequiredService<IParameterColorService>(),
                    provider.GetRequiredService<IShortcutConfigurationManager>(),
                    provider.GetRequiredService<UserPreferences>(),
                    provider.GetRequiredService<IParameterTableConfigurationManager>()));
            services.AddSingleton<TransformationEngineInfoFormatter>();

            // Register MainStatusRenderer both as interface and concrete class
            services.AddSingleton<MainStatusContentProvider>(provider =>
                new MainStatusContentProvider(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<TransformationEngineInfoFormatter>(),
                    provider.GetRequiredService<PhoneTrackingInfoFormatter>(),
                    provider.GetRequiredService<PCTrackingInfoFormatter>(),
                    provider.GetRequiredService<IExternalEditorService>(),
                    provider.GetRequiredService<IVersionService>()
                ));
            services.AddSingleton<IMainStatusRenderer>(provider => provider.GetRequiredService<MainStatusContentProvider>());

            // Register network status renderer
            services.AddSingleton<NetworkStatusContentProvider>(provider =>
                new NetworkStatusContentProvider(
                    provider.GetRequiredService<IPortStatusMonitorService>(),
                    provider.GetRequiredService<INetworkStatusFormatter>(),
                    provider.GetRequiredService<IExternalEditorService>(),
                    provider.GetRequiredService<IAppLogger>()
                ));

            // Register initialization content provider
            services.AddSingleton<InitializationContentProvider>(provider =>
                new InitializationContentProvider(
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IExternalEditorService>()
                ));

            // Register console mode manager
            services.AddSingleton<IConsoleModeManager>(provider =>
                new ConsoleModeManager(
                    provider.GetRequiredService<IConsole>(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IShortcutConfigurationManager>(),
                    new IConsoleModeContentProvider[]
                    {
                        provider.GetRequiredService<MainStatusContentProvider>(),
                        provider.GetRequiredService<SystemHelpContentProvider>(),
                        provider.GetRequiredService<NetworkStatusContentProvider>(),
                        provider.GetRequiredService<InitializationContentProvider>()
                    }
                ));

            // Register recovery policy
            services.AddSingleton<IRecoveryPolicy>(provider =>
            {
                var pcConfig = provider.GetRequiredService<VTubeStudioPCConfig>();
                return new SimpleRecoveryPolicy(TimeSpan.FromSeconds(pcConfig.RecoveryIntervalSeconds));
            });

            // Register application initialization service
            services.AddScoped<IApplicationInitializationService>(provider =>
                new ApplicationInitializationService(
                    provider.GetRequiredService<IVTubeStudioPCClient>(),
                    provider.GetRequiredService<IVTubeStudioPhoneClient>(),
                    provider.GetRequiredService<ITransformationEngine>(),
                    provider.GetRequiredService<IVTubeStudioPCParameterManager>(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)!,
                    provider.GetRequiredService<IConsoleModeManager>(),
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<InitializationContentProvider>()
                ));

            // Register the orchestrator - scoped to ensure one instance per execution context
            services.AddScoped<IApplicationOrchestrator>(provider =>
                new ApplicationOrchestrator(
                    provider.GetRequiredService<IVTubeStudioPCClient>(),
                    provider.GetRequiredService<IVTubeStudioPhoneClient>(),
                    provider.GetRequiredService<ITransformationEngine>(),
                    provider.GetRequiredService<VTubeStudioPhoneClientConfig>(),
                    provider.GetRequiredService<IAppLogger>(),
                    provider.GetRequiredService<IConsoleModeManager>(),
                    provider.GetRequiredService<IKeyboardInputHandler>(),
                    provider.GetRequiredService<IVTubeStudioPCParameterManager>(),
                    provider.GetRequiredService<IRecoveryPolicy>(),
                    provider.GetRequiredService<IConsoleWindowManager>(),
                    provider.GetRequiredService<IParameterColorService>(),
                    provider.GetRequiredService<IShortcutConfigurationManager>(),
                    provider.GetRequiredService<ApplicationConfig>(),
                    provider.GetRequiredService<UserPreferences>(),
                    provider.GetRequiredService<IConfigManager>(),
                    provider.GetKeyedService<IFileChangeWatcher>(APPLICATION_CONFIG_KEY)!,
                    provider.GetRequiredService<IApplicationInitializationService>()
                ));

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
                .MinimumLevel.Information()
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