using FluentAssertions;
using HollowPoint.Azure.Tools;
using System.Linq.Expressions;

namespace HollowPoint.Azure.Tests;

public class ODataTranslatorTests
{
    private readonly ODataTranslator _translator;

    public ODataTranslatorTests()
    {
        _translator = new ODataTranslator();
    }

    [Fact]
    public void Translate_SimpleEqualityExpression_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.PartitionKey == "test";

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(PartitionKey eq 'test')");
    }

    [Fact]
    public void Translate_AndExpression_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.PartitionKey == "test" && e.Count > 10;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("((PartitionKey eq 'test') and (Count gt 10))");
    }

    [Fact]
    public void Translate_OrExpression_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.IsDeleted == false || e.Count < 5;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("((IsDeleted eq 0) or (Count lt 5))");
    }

    [Fact]
    public void Translate_ComplexExpression_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e =>
            (e.PartitionKey == "test" && e.Count > 10) || e.IsDeleted == true;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(((PartitionKey eq 'test') and (Count gt 10)) or (IsDeleted eq 1))");
    }

    [Fact]
    public void Translate_GreaterThanOrEqual_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Count >= 100;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Count ge 100)");
    }

    [Fact]
    public void Translate_LessThanOrEqual_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Count <= 50;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Count le 50)");
    }

    [Fact]
    public void Translate_NotEqual_ReturnsCorrectODataFilter()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Title != "test";

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Title ne 'test')");
    }

    [Fact]
    public void Translate_StringProperty_QuotesValue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Title == "Hello World";

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Title eq 'Hello World')");
    }

    [Fact]
    public void Translate_NumericProperty_DoesNotQuoteValue()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Count == 42;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Count eq 42)");
    }

    [Fact]
    public void Translate_BooleanTrue_Converts1()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.IsDeleted == true;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(IsDeleted eq 1)");
    }

    [Fact]
    public void Translate_BooleanFalse_Converts0()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.IsDeleted == false;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(IsDeleted eq 0)");
    }

    [Fact]
    public void Translate_WhereWithTake_SetsCorrectTakeValue()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.Count > 5)
            .Take(10);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.Take.Should().Be(10);
    }

    [Fact]
    public void Translate_WhereWithSkip_SetsCorrectSkipValue()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.Count > 5)
            .Skip(20);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.Skip.Should().Be(20);
    }

    [Fact]
    public void Translate_WhereWithTakeAndSkip_SetsBothValues()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.Count > 5)
            .Skip(10)
            .Take(5);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.Skip.Should().Be(10);
        _translator.Take.Should().Be(5);
    }

    [Fact]
    public void Translate_DateTimeComparison_QuotesValue()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        Expression<Func<TestEntity, bool>> expression = e => e.DateAdded > testDate;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Contain("DateAdded gt '");
        result.Should().Contain("2024");
    }

    [Fact]
    public void Translate_MultipleConditionsWithParentheses_PreservesLogic()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e =>
            e.PartitionKey == "pk1" && (e.Count > 10 || e.Count < 5);

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Contain("and");
        result.Should().Contain("or");
        result.Should().Contain("(PartitionKey eq 'pk1')");
    }

    [Fact]
    public void Translate_NullComparison_UsesIsOperator()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Title == null;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Contain("IS");
        result.Should().Contain("NULL");
    }

    [Fact]
    public void Translate_NotNullComparison_UsesIsNotOperator()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Title != null;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Contain("IS NOT");
        result.Should().Contain("NULL");
    }

    [Fact]
    public void Translate_LocalVariableComparison_IncludesValue()
    {
        // Arrange
        var localValue = "test-value";
        Expression<Func<TestEntity, bool>> expression = e => e.Title == localValue;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Title eq 'test-value')");
    }

    [Fact]
    public void Translate_LocalNumericVariableComparison_IncludesValue()
    {
        // Arrange
        var threshold = 100;
        Expression<Func<TestEntity, bool>> expression = e => e.Count > threshold;

        // Act
        var result = _translator.Translate(expression);

        // Assert
        result.Should().Be("(Count gt 100)");
    }

    [Fact]
    public void Translate_OrderByAscending_SetsOrderByProperty()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.Count > 5)
            .OrderBy(e => e.Title);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.OrderBy.Should().Be("Title asc");
    }

    [Fact]
    public void Translate_OrderByDescending_SetsOrderByProperty()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.Count > 5)
            .OrderByDescending(e => e.DateAdded);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.OrderBy.Should().Be("DateAdded desc");
    }

    [Fact]
    public void Translate_MultipleOrderBy_ChainsOrderClauses()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.IsDeleted == false)
            .OrderBy(e => e.PartitionKey)
            .OrderByDescending(e => e.Count);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        // Note: This implementation accumulates OrderBy clauses (like ThenBy behavior)
        // First OrderBy is processed last due to expression tree structure
        _translator.OrderBy.Should().Be("Count desc, PartitionKey asc");
    }

    [Fact]
    public void Translate_OrderByWithoutWhere_SetsOrderByProperty()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .OrderBy(e => e.Title);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.OrderBy.Should().Be("Title asc");
    }

    [Fact]
    public void Translate_OrderByWithTakeAndSkip_SetsAllProperties()
    {
        // Arrange
        var query = new[] { new TestEntity() }.AsQueryable()
            .Where(e => e.IsDeleted == false)
            .OrderBy(e => e.DateAdded)
            .Skip(10)
            .Take(20);

        // Act
        var result = _translator.Translate(query.Expression);

        // Assert
        _translator.OrderBy.Should().Be("DateAdded asc");
        _translator.Skip.Should().Be(10);
        _translator.Take.Should().Be(20);
        result.Should().Contain("IsDeleted eq 0");
    }
}
