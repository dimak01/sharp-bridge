using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    /// <summary>
    /// Unit tests for NetworkStatusFormatter.
    /// Tests the formatting of network troubleshooting information and command generation.
    /// </summary>
    public class NetworkStatusFormatterTests
    {
        private readonly Mock<INetworkCommandProvider> _mockCommandProvider;
        private readonly Mock<ITableFormatter> _mockTableFormatter;

        private readonly NetworkStatusFormatter _formatter;



        public NetworkStatusFormatterTests()
        {
            _mockCommandProvider = new Mock<INetworkCommandProvider>();
            _mockTableFormatter = new Mock<ITableFormatter>();
            _formatter = new NetworkStatusFormatter(_mockCommandProvider.Object, _mockTableFormatter.Object);

            // Setup default command provider responses
            _mockCommandProvider.Setup(x => x.GetPlatformName()).Returns("Windows");
            _mockCommandProvider.Setup(x => x.GetCheckPortStatusCommand(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((port, protocol) => $"netstat -an | findstr :{port}");
            _mockCommandProvider.Setup(x => x.GetAddFirewallRuleCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string, string, string, string, string>((name, dir, action, protocol, localPort, remotePort, remoteAddress) =>
                    $"netsh advfirewall firewall add rule name=\"{name}\" dir={dir} action={action} protocol={protocol}");
            _mockCommandProvider.Setup(x => x.GetRemoveFirewallRuleCommand(It.IsAny<string>()))
                .Returns<string>(name => $"netsh advfirewall firewall delete rule name=\"{name}\"");
            _mockCommandProvider.Setup(x => x.GetTestConnectivityCommand(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((host, port) => $"Test-NetConnection -ComputerName {host} -Port {port} -InformationLevel Detailed");

            // Setup default table formatter responses
            _mockTableFormatter.Setup(x => x.AppendTable(It.IsAny<StringBuilder>(), It.IsAny<string>(), It.IsAny<IEnumerable<It.IsAnyType>>(), It.IsAny<IList<ITableColumnFormatter<It.IsAnyType>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>()))
                .Callback<StringBuilder, string, IEnumerable<object>, IEnumerable<object>, int, int, int, int?, int>((sb, title, items, formatters, targetColumnCount, consoleWidth, singleColumnBarWidth, singleColumnMaxItems, indent) =>
                {
                    sb.AppendLine($"{new string(' ', indent)}{title}");
                    foreach (var item in items)
                    {
                        // Handle FirewallRule objects specifically
                        if (item is FirewallRule rule)
                        {
                            sb.AppendLine($"{new string(' ', indent)}  {rule.Name}");
                        }
                        else
                        {
                            sb.AppendLine($"{new string(' ', indent)}  {item}");
                        }
                    }
                });
        }

        [Fact]
        public void Constructor_WithNullCommandProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NetworkStatusFormatter(null!, _mockTableFormatter.Object));
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithBasicNetworkStatus_ReturnsFormattedOutput()
        {
            // Arrange
            var networkStatus = CreateBasicNetworkStatus();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("NETWORK TROUBLESHOOTING:", result);
            Assert.Contains("Platform: " + ConsoleColors.ColorizeBasicType("Windows"), result);
            Assert.Contains("IPHONE CONNECTION", result);
            Assert.Contains("PC VTube Studio CONNECTION", result);
            Assert.Contains("TROUBLESHOOTING COMMANDS:", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithTimestamp_IncludesFormattedTime()
        {
            // Arrange
            var timestamp = new DateTime(2023, 12, 25, 14, 30, 45);
            var networkStatus = CreateBasicNetworkStatus();
            networkStatus.LastUpdated = timestamp;
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("Last Updated: " + ConsoleColors.ColorizeBasicType("14:30:45"), result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithAllowedConnections_ShowsAllowedStatus()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithAllowedConnections();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - check for key parts with ANSI codes
            Assert.Contains("\u001b[93mLocal UDP Port 28964\u001b[0m: \u001b[96mAllowed\u001b[0m \u001b[92m✓\u001b[0m", result);
            Assert.Contains("\u001b[93mOutbound UDP to 192.168.1.100\u001b[0m: \u001b[96mAllowed\u001b[0m \u001b[92m✓\u001b[0m", result);
            Assert.Contains("\u001b[93mWebSocket TCP to localhost:8001\u001b[0m: \u001b[96mAllowed\u001b[0m \u001b[92m✓\u001b[0m", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithBlockedConnections_ShowsBlockedStatus()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithBlockedConnections();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - check for key parts with ANSI codes
            Assert.Contains("\u001b[93mLocal UDP Port 28964\u001b[0m: \u001b[96mBlocked\u001b[0m \u001b[91mX\u001b[0m", result);
            Assert.Contains("\u001b[93mOutbound UDP to 192.168.1.100\u001b[0m: \u001b[96mBlocked\u001b[0m \u001b[91mX\u001b[0m", result);
            Assert.Contains("\u001b[93mWebSocket TCP to localhost:8001\u001b[0m: \u001b[96mBlocked\u001b[0m \u001b[91mX\u001b[0m", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRules_ShowsRuleDetails()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithFirewallRules();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Now using table format, so we check for the table title and rule names
            Assert.Contains("Matching rules (2 found):", result);
            Assert.Contains("Allow UDP Rule", result);
            Assert.Contains("Block TCP Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithNoRules_ShowsDefaultActionMessage()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithNoRules();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("No explicit rules found – default action applied", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithManyRules_ShowsTopTenAndCount()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithManyRules();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Now shows top 10 instead of top 5
            Assert.Contains("Matching rules (top 10 of 12)", result);
            Assert.Contains("… and 2 more rules", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_GeneratesCorrectCommands()
        {
            // Arrange
            var networkStatus = CreateBasicNetworkStatus();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("iPhone UDP Commands", result);
            Assert.Contains("PC VTube Studio Commands", result);
            Assert.Contains("Check local port", result);
            Assert.Contains("Add inbound rule", result);
            Assert.Contains("Add outbound rule", result);
            Assert.Contains("Remove inbound rule", result);
            Assert.Contains("Remove outbound rule", result);
            Assert.Contains("Check WebSocket port", result);
            Assert.Contains("Test connectivity", result);
            Assert.Contains("Add WebSocket rule", result);
            Assert.Contains("Add discovery rule", result);
            Assert.Contains("Remove WebSocket rule", result);
            Assert.Contains("Remove discovery rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithBlockedConnections_HighlightsRelevantAddCommands()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithBlockedConnections();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            // Basic validation that the formatter produces output
            Assert.False(string.IsNullOrEmpty(result));
            Assert.Contains("NETWORK TROUBLESHOOTING:", result);
            Assert.Contains("netsh advfirewall firewall add rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithAllowedConnections_UsesNormalCommandColors()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithAllowedConnections();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            // Should contain the add commands
            Assert.Contains("\u001b[93mAdd inbound rule\u001b[0m:", result);
            Assert.Contains("\u001b[93mAdd WebSocket rule\u001b[0m:", result);
            // Should contain the basic command structure
            Assert.Contains("netsh advfirewall firewall add rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithDiscoveryAnalysis_ShowsDiscoverySection()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithDiscovery();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("Discovery UDP to localhost:47779", result);
            Assert.Contains("Add discovery rule", result);
            Assert.Contains("Remove discovery rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithNullAnalysis_HandlesGracefully()
        {
            // Arrange
            var networkStatus = CreateNetworkStatusWithNullAnalysis();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("NETWORK TROUBLESHOOTING:", result);
            Assert.Contains("TROUBLESHOOTING COMMANDS:", result);
            // Should not crash and should still show command sections
        }

        [Fact]
        public void RenderNetworkTroubleshooting_UsesConfigurationValues()
        {
            // Arrange
            var networkStatus = CreateBasicNetworkStatus();
            var appConfig = CreateCustomApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            Assert.Contains("Local UDP Port 12345", result);
            Assert.Contains("Outbound UDP to 10.0.0.50", result);
            Assert.Contains("WebSocket TCP to myhost:9001", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_CallsCommandProviderWithCorrectParameters()
        {
            // Arrange
            var networkStatus = CreateBasicNetworkStatus();
            var appConfig = CreateBasicApplicationConfig();

            // Act
            _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert
            _mockCommandProvider.Verify(x => x.GetPlatformName(), Times.Once);
            _mockCommandProvider.Verify(x => x.GetCheckPortStatusCommand("28964", "UDP"), Times.Once);
            _mockCommandProvider.Verify(x => x.GetCheckPortStatusCommand("8001", "TCP"), Times.Once);
            _mockCommandProvider.Verify(x => x.GetTestConnectivityCommand("localhost", "8001"), Times.Once);
            _mockCommandProvider.Verify(x => x.GetAddFirewallRuleCommand(
                "SharpBridge iPhone UDP Inbound", "in", "allow", "UDP", "28964", null, null), Times.Once);
            _mockCommandProvider.Verify(x => x.GetAddFirewallRuleCommand(
                "SharpBridge PC WebSocket", "out", "allow", "TCP", null, "8001", "localhost"), Times.Once);
        }

        // Helper methods for creating test data

        private static NetworkStatus CreateBasicNetworkStatus()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    OutboundFirewallAnalysis = CreateBasicFirewallAnalysis(true)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithAllowedConnections()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    OutboundFirewallAnalysis = CreateBasicFirewallAnalysis(true)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithBlockedConnections()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateBasicFirewallAnalysis(false),
                    OutboundFirewallAnalysis = CreateBasicFirewallAnalysis(false)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateBasicFirewallAnalysis(false),
                    DiscoveryFirewallAnalysis = CreateBasicFirewallAnalysis(false),
                    DiscoveryAllowed = false
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithFirewallRules()
        {
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Allow UDP Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "28964",
                    IsEnabled = true,
                    ApplicationName = @"C:\Path\To\SharpBridge.exe"
                },
                new FirewallRule
                {
                    Name = "Block TCP Rule",
                    Direction = "Outbound",
                    Action = "Block",
                    Protocol = "TCP",
                    LocalPort = "8001",
                    IsEnabled = false,
                    ApplicationName = null // Global rule
                }
            };

            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithNoRules()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithManyRules()
        {
            var manyRules = new List<FirewallRule>();
            for (int i = 1; i <= 12; i++)
            {
                manyRules.Add(new FirewallRule
                {
                    Name = $"Rule {i}",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "28964",
                    IsEnabled = true,
                    ApplicationName = null
                });
            }

            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, manyRules),
                    OutboundFirewallAnalysis = CreateBasicFirewallAnalysis(true)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithDiscovery()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    OutboundFirewallAnalysis = CreateBasicFirewallAnalysis(true)
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryFirewallAnalysis = CreateBasicFirewallAnalysis(true),
                    DiscoveryAllowed = true
                }
            };
        }

        private static NetworkStatus CreateNetworkStatusWithNullAnalysis()
        {
            return new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = null,
                    OutboundFirewallAnalysis = null
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = null,
                    DiscoveryFirewallAnalysis = null,
                    DiscoveryAllowed = false
                }
            };
        }

        private static ApplicationConfig CreateBasicApplicationConfig()
        {
            return new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    LocalPort = 28964,
                    IphonePort = 21412,
                    IphoneIpAddress = "192.168.1.100"
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };
        }

        private static ApplicationConfig CreateCustomApplicationConfig()
        {
            return new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    LocalPort = 12345,
                    IphonePort = 54321,
                    IphoneIpAddress = "10.0.0.50"
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "myhost",
                    Port = 9001
                }
            };
        }

        private static FirewallAnalysisResult CreateBasicFirewallAnalysis(bool isAllowed)
        {
            return new FirewallAnalysisResult
            {
                IsAllowed = isAllowed,
                DefaultActionAllowed = isAllowed,
                ProfileName = "Private",
                RelevantRules = new List<FirewallRule>()
            };
        }

        private static FirewallAnalysisResult CreateFirewallAnalysisWithRules(bool isAllowed, List<FirewallRule> rules)
        {
            return new FirewallAnalysisResult
            {
                IsAllowed = isAllowed,
                DefaultActionAllowed = isAllowed,
                ProfileName = "Private",
                RelevantRules = rules
            };
        }

        #region FormatRuleDescription Branch Coverage Tests

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithRemoteAddress_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "28964",
                    RemoteAddress = "192.168.1.100", // Specific remote address
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithZeroRemoteAddress_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Outbound",
                    Action = "Allow",
                    Protocol = "TCP",
                    LocalPort = "8001",
                    RemoteAddress = "0.0.0.0", // Zero address should show as "Any"
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithSpecificRemoteAddress_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Outbound",
                    Action = "Allow",
                    Protocol = "TCP",
                    LocalPort = "8001",
                    RemoteAddress = "10.0.0.5", // Specific remote address
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithRemoteAddressAny_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "28964",
                    RemoteAddress = "any", // "any" should be treated as wildcard
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithRemoteAddressStar_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Outbound",
                    Action = "Allow",
                    Protocol = "TCP",
                    LocalPort = "8001",
                    RemoteAddress = "*", // "*" should be treated as wildcard
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = CreateBasicApplicationConfig();

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithNullHostInPCConfig_UsesLocalhost()
        {
            // Arrange
            var networkStatus = CreateBasicNetworkStatus();
            var appConfig = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    Host = null!, // Null host should default to "localhost"
                    Port = 8001
                }
            };

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should use "localhost" in the test connectivity command
            Assert.Contains("Test-NetConnection -ComputerName localhost -Port 8001", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithNullLocalPort_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = null, // Null port should not be shown
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig("127.0.0.1")
                {
                    IphonePort = 0 // Use port 0 to avoid showing in output
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 0 // Use port 0 to avoid showing in output
                }
            };

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithEmptyLocalPort_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "", // Empty port should not be shown
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig("127.0.0.1")
                {
                    IphonePort = 0 // Use port 0 to avoid showing in output
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 0 // Use port 0 to avoid showing in output
                }
            };

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithStarLocalPort_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "*", // Star port should not be shown
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig("127.0.0.1")
                {
                    IphonePort = 0 // Use port 0 to avoid showing in output
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 0 // Use port 0 to avoid showing in output
                }
            };

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        [Fact]
        public void RenderNetworkTroubleshooting_WithFirewallRuleWithAnyLocalPort_ShowsRuleInTable()
        {
            // Arrange
            var rules = new List<FirewallRule>
            {
                new FirewallRule
                {
                    Name = "Test Rule",
                    Direction = "Inbound",
                    Action = "Allow",
                    Protocol = "UDP",
                    LocalPort = "any", // "any" port should not be shown
                    IsEnabled = true,
                    ApplicationName = null
                }
            };

            var networkStatus = new NetworkStatus
            {
                LastUpdated = DateTime.Now,
                IPhone = new IPhoneConnectionStatus
                {
                    InboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, rules),
                    OutboundFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>())
                },
                PC = new PCConnectionStatus
                {
                    WebSocketFirewallAnalysis = CreateFirewallAnalysisWithRules(true, new List<FirewallRule>()),
                    DiscoveryAllowed = true
                }
            };

            var appConfig = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig("127.0.0.1")
                {
                    IphonePort = 0 // Use port 0 to avoid showing in output
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 0 // Use port 0 to avoid showing in output
                }
            };

            // Act
            var result = _formatter.RenderNetworkTroubleshooting(networkStatus, appConfig);

            // Assert - Should show rule in table format
            Assert.Contains("Matching rule (1 found):", result);
            Assert.Contains("Test Rule", result);
        }

        #endregion
    }
}
