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

namespace SharpBridge
{
    /// <summary>
    /// Handles registration of services for dependency injection
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services with the DI container with custom configuration paths
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <param name="configDirectory">Config directory path</param>
        /// <param name="pcConfigFilename">PC config filename</param>
        /// <param name="phoneConfigFilename">Phone config filename</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSharpBridgeServices(
            this IServiceCollection services, 
            string configDirectory, 
            string pcConfigFilename, 
            string phoneConfigFilename)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(configDirectory))
                throw new ArgumentException("Config directory cannot be null or empty", nameof(configDirectory));
            if (string.IsNullOrEmpty(pcConfigFilename))
                throw new ArgumentException("PC config filename cannot be null or empty", nameof(pcConfigFilename));
            if (string.IsNullOrEmpty(phoneConfigFilename))
                throw new ArgumentException("Phone config filename cannot be null or empty", nameof(phoneConfigFilename));
                
            // Register config manager
            services.AddSingleton<ConfigManager>(provider => 
                new ConfigManager(configDirectory, pcConfigFilename, phoneConfigFilename));
            
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
            
            // Register clients
            services.AddSingleton<IWebSocketWrapper, WebSocketWrapper>();
            
            // Register UDP client factory
            services.AddSingleton<IUdpClientWrapperFactory, UdpClientWrapperFactory>();
            
            // Register core services
            services.AddTransient<IVTubeStudioPhoneClient>(provider => 
            {
                var factory = provider.GetRequiredService<IUdpClientWrapperFactory>();
                return new VTubeStudioPhoneClient(
                    factory.CreateForPhoneClient(),
                    provider.GetRequiredService<VTubeStudioPhoneClientConfig>(),
                    provider.GetRequiredService<IAppLogger>()
                );
            });
            
            services.AddTransient<ITransformationEngine, TransformationEngine>();
            
            // Register VTubeStudioPCClient as a singleton
            services.AddSingleton<VTubeStudioPCClient>();
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
            
            // Register logging services
            services.AddSingleton(ConfigureSerilog());
            
            // Register the Serilog logger as a singleton
            services.AddSingleton<IAppLogger, SerilogAppLogger>();
            
            // Register keyboard input handler
            services.AddSingleton<IKeyboardInputHandler, KeyboardInputHandler>();
            
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
            
            // Configure Serilog with basic settings
            // This is a temporary configuration, will be replaced with a proper configuration file
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "sharp-bridge-.log"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 1024 * 1024, // 1MB size limit per file
                    retainedFileCountLimit: 31,      // Keep 31 files (for a month of logs)
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
                
        }
        
        /// <summary>
        /// Configures the VTubeStudioPhoneClientConfig with the specified settings
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure the phone client options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigureVTubeStudioPhoneClient(
            this IServiceCollection services, 
            Action<VTubeStudioPhoneClientConfig> configureOptions)
        {
            services.AddSingleton(sp => 
            {
                var configManager = sp.GetRequiredService<ConfigManager>();
                var config = configManager.LoadPhoneConfigAsync().GetAwaiter().GetResult();
                configureOptions(config);
                configManager.SavePhoneConfigAsync(config).GetAwaiter().GetResult();
                return config;
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures the VTubeStudioPCConfig with the specified settings
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure the VTube Studio PC options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigureVTubeStudioPC(
            this IServiceCollection services, 
            Action<VTubeStudioPCConfig> configureOptions)
        {
            services.AddSingleton(sp => 
            {
                var configManager = sp.GetRequiredService<ConfigManager>();
                var config = configManager.LoadPCConfigAsync().GetAwaiter().GetResult();
                configureOptions(config);
                configManager.SavePCConfigAsync(config).GetAwaiter().GetResult();
                return config;
            });
            
            return services;
        }
    }
} 