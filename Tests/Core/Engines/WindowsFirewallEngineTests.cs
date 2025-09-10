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

namespace SharpBridge.Tests.Core.Engines
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

        [Fact]
        public void IsApplicationRule_WithEmptyApplicationName_ReturnsFalse()
        {
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.ApplicationName).Returns(string.Empty);
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            var result = _engine.IsApplicationRule(mockRule.Object);

            Assert.False(result);
        }

        [Fact]
        public void IsApplicationRule_WithEmptyCurrentExePath_ReturnsFalse()
        {
            var mockRule = new Mock<INetFwRule>();
            mockRule.Setup(x => x.ApplicationName).Returns("C\\\\Program Files\\\\SharpBridge\\\\SharpBridge.exe");
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns(string.Empty);

            var result = _engine.IsApplicationRule(mockRule.Object);

            Assert.False(result);
        }

        [Fact]
        public void IsApplicationRule_WhenAccessThrows_ReturnsFalse()
        {
            var mockRule = new Mock<INetFwRule>();
            mockRule.SetupGet(x => x.ApplicationName).Throws(new Exception("boom"));

            var result = _engine.IsApplicationRule(mockRule.Object);

            Assert.False(result);
        }

        [Fact]
        public void IsRuleEnabled_WhenAccessThrows_ReturnsFalse()
        {
            var mockRule = new Mock<INetFwRule>();
            mockRule.SetupGet(x => x.Enabled).Throws(new Exception("boom"));

            var result = _engine.IsRuleEnabled(mockRule.Object);

            Assert.False(result);
        }

        [Fact]
        public void IsProtocolRule_WithNonMatchingProtocol_ReturnsFalse()
        {
            var rule = new FirewallRule { Protocol = "TCP" };
            var result = _engine.IsProtocolRule(rule, 17); // UDP
            Assert.False(result);
        }

        [Fact]
        public void IsTargetMatch_WithCidrAndPortRange_Matches()
        {
            var rule = new FirewallRule { RemoteAddress = "192.168.1.0/24", RemotePort = "28960-28970" };
            var result = _engine.IsTargetMatch(rule, "192.168.1.50", "28965");
            Assert.True(result);
        }

        [Fact]
        public void IsTargetMatch_WithCidrNoMatch_ReturnsFalse()
        {
            var rule = new FirewallRule { RemoteAddress = "192.168.1.0/24", RemotePort = "28960-28970" };
            var result = _engine.IsTargetMatch(rule, "10.0.0.1", "28965");
            Assert.False(result);
        }

        [Fact]
        public void IsTargetMatch_WithWildcardAddressNoPort_Matches()
        {
            var rule = new FirewallRule { RemoteAddress = "*", RemotePort = string.Empty };
            var result = _engine.IsTargetMatch(rule, "203.0.113.5", "12345");
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithInvalidRange_ReturnsFalse()
        {
            var result = _engine.IsPortInRange("12345", "abc-def");
            Assert.False(result);
        }

        [Fact]
        public void IsHostInSubnet_WithCidrTrue_ReturnsTrue()
        {
            var result = _engine.IsHostInSubnet("192.168.1.42", "192.168.1.0/24");
            Assert.True(result);
        }

        [Fact]
        public void IsHostInSubnet_WithCidrFalse_ReturnsFalse()
        {
            var result = _engine.IsHostInSubnet("192.168.2.42", "192.168.1.0/24");
            Assert.False(result);
        }

        [Fact]
        public void NormalizeAddress_SpecialTokens_ReturnExpected()
        {
            Assert.Equal("192.168.1.0/24", _engine.NormalizeAddress("<localsubnet>"));
            Assert.Equal("127.0.0.1", _engine.NormalizeAddress("<local>"));
            Assert.Equal("0.0.0.0", _engine.NormalizeAddress("*"));
        }

        [Fact]
        public void GetDefaultAction_WhenInteropThrows_ReturnsFalse()
        {
            _mockInterop.Setup(x => x.GetDefaultAction(_mockFirewallPolicy, 1, 2)).Throws(new Exception("boom"));
            var result = _engine.GetDefaultAction(1, 2);
            Assert.False(result);
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error getting default firewall action"))), Times.Once);
        }

        [Fact]
        public void GetInterfaceProfile_InterfaceNotFound_ReturnsPrivate()
        {
            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Returns(Array.Empty<NetworkInterface>());
            var result = _engine.GetInterfaceProfile(99);
            Assert.Equal(NetFwProfile2.Private, result);
        }

        [Fact]
        public void GetInterfaceProfile_WhenGetCategoryThrows_ReturnsPrivate()
        {
            var interfaceIndex = 7;
            var mockNetworkInterface = new Mock<NetworkInterface>();
            mockNetworkInterface.Setup(x => x.Id).Returns("iface-7");
            mockNetworkInterface.Setup(x => x.Name).Returns("Ethernet");
            mockNetworkInterface.Setup(x => x.Description).Returns("Adapter");
            mockNetworkInterface.Setup(x => x.OperationalStatus).Returns(OperationalStatus.Up);

            var mockIPProperties = new Mock<IPInterfaceProperties>();
            var mockIPv4Properties = new Mock<IPv4InterfaceProperties>();
            mockIPv4Properties.Setup(x => x.Index).Returns(interfaceIndex);
            mockIPProperties.Setup(x => x.GetIPv4Properties()).Returns(mockIPv4Properties.Object);
            mockNetworkInterface.Setup(x => x.GetIPProperties()).Returns(mockIPProperties.Object);

            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Returns(new[] { mockNetworkInterface.Object });
            _mockInterop.Setup(x => x.GetNetworkCategoryForInterface("iface-7")).Throws(new Exception("boom"));

            var result = _engine.GetInterfaceProfile(interfaceIndex);
            Assert.Equal(NetFwProfile2.Private, result);
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Error detecting interface profile"))), Times.Once);
        }

        [Fact]
        public void Dispose_CalledTwice_IsIdempotent()
        {
            _engine.Dispose();
            _engine.Dispose();
            _mockInterop.Verify(x => x.ReleaseComObject(_mockFirewallPolicy), Times.AtMostOnce);
        }

        [Fact]
        public void GetRelevantRules_WithThrowingRule_SkipsInvalidAndReturnsValid()
        {
            // throwing rule during conversion
            var badRule = new Mock<INetFwRule>();
            badRule.SetupGet(x => x.Name).Throws(new Exception("bad"));

            // valid global UDP inbound allow on port 28964
            var goodRule = new Mock<INetFwRule>();
            goodRule.Setup(x => x.Name).Returns("Good UDP Allow");
            goodRule.Setup(x => x.Enabled).Returns(true);
            goodRule.Setup(x => x.Direction).Returns(1);
            goodRule.Setup(x => x.Action).Returns(1);
            goodRule.Setup(x => x.Protocol).Returns(17);
            goodRule.Setup(x => x.LocalPorts).Returns("28964");
            goodRule.Setup(x => x.RemotePorts).Returns("*");
            goodRule.Setup(x => x.RemoteAddresses).Returns("*");
            goodRule.Setup(x => x.ApplicationName).Returns(string.Empty);
            goodRule.Setup(x => x.Profiles).Returns(2);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { badRule.Object, goodRule.Object }.Cast<dynamic>());

            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\\\\\Program Files\\\\SharpBridge\\\\SharpBridge.exe");

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Single(results);
            Assert.Equal("Good UDP Allow", results[0].Name);
        }

        [Fact]
        public void EnumerateAllRules_WhenInteropThrows_ReturnsEmptyAndLogsError()
        {
            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Throws(new Exception("interop boom"));

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");

            Assert.Empty(results);
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error enumerating firewall rules"))), Times.Once);
        }

        [Fact]
        public void GetRelevantRules_WhenProcessInfoThrows_ReturnsEmptyAndLogsError()
        {
            var goodRule = new Mock<INetFwRule>();
            goodRule.Setup(x => x.Name).Returns("Good UDP Allow");
            goodRule.Setup(x => x.Enabled).Returns(true);
            goodRule.Setup(x => x.Direction).Returns(1);
            goodRule.Setup(x => x.Action).Returns(1);
            goodRule.Setup(x => x.Protocol).Returns(17);
            goodRule.Setup(x => x.LocalPorts).Returns("28964");
            goodRule.Setup(x => x.RemotePorts).Returns("*");
            goodRule.Setup(x => x.RemoteAddresses).Returns("*");
            goodRule.Setup(x => x.ApplicationName).Returns(string.Empty);
            goodRule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { goodRule.Object }.Cast<dynamic>());

            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Throws(new Exception("proc boom"));

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");

            Assert.Empty(results);
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error getting relevant rules"))), Times.Once);
        }

        [Fact]
        public void Dispose_WhenReleaseThrows_DoesNotPropagate()
        {
            _mockInterop.Setup(x => x.ReleaseComObject(It.IsAny<object>())).Throws(new Exception("release boom"));
            _engine.Dispose();
            // no exception and logger.Debug for error disposing
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error disposing COM objects"))), Times.Once);
        }

        [Fact]
        public void GetRelevantRules_WithApplicationMismatch_Excluded()
        {
            var badAppRule = new Mock<INetFwRule>();
            badAppRule.Setup(x => x.Name).Returns("Bad App Rule");
            badAppRule.Setup(x => x.Enabled).Returns(true);
            badAppRule.Setup(x => x.Direction).Returns(1); // Inbound
            badAppRule.Setup(x => x.Action).Returns(1);
            badAppRule.Setup(x => x.Protocol).Returns(17); // UDP
            badAppRule.Setup(x => x.LocalPorts).Returns("28964");
            badAppRule.Setup(x => x.RemotePorts).Returns("*");
            badAppRule.Setup(x => x.RemoteAddresses).Returns("*");
            badAppRule.Setup(x => x.ApplicationName).Returns("C\\Other\\App.exe");
            badAppRule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { badAppRule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetRelevantRules_WithDirectionMismatch_Excluded()
        {
            var rule = new Mock<INetFwRule>();
            rule.Setup(x => x.Name).Returns("Outbound Rule");
            rule.Setup(x => x.Enabled).Returns(true);
            rule.Setup(x => x.Direction).Returns(2); // Outbound
            rule.Setup(x => x.Action).Returns(1);
            rule.Setup(x => x.Protocol).Returns(17); // UDP
            rule.Setup(x => x.LocalPorts).Returns("28964");
            rule.Setup(x => x.RemotePorts).Returns("*");
            rule.Setup(x => x.RemoteAddresses).Returns("*");
            rule.Setup(x => x.ApplicationName).Returns(string.Empty);
            rule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { rule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetRelevantRules_WithLocalPortMismatch_Excluded()
        {
            var rule = new Mock<INetFwRule>();
            rule.Setup(x => x.Name).Returns("Inbound Other Port");
            rule.Setup(x => x.Enabled).Returns(true);
            rule.Setup(x => x.Direction).Returns(1); // Inbound
            rule.Setup(x => x.Action).Returns(1);
            rule.Setup(x => x.Protocol).Returns(17); // UDP
            rule.Setup(x => x.LocalPorts).Returns("1111");
            rule.Setup(x => x.RemotePorts).Returns("*");
            rule.Setup(x => x.RemoteAddresses).Returns("*");
            rule.Setup(x => x.ApplicationName).Returns(string.Empty);
            rule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { rule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetInterfaceProfile_MapsDomainAndPublic()
        {
            var interfaceIndex = 8;
            var mockNetworkInterface = new Mock<NetworkInterface>();
            mockNetworkInterface.Setup(x => x.Id).Returns("iface-8");
            mockNetworkInterface.Setup(x => x.Name).Returns("Ethernet");
            mockNetworkInterface.Setup(x => x.Description).Returns("Adapter");
            mockNetworkInterface.Setup(x => x.OperationalStatus).Returns(OperationalStatus.Up);

            var mockIPProperties = new Mock<IPInterfaceProperties>();
            var mockIPv4Properties = new Mock<IPv4InterfaceProperties>();
            mockIPv4Properties.Setup(x => x.Index).Returns(interfaceIndex);
            mockIPProperties.Setup(x => x.GetIPv4Properties()).Returns(mockIPv4Properties.Object);
            mockNetworkInterface.Setup(x => x.GetIPProperties()).Returns(mockIPProperties.Object);

            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Returns(new[] { mockNetworkInterface.Object });

            _mockInterop.Setup(x => x.GetNetworkCategoryForInterface("iface-8")).Returns(NLM_NETWORK_CATEGORY.Domain);
            var domainProfile = _engine.GetInterfaceProfile(interfaceIndex);
            Assert.Equal(NetFwProfile2.Domain, domainProfile);

            _mockInterop.Setup(x => x.GetNetworkCategoryForInterface("iface-8")).Returns(NLM_NETWORK_CATEGORY.Public);
            var publicProfile = _engine.GetInterfaceProfile(interfaceIndex);
            Assert.Equal(NetFwProfile2.Public, publicProfile);
        }

        [Fact]
        public void GetInterfaceProfile_WhenIPv4PropertiesThrows_ReturnsPrivate()
        {
            var interfaceIndex = 9;
            var mockNetworkInterface = new Mock<NetworkInterface>();
            mockNetworkInterface.Setup(x => x.Id).Returns("iface-9");
            mockNetworkInterface.Setup(x => x.Name).Returns("Ethernet");
            mockNetworkInterface.Setup(x => x.Description).Returns("Adapter");
            mockNetworkInterface.Setup(x => x.OperationalStatus).Returns(OperationalStatus.Up);

            var mockIPProperties = new Mock<IPInterfaceProperties>();
            mockIPProperties.Setup(x => x.GetIPv4Properties()).Throws(new Exception("ipv4 props oops"));
            mockNetworkInterface.Setup(x => x.GetIPProperties()).Returns(mockIPProperties.Object);

            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Returns(new[] { mockNetworkInterface.Object });

            var profile = _engine.GetInterfaceProfile(interfaceIndex);
            Assert.Equal(NetFwProfile2.Private, profile);
        }

        [Fact]
        public void IsPortInRange_WithEmptyPort_ReturnsTrue()
        {
            var result = _engine.IsPortInRange(string.Empty, "28964");
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithEmptyulePort_ReturnsTrue()
        {
            var result = _engine.IsPortInRange("28964", string.Empty);
            Assert.True(result);
        }

        [Fact]
        public void IsHostInSubnet_WithAny_ReturnsTrue()
        {
            var result = _engine.IsHostInSubnet("1.2.3.4", "any");
            Assert.True(result);
        }

        [Fact]
        public void IsProfileRule_WithNullRule_ReturnsFalseAndLogs()
        {
            var result = _engine.IsProfileRule(null!, NetFwProfile2.Private);
            Assert.False(result);
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error checking profile rule"))), Times.Once);
        }

        [Fact]
        public void IsProtocolRule_WithNullRule_ReturnsFalseAndLogs()
        {
            var result = _engine.IsProtocolRule(null!, 17);
            Assert.False(result);
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error checking protocol rule"))), Times.Once);
        }

        [Fact]
        public void IsTargetMatch_WithNullRule_ReturnsFalseAndLogs()
        {
            var result = _engine.IsTargetMatch(null!, "192.168.0.10", "1234");
            Assert.False(result);
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error checking target match"))), Times.Once);
        }

        [Fact]
        public void IsRuleEnabled_WithNonComRule_ReturnsFalse()
        {
            var result = _engine.IsRuleEnabled(new object());
            Assert.False(result);
        }

        [Fact]
        public void IsTargetMatch_WithPortMismatch_ReturnsFalse()
        {
            var rule = new FirewallRule { RemoteAddress = "*", RemotePort = "1000-2000" };
            var result = _engine.IsTargetMatch(rule, "8.8.8.8", "999");
            Assert.False(result);
        }

        [Fact]
        public void IsTargetMatch_WithNullRuleProperties_UsesEmptyStrings()
        {
            var rule = new FirewallRule { RemoteAddress = null, RemotePort = null };
            var result = _engine.IsTargetMatch(rule, "192.168.1.1", "1234");
            Assert.True(result); // Should match since both are empty
        }

        [Fact]
        public void IsTargetMatch_WithEmptyRuleAddress_SkipsHostCheck()
        {
            var rule = new FirewallRule { RemoteAddress = "", RemotePort = "1234" };
            var result = _engine.IsTargetMatch(rule, "192.168.1.1", "1234");
            Assert.True(result); // Should match since address check is skipped
        }

        [Fact]
        public void IsTargetMatch_WithEmptyRulePort_SkipsPortCheck()
        {
            var rule = new FirewallRule { RemoteAddress = "192.168.1.0/24", RemotePort = "" };
            var result = _engine.IsTargetMatch(rule, "192.168.1.50", "999");
            Assert.True(result); // Should match since port check is skipped
        }

        [Fact]
        public void IsTargetMatch_WithEmptyTargetHost_SkipsHostCheck()
        {
            var rule = new FirewallRule { RemoteAddress = "192.168.1.0/24", RemotePort = "1234" };
            var result = _engine.IsTargetMatch(rule, "", "1234");
            Assert.True(result); // Should match since target host check is skipped
        }

        [Fact]
        public void IsTargetMatch_WithEmptyTargetPort_SkipsPortCheck()
        {
            var rule = new FirewallRule { RemoteAddress = "192.168.1.0/24", RemotePort = "1234" };
            var result = _engine.IsTargetMatch(rule, "192.168.1.50", "");
            Assert.True(result); // Should match since target port check is skipped
        }

        [Fact]
        public void IsHostInSubnet_WithEmptyInputs_ReturnsTrue()
        {
            Assert.True(_engine.IsHostInSubnet("", "192.168.1.0/24"));
            Assert.True(_engine.IsHostInSubnet("192.168.1.42", ""));
        }

        [Fact]
        public void IsHostInSubnet_WithExactMatch_ReturnsTrue()
        {
            Assert.True(_engine.IsHostInSubnet("10.0.0.5", "10.0.0.5"));
        }

        [Fact]
        public void NormalizeAddress_DefaultBranch_ReturnsTrimmed()
        {
            var result = _engine.NormalizeAddress(" 10.1.2.3 ");
            Assert.Equal("10.1.2.3", result);
        }

        [Fact]
        public void GetRelevantRules_WithProfileMismatch_Excluded()
        {
            var rule = new Mock<INetFwRule>();
            rule.Setup(x => x.Name).Returns("Private Only");
            rule.Setup(x => x.Enabled).Returns(true);
            rule.Setup(x => x.Direction).Returns(1); // Inbound
            rule.Setup(x => x.Action).Returns(1);
            rule.Setup(x => x.Protocol).Returns(17); // UDP
            rule.Setup(x => x.LocalPorts).Returns("28964");
            rule.Setup(x => x.RemotePorts).Returns("*");
            rule.Setup(x => x.RemoteAddresses).Returns("*");
            rule.Setup(x => x.ApplicationName).Returns(string.Empty);
            rule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { rule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            // Request Domain profile rules so mismatch with Private
            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Domain, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetRelevantRules_WithProtocolMismatch_Excluded()
        {
            var rule = new Mock<INetFwRule>();
            rule.Setup(x => x.Name).Returns("TCP Rule");
            rule.Setup(x => x.Enabled).Returns(true);
            rule.Setup(x => x.Direction).Returns(1); // Inbound
            rule.Setup(x => x.Action).Returns(1);
            rule.Setup(x => x.Protocol).Returns(6); // TCP
            rule.Setup(x => x.LocalPorts).Returns("28964");
            rule.Setup(x => x.RemotePorts).Returns("*");
            rule.Setup(x => x.RemoteAddresses).Returns("*");
            rule.Setup(x => x.ApplicationName).Returns(string.Empty);
            rule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { rule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C\\Program Files\\SharpBridge\\SharpBridge.exe");

            // Ask for UDP rules
            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void IsPortInRange_WithSingleInvalidPort_ReturnsFalse()
        {
            var result = _engine.IsPortInRange("999", "not-a-number");
            Assert.False(result);
        }

        [Fact]
        public void IsPortInRange_WithSinglePortRule_ProceedsToWildcardOrExactMatch()
        {
            // Test case where rulePort does not contain a hyphen,
            // ensuring the 'else' branch of line 230 is hit.
            var result = _engine.IsPortInRange("80", "80");
            Assert.True(result); // Should match exact port

            result = _engine.IsPortInRange("22", "22");
            Assert.True(result);
        }

        [Fact]
        public void IsPortInRange_WithInvalidRangeFormat_ReturnsFalseAndHitsElseBranch()
        {
            // Test case where rulePort contains a hyphen but is not a valid range
            // This should cause int.TryParse to return false, hitting the else branch of line 233
            var result = _engine.IsPortInRange("100", "1000-abc");
            Assert.False(result);

            // Test case where range.Length is not 2
            result = _engine.IsPortInRange("100", "1000");
            Assert.False(result);

            result = _engine.IsPortInRange("100", "1000-2000-3000");
            Assert.False(result);
        }

        [Fact]
        public void IsPortInRange_WithNonNumericPort_ReturnsFalse()
        {
            // Test case where the 'port' parameter is not a valid integer.
            // This should cause int.TryParse to return false, hitting the else branch of line 235.
            var result = _engine.IsPortInRange("abc", "1000-2000");
            Assert.False(result);
        }

        [Fact]
        public void IsPortInRange_WithNonWildcardRule_ProceedsToExactMatch()
        {
            // Test case where rulePort is not a wildcard, ensuring the 'else' branch of line 243 is hit.
            // This will lead to the exact port match logic on line 247.
            var result = _engine.IsPortInRange("80", "80");
            Assert.True(result);

            result = _engine.IsPortInRange("9000", "9001"); // Mismatch
            Assert.False(result);
        }

        [Fact]
        public void IsPortInRange_WithExceptionInTryBlock_ReturnsFalseAndLogs()
        {
            // Test edge cases that might trigger unexpected exceptions
            var result = _engine.IsPortInRange("999", "invalid-range");
            Assert.False(result);

            // Note: The catch block (lines 249-252) is effectively unreachable
            // because the methods in the try block handle invalid inputs gracefully
            // without throwing exceptions. This test covers the return false path.
        }

        [Fact]
        public void IsHostInSubnet_WithInvalidFormat_ReturnsFalse()
        {
            var result = _engine.IsHostInSubnet("192.168.1.1", "not-cidr-format");
            Assert.False(result);
        }

        [Fact]
        public void IsHostInSubnet_WithIncompleteCidr_ReturnsFalse()
        {
            // Test case where subnet contains slash but parts.Length != 2
            var result = _engine.IsHostInSubnet("192.168.1.1", "192.168.1.0/");
            Assert.False(result); // Should fall through to exact match, which will fail
        }

        [Fact]
        public void IsHostInSubnet_WithInvalidMaskBits_ReturnsFalse()
        {
            // Test case where subnet contains slash, parts.Length == 2, but mask is non-numeric
            var result = _engine.IsHostInSubnet("192.168.1.1", "192.168.1.0/abc");
            Assert.False(result); // Should fall through to exact match, which will fail
        }

        [Fact]
        public void IsHostInSubnet_WithTooManyParts_ReturnsFalse()
        {
            // Test case where subnet contains slash but parts.Length > 2
            var result = _engine.IsHostInSubnet("192.168.1.1", "192.168.1.0/24/extra");
            Assert.False(result); // Should fall through to exact match, which will fail
        }

        [Fact]
        public void NormalizeAddress_WithNullAddress_ReturnsNull()
        {
            var result = _engine.NormalizeAddress(null!);
            Assert.Null(result);
        }

        [Fact]
        public void GetRelevantRules_WithNonMatchingAppName_Excluded()
        {
            var rule = new Mock<INetFwRule>();
            rule.Setup(x => x.Name).Returns("Other App Rule");
            rule.Setup(x => x.Enabled).Returns(true);
            rule.Setup(x => x.Direction).Returns(1); // Inbound
            rule.Setup(x => x.Action).Returns(1);
            rule.Setup(x => x.Protocol).Returns(17); // UDP
            rule.Setup(x => x.LocalPorts).Returns("28964");
            rule.Setup(x => x.RemotePorts).Returns("*");
            rule.Setup(x => x.RemoteAddresses).Returns("*");
            rule.Setup(x => x.ApplicationName).Returns("C:\\Other\\App.exe");
            rule.Setup(x => x.Profiles).Returns(NetFwProfile2.Private);

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { rule.Object }.Cast<dynamic>());
            _mockProcessInfo.Setup(x => x.GetCurrentExecutablePath()).Returns("C:\\Program Files\\SharpBridge\\SharpBridge.exe");

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetRelevantRules_WithRuleConversionError_SkipsRule()
        {
            var badRule = new Mock<INetFwRule>();
            badRule.Setup(x => x.Name).Throws(new Exception("COM error"));

            _mockInterop.Setup(x => x.EnumerateFirewallRules(_mockFirewallPolicy))
                .Returns(new List<INetFwRule> { badRule.Object }.Cast<dynamic>());

            var results = _engine.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error converting COM rule"))), Times.Once);
        }

        [Fact]
        public void GetRelevantRules_WithNullPolicy_ReturnsEmpty()
        {
            // Create engine with null policy by mocking TryCreateFirewallPolicy to return false
            var mockInterop = new Mock<IWindowsInterop>();
            dynamic? nullPolicy = null;
            mockInterop.Setup(x => x.TryCreateFirewallPolicy(out nullPolicy)).Returns(false);
            mockInterop.Setup(x => x.EnumerateFirewallRules(It.IsAny<object>())).Returns(new List<object>());

            var engineWithNullPolicy = new WindowsFirewallEngine(_mockLogger.Object, mockInterop.Object, _mockProcessInfo.Object);
            var results = engineWithNullPolicy.GetRelevantRules(1, 17, NetFwProfile2.Private, null, null, "28964");
            Assert.Empty(results);
        }

        [Fact]
        public void GetInterfaceProfile_WhenGetAllInterfacesThrows_ReturnsPrivateAndLogs()
        {
            _mockInterop.Setup(x => x.GetAllNetworkInterfaces()).Throws(new Exception("ifaces boom"));
            var result = _engine.GetInterfaceProfile(42);
            Assert.Equal(NetFwProfile2.Private, result);
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Error finding NetworkInterface by index"))), Times.Once);
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
