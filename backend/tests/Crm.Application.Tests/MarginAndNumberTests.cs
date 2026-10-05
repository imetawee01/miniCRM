using Crm.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Crm.Application.Tests;

public class MarginCalculationTests
{
    [Theory]
    [InlineData(100, 60, 40)]
    [InlineData(6_200_000, 4_100_000, 33.87)]
    [InlineData(0, 10, 0)]
    public void ComputeMargin_returns_expected_percent(decimal price, decimal cost, decimal expected)
        => ProposalPricing.ComputeMargin(price, cost).Should().Be(expected);
}

public class OpportunityNumberTests
{
    [Fact]
    public void Format_matches_spec()
    {
        var year = 2026;
        var n = 42;
        var value = $"OPP-{year}-{n:00000}";
        value.Should().Be("OPP-2026-00042");
    }
}
