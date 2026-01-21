using FluentAssertions;
using HollowPoint.Azure;

namespace HollowPoint.Azure.Tests;

public class BaseTableEntityTests
{
    [Fact]
    public void Constructor_InitializesPropertiesWithDefaults()
    {
        // Act
        var entity = new BaseTableEntity();

        // Assert
        entity.PartitionKey.Should().Be(string.Empty);
        entity.RowKey.Should().Be(string.Empty);
        entity.Title.Should().Be(string.Empty);
        entity.IsDeleted.Should().BeFalse();
        entity.DateAdded.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DateAdded_UsesUtcTime()
    {
        // Act
        var entity = new BaseTableEntity();

        // Assert
        entity.DateAdded.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ToString_ReturnsTitle()
    {
        // Arrange
        var entity = new BaseTableEntity
        {
            Title = "Test Title"
        };

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Be("Test Title");
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new BaseTableEntity();
        var testDate = DateTime.UtcNow.AddDays(-5);

        // Act
        entity.PartitionKey = "pk-test";
        entity.RowKey = "rk-test";
        entity.Title = "My Entity";
        entity.IsDeleted = true;
        entity.DateAdded = testDate;

        // Assert
        entity.PartitionKey.Should().Be("pk-test");
        entity.RowKey.Should().Be("rk-test");
        entity.Title.Should().Be("My Entity");
        entity.IsDeleted.Should().BeTrue();
        entity.DateAdded.Should().Be(testDate);
    }

    [Fact]
    public void ETag_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new BaseTableEntity();
        var etag = new global::Azure.ETag("test-etag");

        // Act
        entity.ETag = etag;

        // Assert
        entity.ETag.Should().Be(etag);
    }

    [Fact]
    public void Timestamp_CanBeSetAndRetrieved()
    {
        // Arrange
        var entity = new BaseTableEntity();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        entity.Timestamp = timestamp;

        // Assert
        entity.Timestamp.Should().Be(timestamp);
    }
}
