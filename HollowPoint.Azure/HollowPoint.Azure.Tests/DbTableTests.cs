using FluentAssertions;
using HollowPoint.Azure;
using HollowPoint.Azure.Configuration;
using HollowPoint.Azure.Interfaces;
using Moq;
using Azure.Data.Tables;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Azure;

namespace HollowPoint.Azure.Tests;

public class DbTableTests
{
    private readonly Mock<TableServiceClient> _mockServiceClient;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly IOptions<AzureTableOptions> _options;
    private readonly IDbTable<TestEntity> _dbTable;

    public DbTableTests()
    {
        _mockServiceClient = new Mock<TableServiceClient>();
        _mockTableClient = new Mock<TableClient>();
        _mockCache = new Mock<IMemoryCache>();

        _options = Options.Create(new AzureTableOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            EnableCaching = false // Disable caching for predictable tests
        });

        // Setup TableServiceClient to return mock TableClient
        _mockServiceClient
            .Setup(x => x.GetTableClient(It.IsAny<string>()))
            .Returns(_mockTableClient.Object);

        // Setup TableClient.CreateIfNotExists to succeed
        var mockTableResponse = new Mock<Response<global::Azure.Data.Tables.Models.TableItem>>();
        _mockTableClient
            .Setup(x => x.CreateIfNotExists(It.IsAny<CancellationToken>()))
            .Returns(mockTableResponse.Object);

        _dbTable = new DbTable<TestEntity>(
            _mockServiceClient.Object,
            _mockCache.Object,
            _options);
    }

    [Fact]
    public void Constructor_WithNullServiceClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new DbTable<TestEntity>(null!, _mockCache.Object, _options);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceClient");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new DbTable<TestEntity>(_mockServiceClient.Object, null!, _options);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new DbTable<TestEntity>(_mockServiceClient.Object, _mockCache.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Add_WithValidEntity_CallsTableClientAddEntity()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk",
            Title = "Test"
        };

        _mockTableClient
            .Setup(x => x.AddEntity(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Returns((Response)null!);

        // Act
        _dbTable.Add(entity);

        // Assert
        _mockTableClient.Verify(x => x.AddEntity(
            It.Is<TestEntity>(e => e.PartitionKey == "pk" && e.RowKey == "rk"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Add_WithEmptyPartitionKey_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "",
            RowKey = "rk"
        };

        // Act & Assert
        Action act = () => _dbTable.Add(entity);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*PartitionKey*");
    }

    [Fact]
    public void Add_WithEmptyRowKey_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = ""
        };

        // Act & Assert
        Action act = () => _dbTable.Add(entity);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*RowKey*");
    }

    [Fact]
    public void Update_WithValidEntity_CallsTableClientUpdateEntity()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk",
            Title = "Updated",
            ETag = new ETag("test-etag")
        };

        _mockTableClient
            .Setup(x => x.UpdateEntity(
                It.IsAny<TestEntity>(),
                It.IsAny<ETag>(),
                It.IsAny<TableUpdateMode>(),
                It.IsAny<CancellationToken>()))
            .Returns((Response)null!);

        // Act
        _dbTable.Update(entity);

        // Assert
        _mockTableClient.Verify(x => x.UpdateEntity(
            It.Is<TestEntity>(e => e.Title == "Updated"),
            It.IsAny<ETag>(),
            TableUpdateMode.Replace,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Delete_WithValidEntity_CallsTableClientDeleteEntity()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk",
            ETag = new ETag("test-etag")
        };

        _mockTableClient
            .Setup(x => x.DeleteEntity(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()))
            .Returns((Response)null!);

        // Act
        _dbTable.Delete(entity);

        // Assert
        _mockTableClient.Verify(x => x.DeleteEntity(
            "pk",
            "rk",
            It.IsAny<ETag>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Get_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var expectedEntity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk",
            Title = "Found"
        };

        var mockResponse = new Mock<Response<TestEntity>>();
        mockResponse.Setup(x => x.Value).Returns(expectedEntity);

        _mockTableClient
            .Setup(x => x.GetEntity<TestEntity>(
                "pk",
                "rk",
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockResponse.Object);

        // Act
        var result = _dbTable.Get("pk", "rk");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Found");
    }

    [Fact]
    public void Get_WithNonExistentEntity_ReturnsNull()
    {
        // Arrange
        _mockTableClient
            .Setup(x => x.GetEntity<TestEntity>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException(404, "Not found"));

        // Act
        var result = _dbTable.Get("pk", "rk");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Query_ReturnsAzureTableQueryable()
    {
        // Act
        var result = _dbTable.Query();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_CallsTableClientAddEntityAsync()
    {
        // Arrange
        var entity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk"
        };

        var mockResponse = new Mock<Response>();
        _mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<TestEntity>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        await _dbTable.AddAsync(entity);

        // Assert
        _mockTableClient.Verify(x => x.AddEntityAsync(
            It.Is<TestEntity>(e => e.PartitionKey == "pk"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var expectedEntity = new TestEntity
        {
            PartitionKey = "pk",
            RowKey = "rk",
            Title = "Async Found"
        };

        var mockResponse = new Mock<Response<TestEntity>>();
        mockResponse.Setup(x => x.Value).Returns(expectedEntity);

        _mockTableClient
            .Setup(x => x.GetEntityAsync<TestEntity>(
                "pk",
                "rk",
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _dbTable.GetAsync("pk", "rk");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Async Found");
    }

    [Fact]
    public async Task GetAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Arrange
        _mockTableClient
            .Setup(x => x.GetEntityAsync<TestEntity>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not found"));

        // Act
        var result = await _dbTable.GetAsync("pk", "rk");

        // Assert
        result.Should().BeNull();
    }
}
