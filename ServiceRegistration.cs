using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

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
            services.AddTransient<IWebSocketWrapper, WebSocketWrapper>();
            services.AddTransient<IUdpClientWrapper>(provider => 
            {
                // Use a factory method to allow configuration of the UDP client port
                var config = provider.GetRequiredService<VTubeStudioPhoneClientConfig>();
                return new UdpClientWrapper(new System.Net.Sockets.UdpClient(config.LocalPort));
            });
            
            // Register core services
            services.AddTransient<IVTubeStudioPCClient, VTubeStudioPCClient>();
            services.AddTransient<IVTubeStudioPhoneClient, VTubeStudioPhoneClient>();
            services.AddTransient<ITransformationEngine, TransformationEngine>();
            
            // Register logging services
            services.AddSingleton<IAppLogger, ConsoleAppLogger>();
            
            services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();
            // Register the orchestrator - scoped to ensure one instance per execution context
            services.AddScoped<IApplicationOrchestrator, ApplicationOrchestrator>();
            
            return services;
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