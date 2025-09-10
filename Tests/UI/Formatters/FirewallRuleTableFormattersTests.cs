using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.UI.Formatters
{
    public class FirewallRuleTableFormattersTests
    {
        [Fact]
        public void CreateColumnFormatters_ReturnsCorrectNumberOfColumns()
        {
            // Act
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();

            // Assert
            formatters.Should().HaveCount(7);
            formatters.Should().AllBeAssignableTo<ITableColumnFormatter<FirewallRule>>();
        }

        [Fact]
        public void CreateColumnFormatters_ReturnsCorrectColumnHeaders()
        {
            // Act
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();

            // Assert
            var headers = formatters.Select(f => f.Header).ToList();
            headers.Should().ContainInOrder(
                "Status",
                "Action",
                "Rule Name",
                "Protocol",
                "Port",
                "Direction",
                "Scope"
            );
        }

        [Theory]
        [InlineData(true, "[Enabled]")]
        [InlineData(false, "[Disabled]")]
        public void StatusColumn_FormatsCorrectly(bool isEnabled, string expectedStatus)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var statusFormatter = formatters[0]; // Status column
            var rule = new FirewallRule { IsEnabled = isEnabled };

            // Act
            var result = statusFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Contain(expectedStatus);
        }

        [Theory]
        [InlineData("Allow", "Allow")]
        [InlineData("Block", "Block")]
        [InlineData("Deny", "Deny")]
        public void ActionColumn_FormatsCorrectly(string action, string expectedAction)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var actionFormatter = formatters[1]; // Action column
            var rule = new FirewallRule { Action = action };

            // Act
            var result = actionFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Contain(expectedAction);
        }

        [Theory]
        [InlineData("My Rule", "My Rule")]
        [InlineData("", "Unnamed Rule")]
        [InlineData(null, "Unnamed Rule")]
        public void RuleNameColumn_FormatsCorrectly(string ruleName, string expectedName)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var nameFormatter = formatters[2]; // Rule Name column
            var rule = new FirewallRule { Name = ruleName };

            // Act
            var result = nameFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("UDP", "UDP")]
        [InlineData("TCP", "TCP")]
        [InlineData("Any", "Any")]
        public void ProtocolColumn_FormatsCorrectly(string protocol, string expectedProtocol)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var protocolFormatter = formatters[3]; // Protocol column
            var rule = new FirewallRule { Protocol = protocol };

            // Act
            var result = protocolFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Be(expectedProtocol);
        }

        [Theory]
        [InlineData("8080", "8080")]
        [InlineData("*", "*")]
        [InlineData("any", "any")]
        [InlineData("", "*")]
        [InlineData(null, "*")]
        public void PortColumn_FormatsCorrectly(string localPort, string expectedPort)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var portFormatter = formatters[4]; // Port column
            var rule = new FirewallRule { LocalPort = localPort };

            // Act
            var result = portFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Be(expectedPort);
        }

        [Theory]
        [InlineData("inbound", "", "", "(Any → ThisDevice)")]
        [InlineData("outbound", "", "", "(ThisDevice → Any)")]
        [InlineData("inbound", "192.168.1.1", "", "(192.168.1.1 → ThisDevice)")]
        [InlineData("outbound", "192.168.1.1", "", "(ThisDevice → 192.168.1.1)")]
        [InlineData("inbound", "0.0.0.0", "", "(Any → ThisDevice)")]
        [InlineData("inbound", "*", "", "(Any → ThisDevice)")]
        [InlineData("inbound", "any", "", "(Any → ThisDevice)")]
        public void DirectionColumn_FormatsCorrectly(string direction, string remoteAddress, string localPort, string expectedDirection)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var directionFormatter = formatters[5]; // Direction column
            var rule = new FirewallRule
            {
                Direction = direction,
                RemoteAddress = remoteAddress,
                LocalPort = localPort
            };

            // Act
            var result = directionFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Be(expectedDirection);
        }

        [Theory]
        [InlineData("C:\\MyApp\\app.exe", "C:\\MyApp\\app.exe")]
        [InlineData("", "Global")]
        [InlineData(null, "Global")]
        public void ScopeColumn_FormatsCorrectly(string applicationName, string expectedScope)
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();
            var scopeFormatter = formatters[6]; // Scope column
            var rule = new FirewallRule { ApplicationName = applicationName };

            // Act
            var result = scopeFormatter.ValueFormatter(rule);

            // Assert
            result.Should().Be(expectedScope);
        }

        [Fact]
        public void AllFormatters_HaveReasonableWidthConstraints()
        {
            // Arrange
            var formatters = FirewallRuleTableFormatters.CreateColumnFormatters();

            // Act & Assert
            foreach (var formatter in formatters)
            {
                formatter.MinWidth.Should().BeGreaterThan(0, "each column should have a minimum width");
                formatter.MaxWidth.Should().BeGreaterThan(formatter.MinWidth, "max width should be greater than min width");
            }
        }
    }
}
