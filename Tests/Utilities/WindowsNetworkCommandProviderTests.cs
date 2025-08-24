using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    /// <summary>
    /// Unit tests for WindowsNetworkCommandProvider.
    /// Demonstrates how clean and testable the utility is with pure string generation.
    /// </summary>
    public class WindowsNetworkCommandProviderTests
    {
        private readonly WindowsNetworkCommandProvider _provider;

        public WindowsNetworkCommandProviderTests()
        {
            _provider = new WindowsNetworkCommandProvider();
        }

        [Fact]
        public void GetPlatformName_ReturnsWindows()
        {
            // Act
            var result = _provider.GetPlatformName();

            // Assert
            Assert.Equal("Windows", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithBasicParameters_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "Test Rule";
            var direction = "in";
            var action = "allow";
            var protocol = "UDP";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Test Rule\" dir=in action=allow protocol=UDP", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithAllParameters_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "SharpBridge UDP Rule";
            var direction = "out";
            var action = "block";
            var protocol = "TCP";
            var localPort = "28964";
            var remotePort = "8001";
            var remoteAddress = "192.168.1.100";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, localPort, remotePort, remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"SharpBridge UDP Rule\" dir=out action=block protocol=TCP localport=28964 remoteport=8001 remoteip=192.168.1.100", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithOnlyLocalPort_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "Local Port Rule";
            var direction = "in";
            var action = "allow";
            var protocol = "UDP";
            var localPort = "28964";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, localPort);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Local Port Rule\" dir=in action=allow protocol=UDP localport=28964", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithOnlyRemotePort_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "Remote Port Rule";
            var direction = "out";
            var action = "allow";
            var protocol = "TCP";
            var remotePort = "8001";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, remotePort: remotePort);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Remote Port Rule\" dir=out action=allow protocol=TCP remoteport=8001", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithOnlyRemoteAddress_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "Remote Address Rule";
            var direction = "in";
            var action = "block";
            var protocol = "UDP";
            var remoteAddress = "10.0.0.50";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, remoteAddress: remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Remote Address Rule\" dir=in action=block protocol=UDP remoteip=10.0.0.50", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithEmptyOptionalParameters_ReturnsBasicCommand()
        {
            // Arrange
            var ruleName = "Basic Rule";
            var direction = "in";
            var action = "allow";
            var protocol = "UDP";
            var localPort = "";
            var remotePort = "";
            var remoteAddress = "";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, localPort, remotePort, remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Basic Rule\" dir=in action=allow protocol=UDP", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithNullOptionalParameters_ReturnsBasicCommand()
        {
            // Arrange
            var ruleName = "Null Parameters Rule";
            var direction = "out";
            var action = "block";
            var protocol = "TCP";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, null, null, null);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Null Parameters Rule\" dir=out action=block protocol=TCP", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithSpecialCharactersInRuleName_EscapesCorrectly()
        {
            // Arrange
            var ruleName = "Test \"Quote\" Rule";
            var direction = "in";
            var action = "allow";
            var protocol = "UDP";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"Test \"Quote\" Rule\" dir=in action=allow protocol=UDP", result);
        }

        [Fact]
        public void GetRemoveFirewallRuleCommand_WithBasicRuleName_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "Test Rule";

            // Act
            var result = _provider.GetRemoveFirewallRuleCommand(ruleName);

            // Assert
            Assert.Equal("netsh advfirewall firewall delete rule name=\"Test Rule\"", result);
        }

        [Fact]
        public void GetRemoveFirewallRuleCommand_WithComplexRuleName_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "SharpBridge UDP Inbound Rule";

            // Act
            var result = _provider.GetRemoveFirewallRuleCommand(ruleName);

            // Assert
            Assert.Equal("netsh advfirewall firewall delete rule name=\"SharpBridge UDP Inbound Rule\"", result);
        }

        [Fact]
        public void GetRemoveFirewallRuleCommand_WithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var ruleName = "Test \"Quote\" Rule";

            // Act
            var result = _provider.GetRemoveFirewallRuleCommand(ruleName);

            // Assert
            Assert.Equal("netsh advfirewall firewall delete rule name=\"Test \"Quote\" Rule\"", result);
        }

        [Fact]
        public void GetCheckPortStatusCommand_WithUDPPort_ReturnsCorrectCommand()
        {
            // Arrange
            var port = "28964";
            var protocol = "UDP";

            // Act
            var result = _provider.GetCheckPortStatusCommand(port, protocol);

            // Assert
            Assert.Equal("netstat -an | findstr :28964", result);
        }

        [Fact]
        public void GetCheckPortStatusCommand_WithTCPPort_ReturnsCorrectCommand()
        {
            // Arrange
            var port = "8001";
            var protocol = "TCP";

            // Act
            var result = _provider.GetCheckPortStatusCommand(port, protocol);

            // Assert
            Assert.Equal("netstat -an | findstr :8001", result);
        }

        [Fact]
        public void GetCheckPortStatusCommand_WithDifferentPorts_ReturnsCorrectCommands()
        {
            // Arrange
            var testCases = new[]
            {
                new { Port = "80", Protocol = "TCP", Expected = "netstat -an | findstr :80" },
                new { Port = "443", Protocol = "TCP", Expected = "netstat -an | findstr :443" },
                new { Port = "53", Protocol = "UDP", Expected = "netstat -an | findstr :53" },
                new { Port = "123", Protocol = "UDP", Expected = "netstat -an | findstr :123" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _provider.GetCheckPortStatusCommand(testCase.Port, testCase.Protocol);
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public void GetTestConnectivityCommand_WithBasicHostAndPort_ReturnsCorrectCommand()
        {
            // Arrange
            var host = "localhost";
            var port = "8001";

            // Act
            var result = _provider.GetTestConnectivityCommand(host, port);

            // Assert
            Assert.Equal("Test-NetConnection -ComputerName localhost -Port 8001 -InformationLevel Detailed", result);
        }

        [Fact]
        public void GetTestConnectivityCommand_WithIPAddress_ReturnsCorrectCommand()
        {
            // Arrange
            var host = "192.168.1.100";
            var port = "28964";

            // Act
            var result = _provider.GetTestConnectivityCommand(host, port);

            // Assert
            Assert.Equal("Test-NetConnection -ComputerName 192.168.1.100 -Port 28964 -InformationLevel Detailed", result);
        }

        [Fact]
        public void GetTestConnectivityCommand_WithDomainName_ReturnsCorrectCommand()
        {
            // Arrange
            var host = "api.vtubestudio.com";
            var port = "443";

            // Act
            var result = _provider.GetTestConnectivityCommand(host, port);

            // Assert
            Assert.Equal("Test-NetConnection -ComputerName api.vtubestudio.com -Port 443 -InformationLevel Detailed", result);
        }

        [Fact]
        public void GetTestConnectivityCommand_WithDifferentPorts_ReturnsCorrectCommands()
        {
            // Arrange
            var testCases = new[]
            {
                new { Host = "localhost", Port = "80", Expected = "Test-NetConnection -ComputerName localhost -Port 80 -InformationLevel Detailed" },
                new { Host = "127.0.0.1", Port = "443", Expected = "Test-NetConnection -ComputerName 127.0.0.1 -Port 443 -InformationLevel Detailed" },
                new { Host = "google.com", Port = "22", Expected = "Test-NetConnection -ComputerName google.com -Port 22 -InformationLevel Detailed" },
                new { Host = "10.0.0.50", Port = "8001", Expected = "Test-NetConnection -ComputerName 10.0.0.50 -Port 8001 -InformationLevel Detailed" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _provider.GetTestConnectivityCommand(testCase.Host, testCase.Port);
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithDifferentDirections_ReturnsCorrectCommands()
        {
            // Arrange
            var testCases = new[]
            {
                new { Direction = "in", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=UDP" },
                new { Direction = "out", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=out action=allow protocol=UDP" },
                new { Direction = "IN", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=IN action=allow protocol=UDP" },
                new { Direction = "OUT", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=OUT action=allow protocol=UDP" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _provider.GetAddFirewallRuleCommand("Test", testCase.Direction, "allow", "UDP");
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithDifferentActions_ReturnsCorrectCommands()
        {
            // Arrange
            var testCases = new[]
            {
                new { Action = "allow", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=UDP" },
                new { Action = "block", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=block protocol=UDP" },
                new { Action = "ALLOW", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=ALLOW protocol=UDP" },
                new { Action = "BLOCK", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=BLOCK protocol=UDP" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _provider.GetAddFirewallRuleCommand("Test", "in", testCase.Action, "UDP");
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithDifferentProtocols_ReturnsCorrectCommands()
        {
            // Arrange
            var testCases = new[]
            {
                new { Protocol = "UDP", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=UDP" },
                new { Protocol = "TCP", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=TCP" },
                new { Protocol = "udp", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=udp" },
                new { Protocol = "tcp", Expected = "netsh advfirewall firewall add rule name=\"Test\" dir=in action=allow protocol=tcp" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _provider.GetAddFirewallRuleCommand("Test", "in", "allow", testCase.Protocol);
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithComplexScenario_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "SharpBridge iPhone UDP Rule";
            var direction = "in";
            var action = "allow";
            var protocol = "UDP";
            var localPort = "28964";
            var remoteAddress = "192.168.1.100";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, localPort, remoteAddress: remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"SharpBridge iPhone UDP Rule\" dir=in action=allow protocol=UDP localport=28964 remoteip=192.168.1.100", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithPCWebSocketScenario_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "SharpBridge PC WebSocket Rule";
            var direction = "out";
            var action = "allow";
            var protocol = "TCP";
            var remotePort = "8001";
            var remoteAddress = "localhost";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, remotePort: remotePort, remoteAddress: remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"SharpBridge PC WebSocket Rule\" dir=out action=allow protocol=TCP remoteport=8001 remoteip=localhost", result);
        }

        [Fact]
        public void GetAddFirewallRuleCommand_WithDiscoveryPortScenario_ReturnsCorrectCommand()
        {
            // Arrange
            var ruleName = "SharpBridge Discovery UDP Rule";
            var direction = "out";
            var action = "allow";
            var protocol = "UDP";
            var remotePort = "47779";
            var remoteAddress = "localhost";

            // Act
            var result = _provider.GetAddFirewallRuleCommand(ruleName, direction, action, protocol, remotePort: remotePort, remoteAddress: remoteAddress);

            // Assert
            Assert.Equal("netsh advfirewall firewall add rule name=\"SharpBridge Discovery UDP Rule\" dir=out action=allow protocol=UDP remoteport=47779 remoteip=localhost", result);
        }
    }
}








