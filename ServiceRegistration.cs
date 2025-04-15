using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;
using System;
using System.Net.WebSockets;

namespace SharpBridge
{
    /// <summary>
    /// Handles registration of services for dependency injection
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services with the DI container
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSharpBridgeServices(this IServiceCollection services)
        {
            // Register configurations
            services.AddSingleton<VTubeStudioConfig>();
            services.AddSingleton<VTubeStudioPhoneClientConfig>();
            
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
                var config = new VTubeStudioPhoneClientConfig();
                configureOptions(config);
                return config;
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures the VTubeStudioConfig with the specified settings
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure the VTube Studio options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigureVTubeStudioPC(
            this IServiceCollection services, 
            Action<VTubeStudioConfig> configureOptions)
        {
            services.AddSingleton(sp => 
            {
                var config = new VTubeStudioConfig();
                configureOptions(config);
                return config;
            });
            
            return services;
        }
    }
} 