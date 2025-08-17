using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities.ComInterop;
using Xunit;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Unit tests for WindowsFirewallEngine using dependency injection and mocking.
    /// Tests all functionality without requiring actual Windows COM interop.
    /// </summary>
    public class WindowsFirewallEngineTests : IDisposable
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IWindowsInterop> _mockInterop;
        private readonly Mock<IProcessInfo> _mockProcessInfo;
        private readonly WindowsFirewallEngine _engine;
        private readonly object _mockFirewallPolicy;

        public WindowsFirewallEngineTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockInterop = new Mock<IWindowsInterop>();
            _mockProcessInfo = new Mock<IProcessInfo>();
            _mockFirewallPolicy = new object(); // Mock firewall policy object

            // Setup default successful COM initialization
            _mockInterop.Setup(x => x.TryCreateFirewallPolicy(out It.Ref<dynamic?>.IsAny))
                .Returns((out dynamic? policy) =>
                {
                    policy = _mockFirewallPolicy;
                    return true;
                });

            _engine = new WindowsFirewallEngine(
                _mockLogger.Object,
                _mockInterop.Object,
                _mockProcessInfo.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WindowsFirewallEngine(null!, _mockInterop.Object, _mockProcessInfo.Object));
        }

        [Fact]
        public void Constructor_WithNullComInterop_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WindowsFirewallEngine(_mockLogger.Object, null!, _mockProcessInfo.Object));
        }



        [Fact]
        public void Constructor_WithNullProcessInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WindowsFirewallEngine(_mockLogger.Object, _mockInterop.Object, null!));
        }

        [Fact]
        public void Constructor_WithFailedComInitialization_LogsWarning()
        {
            // Arrange
            var mockInterop = new Mock<IWindowsInterop>();
            mockInterop.Setup(x => x.TryCreateFirewallPolicy(out It.Ref<dynamic?>.IsAny))
                .Returns((out dynamic? policy) =>
                {
                    policy = null;
                    return false;
                });

            // Act
            using (new WindowsFirewallEngine(
                _mockLogger.Object,
                mockInterop.Object,
                _mockProcessInfo.Object))
            {
                // Engine created and disposed to test initialization warning
            }

            // Assert
            _mockLogger.Verify(x => x.Warning("Failed to initialize Windows Firewall COM objects"), Times.Once);
        }

        [Fact]
        public void GetRelevantRules_WithNullFirewallPolicy_ReturnsEmptyList()
        {
            // Arrange
            var mockInterop = new Mock<IWindowsInterop>();
            mockInterop.Setup(x => x.TryCreateFirewallPolicy(out It.Ref<dynamic?>.IsAny))
                .Returns((out dynamic? policy) =>
                {
                    policy = null;
                    return false;
                });

            var engine = new WindowsFirewallEngine(
                _mockLogger.Object,
                mockInterop.Object,
                _mockProcessInfo.Object);

            // Act
            var result = engine.GetRelevantRules(1, 17, 2);

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(x => x.Warning("Firewall policy not available, returning empty rule list"), Times.Once);
        }

        [Fact]
        public void GetRelevantRules_WithValidPolicy_ReturnsFilteredRules()
        {
            // Arrange
            var mockRules = CreateMockFirewallRules();
            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(mockRules.Cast<dynamic>());

            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath())
                .Returns("C:\\Program Files\\SharpBridge\\SharpBridge.exe");

            // Act
            var result = _engine.GetRelevantRules(1, 17, 2, "192.168.1.100", "28960");

            // Assert
            Assert.NotEmpty(result);
            _mockInterop.Verify(x => x.EnumerateFirewallRules(_mockFirewallPolicy), Times.Once);
        }

        [Fact]
        public void GetDefaultAction_WithNullFirewallPolicy_ReturnsFalse()
        {
            // Arrange
            var mockInterop = new Mock<IWindowsInterop>();
            mockInterop.Setup(x => x.TryCreateFirewallPolicy(out It.Ref<dynamic?>.IsAny))
                .Returns((out dynamic? policy) =>
                {
                    policy = null;
                    return false;
                });

            var engine = new WindowsFirewallEngine(
                _mockLogger.Object,
                mockInterop.Object,
                _mockProcessInfo.Object);

            // Act
            var result = engine.GetDefaultAction(1, 2);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(x => x.Warning("Firewall policy not available, defaulting to block"), Times.Once);
        }

        [Fact]
        public void GetDefaultAction_WithAllowAction_ReturnsTrue()
        {
            // Arrange
            _mockInterop.Setup(x => x.GetDefaultAction(_mockFirewallPolicy, 1, 2))
                .Returns(NetFwAction.Allow);

            // Act
            var result = _engine.GetDefaultAction(1, 2);

            // Assert
            Assert.True(result);
            _mockInterop.Verify(x => x.GetDefaultAction(_mockFirewallPolicy, 1, 2), Times.Once);
        }

        [Fact]
        public void GetDefaultAction_WithBlockAction_ReturnsFalse()
        {
            // Arrange
            _mockInterop.Setup(x => x.GetDefaultAction(_mockFirewallPolicy, 1, 2))
                .Returns(NetFwAction.Block);

            // Act
            var result = _engine.GetDefaultAction(1, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsApplicationRule_WithNonComRule_ReturnsFalse()
        {
            // Arrange
            var nonComRule = new object();

            // Act
            var result = _engine.IsApplicationRule(nonComRule);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsApplicationRule_WithMatchingApplication_ReturnsTrue()
        {
            // Arrange
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.ApplicationName).Returns("C:\\Program Files\\SharpBridge\\SharpBridge.exe");

            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath())
                .Returns("C:\\Program Files\\SharpBridge\\SharpBridge.exe");

            // Act
            var result = _engine.IsApplicationRule(mockRule.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsApplicationRule_WithNonMatchingApplication_ReturnsFalse()
        {
            // Arrange
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.ApplicationName).Returns("C:\\Windows\\System32\\notepad.exe");

            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath())
                .Returns("C:\\Program Files\\SharpBridge\\SharpBridge.exe");

            // Act
            var result = _engine.IsApplicationRule(mockRule.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRuleEnabled_WithEnabledRule_ReturnsTrue()
        {
            // Arrange
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.Enabled).Returns(true);

            // Act
            var result = _engine.IsRuleEnabled(mockRule.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRuleEnabled_WithDisabledRule_ReturnsFalse()
        {
            // Arrange
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.Enabled).Returns(false);

            // Act
            var result = _engine.IsRuleEnabled(mockRule.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsProfileRule_WithMatchingProfile_ReturnsTrue()
        {
            // Arrange
            var rule = new FirewallRule
            {
                Profiles = NetFwProfile2.Private // Profile = 2
            };

            // Act
            var result = _engine.IsProfileRule(rule, NetFwProfile2.Private);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsProfileRule_WithAllProfiles_ReturnsTrue()
        {
            // Arrange
            var rule = new FirewallRule
            {
                Profiles = 7 // NET_FW_PROFILE2_ALL
            };

            // Act
            var result = _engine.IsProfileRule(rule, NetFwProfile2.Private);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsProfileRule_WithNonMatchingProfile_ReturnsFalse()
        {
            // Arrange
            var rule = new FirewallRule
            {
                Profiles = NetFwProfile2.Domain // Profile = 1
            };

            // Act
            var result = _engine.IsProfileRule(rule, NetFwProfile2.Private); // Profile = 2

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsProtocolRule_WithMatchingProtocol_ReturnsTrue()
        {
            // Arrange
            var rule = new FirewallRule
            {
                Protocol = "UDP"
            };

            // Act
            var result = _engine.IsProtocolRule(rule, 17); // UDP protocol

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsProtocolRule_WithAnyProtocol_ReturnsTrue()
        {
            // Arrange
            var rule = new FirewallRule
            {
                Protocol = "Any"
            };

            // Act
            var result = _engine.IsProtocolRule(rule, 17); // UDP protocol

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithExactMatch_ReturnsTrue()
        {
            // Act
            var result = _engine.IsPortInRange("28960", "28960");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithPortInRange_ReturnsTrue()
        {
            // Act
            var result = _engine.IsPortInRange("28965", "28960-28970");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithPortOutOfRange_ReturnsFalse()
        {
            // Act
            var result = _engine.IsPortInRange("29000", "28960-28970");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPortInRange_WithWildcard_ReturnsTrue()
        {
            // Act
            var result = _engine.IsPortInRange("12345", "*");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsHostInSubnet_WithExactMatch_ReturnsTrue()
        {
            // Act
            var result = _engine.IsHostInSubnet("192.168.1.100", "192.168.1.100");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsHostInSubnet_WithWildcard_ReturnsTrue()
        {
            // Act
            var result = _engine.IsHostInSubnet("192.168.1.100", "*");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void NormalizeAddress_WithWildcard_ReturnsZeroAddress()
        {
            // Act
            var result = _engine.NormalizeAddress("*");

            // Assert
            Assert.Equal("0.0.0.0", result);
        }

        [Fact]
        public void NormalizeAddress_WithAny_ReturnsZeroAddress()
        {
            // Act
            var result = _engine.NormalizeAddress("any");

            // Assert
            Assert.Equal("0.0.0.0", result);
        }

        [Fact]
        public void GetFirewallState_CallsSystemApi()
        {
            // Arrange
            _mockInterop.Setup(x => x.IsFirewallServiceRunning()).Returns(true);

            // Act
            var result = _engine.GetFirewallState();

            // Assert
            Assert.True(result);
            _mockInterop.Verify(x => x.IsFirewallServiceRunning(), Times.Once);
        }

        [Fact]
        public void GetCurrentProfiles_WithNullFirewallPolicy_ReturnsPrivateProfile()
        {
            // Arrange
            var mockInterop = new Mock<IWindowsInterop>();
            mockInterop.Setup(x => x.TryCreateFirewallPolicy(out It.Ref<dynamic?>.IsAny))
                .Returns((out dynamic? policy) =>
                {
                    policy = null;
                    return false;
                });

            var engine = new WindowsFirewallEngine(
                _mockLogger.Object,
                mockInterop.Object,
                _mockProcessInfo.Object);

            // Act
            var result = engine.GetCurrentProfiles();

            // Assert
            Assert.Equal(NetFwProfile2.Private, result);
        }

        [Fact]
        public void GetCurrentProfiles_WithValidPolicy_ReturnsProfilesFromComInterop()
        {
            // Arrange
            var expectedProfiles = NetFwProfile2.Domain | NetFwProfile2.Private;
            _mockInterop.Setup(x => x.GetCurrentProfiles(_mockFirewallPolicy))
                .Returns(expectedProfiles);

            // Act
            var result = _engine.GetCurrentProfiles();

            // Assert
            Assert.Equal(expectedProfiles, result);
            _mockInterop.Verify(x => x.GetCurrentProfiles(_mockFirewallPolicy), Times.Once);
        }

        [Fact]
        public void GetBestInterface_CallsSystemApi()
        {
            // Arrange
            var targetHost = "192.168.1.100";
            var expectedInterface = 5;
            _mockInterop.Setup(x => x.GetBestInterface(targetHost)).Returns(expectedInterface);

            // Act
            var result = _engine.GetBestInterface(targetHost);

            // Assert
            Assert.Equal(expectedInterface, result);
            _mockInterop.Verify(x => x.GetBestInterface(targetHost), Times.Once);
        }

        [Fact]
        public void GetInterfaceProfile_WithLoopbackInterface_ReturnsPrivateProfile()
        {
            // Act
            var result = _engine.GetInterfaceProfile(1); // Loopback interface

            // Assert
            Assert.Equal(NetFwProfile2.Private, result);
        }

        [Fact]
        public void GetInterfaceProfile_WithValidInterface_ReturnsCorrectProfile()
        {
            // Arrange
            var interfaceIndex = 5;
            var mockNetworkInterface = new Mock<NetworkInterface>();
            mockNetworkInterface.Setup(x => x.Id).Returns("interface-5");
            mockNetworkInterface.Setup(x => x.Name).Returns("Ethernet");
            mockNetworkInterface.Setup(x => x.Description).Returns("Intel Ethernet");
            mockNetworkInterface.Setup(x => x.OperationalStatus).Returns(OperationalStatus.Up);

            var mockIPProperties = new Mock<IPInterfaceProperties>();
            var mockIPv4Properties = new Mock<IPv4InterfaceProperties>();
            mockIPv4Properties.Setup(x => x.Index).Returns(interfaceIndex);
            mockIPProperties.Setup(x => x.GetIPv4Properties()).Returns(mockIPv4Properties.Object);
            mockNetworkInterface.Setup(x => x.GetIPProperties()).Returns(mockIPProperties.Object);

            var networkInterfaces = new[] { mockNetworkInterface.Object };
            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Returns(networkInterfaces);

            var expectedCategory = NLM_NETWORK_CATEGORY.Private;
            _mockInterop.Setup(x => x.GetNetworkCategoryForInterface("interface-5"))
                .Returns(expectedCategory);

            // Act
            var result = _engine.GetInterfaceProfile(interfaceIndex);

            // Assert
            Assert.Equal(NetFwProfile2.Private, result);
            _mockInterop.Verify(x => x.GetNetworkCategoryForInterface("interface-5"), Times.Once);
        }

        [Fact]
        public void Dispose_ReleasesComObjects()
        {
            // Act
            _engine.Dispose();

            // Assert
            _mockInterop.Verify(x => x.ReleaseComObject(_mockFirewallPolicy), Times.Once);
        }


        /// <summary>
        /// Creates mock firewall rules for testing
        /// </summary>
        private static List<INetFwRule> CreateMockFirewallRules()
        {
            var rules = new List<INetFwRule>();

            // Create a mock rule that allows UDP traffic
            var mockRule1 = new Mock<INetFwRule>();
            mockRule1.Setup(x => x.Name).Returns("Test UDP Allow Rule");
            mockRule1.Setup(x => x.Enabled).Returns(true);
            mockRule1.Setup(x => x.Direction).Returns(1); // Inbound
            mockRule1.Setup(x => x.Action).Returns(1); // Allow
            mockRule1.Setup(x => x.Protocol).Returns(17); // UDP
            mockRule1.Setup(x => x.LocalPorts).Returns("28960");
            mockRule1.Setup(x => x.RemotePorts).Returns("*");
            mockRule1.Setup(x => x.RemoteAddresses).Returns("*");
            mockRule1.Setup(x => x.ApplicationName).Returns(string.Empty);
            mockRule1.Setup(x => x.Profiles).Returns(2); // Private profile
            rules.Add(mockRule1.Object);

            // Create a mock rule that blocks TCP traffic
            var mockRule2 = new Mock<INetFwRule>();
            mockRule2.Setup(x => x.Name).Returns("Test TCP Block Rule");
            mockRule2.Setup(x => x.Enabled).Returns(true);
            mockRule2.Setup(x => x.Direction).Returns(1); // Inbound
            mockRule2.Setup(x => x.Action).Returns(0); // Block
            mockRule2.Setup(x => x.Protocol).Returns(6); // TCP
            mockRule2.Setup(x => x.LocalPorts).Returns("80");
            mockRule2.Setup(x => x.RemotePorts).Returns("*");
            mockRule2.Setup(x => x.RemoteAddresses).Returns("*");
            mockRule2.Setup(x => x.ApplicationName).Returns(string.Empty);
            mockRule2.Setup(x => x.Profiles).Returns(2); // Private profile
            rules.Add(mockRule2.Object);

            return rules;
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}
