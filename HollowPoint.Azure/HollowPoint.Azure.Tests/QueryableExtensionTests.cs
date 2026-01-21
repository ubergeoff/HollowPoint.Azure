using FluentAssertions;
using HollowPoint.Azure.Extensions;
using HollowPoint.Azure.Query;
using Moq;
using Azure.Data.Tables;

namespace HollowPoint.Azure.Tests;

public class QueryableExtensionTests
{
    [Fact]
    public void ToODataString_WithAzureTableQueryable_ReturnsValidODataFilter()
    {
        // Arrange
        var mockTableClient = new Mock<TableClient>();
        var queryable = new AzureTableQueryable<TestEntity>(mockTableClient.Object)
            .Where(e => e.PartitionKey == "test" && e.Count > 10);

        // Act
        var result = queryable.ToODataString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("PartitionKey eq 'test'");
        result.Should().Contain("Count gt 10");
    }

    [Fact]
    public void ToODataString_WithComplexQuery_ReturnsCorrectFilter()
    {
        // Arrange
        var mockTableClient = new Mock<TableClient>();
        var queryable = new AzureTableQueryable<TestEntity>(mockTableClient.Object)
            .Where(e => (e.IsDeleted == false && e.Count >= 5) || e.Title == "Important");

        // Act
        var result = queryable.ToODataString();

        // Assert
        result.Should().Contain("IsDeleted eq 0");
        result.Should().Contain("Count ge 5");
        result.Should().Contain("Title eq 'Important'");
        result.Should().Contain("or");
        result.Should().Contain("and");
    }

    [Fact]
    public void ToODataString_WithNonAzureTableQueryable_ThrowsNotSupportedException()
    {
        // Arrange
        var standardQueryable = new[] { new TestEntity() }.AsQueryable();

        // Act
        Action act = () => standardQueryable.ToODataString();

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*DbTable*");
    }

    [Fact]
    public void ToODataString_WithMultipleConditions_PreservesAllConditions()
    {
        // Arrange
        var mockTableClient = new Mock<TableClient>();
        var queryable = new AzureTableQueryable<TestEntity>(mockTableClient.Object)
            .Where(e => e.PartitionKey == "pk1")
            .Where(e => e.Count > 0)
            .Where(e => e.IsDeleted == false);

        // Act
        var result = queryable.ToODataString();

        // Assert
        result.Should().Contain("PartitionKey eq 'pk1'");
        result.Should().Contain("Count gt 0");
        result.Should().Contain("IsDeleted eq 0");
    }
}
