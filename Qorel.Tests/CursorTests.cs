using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

public class CursorTests
{
    private const int MaxLength = 4096;

    private static readonly BuiltModel<TestEntity, TestField> _model = new TestQueryModel().Built;

    private static readonly SortKey<TestEntity, TestField>[] _priceThenId =
    [
        new(_model.GetField(TestField.Price), SortDirection.Desc),
        new(_model.GetField(TestField.Id), SortDirection.Asc),
    ];

    private static readonly Guid _id = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    [Fact]
    public void Encode_ThenDecode_GivesBackTheValues()
    {
        string cursor = Cursor.Encode(_priceThenId, [19.99m, _id]);

        Cursor.TryDecode(cursor, _priceThenId, MaxLength, out object?[] values).Should().BeTrue();
        values.Should().Equal(19.99m, _id);
    }

    [Fact]
    public void Encode_IsUrlSafe()
        => Cursor.Encode(_priceThenId, [999999.99m, Guid.NewGuid()]).Should().MatchRegex("^[A-Za-z0-9_-]+$");

    [Fact]
    public void NullValues_RoundTrip()
    {
        SortKey<TestEntity, TestField>[] nicknameThenId = [new(_model.GetField(TestField.Nickname), SortDirection.Asc), _priceThenId[1]];

        string cursor = Cursor.Encode(nicknameThenId, [null, _id]);

        Cursor.TryDecode(cursor, nicknameThenId, MaxLength, out object?[] values).Should().BeTrue();
        values.Should().Equal(null, _id);
    }

    [Fact]
    public void Decode_ForDifferentSorts_Fails()
    {
        SortKey<TestEntity, TestField>[] ascending = [_priceThenId[0] with { Direction = SortDirection.Asc }, _priceThenId[1]];

        string cursor = Cursor.Encode(_priceThenId, [19.99m, _id]);

        Cursor.TryDecode(cursor, ascending, MaxLength, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-cursor!!")]
    [InlineData("bm90IGpzb24")]                                         // "not json"
    [InlineData("eyJzb3J0IjoiUHJpY2U6ZGVzYyxJZDphc2MifQ")]              // no values
    [InlineData("eyJzb3J0IjoiUHJpY2U6ZGVzYyxJZDphc2MiLCJ2YWx1ZXMiOlsieCIsInkiXX0")] // values of the wrong type
    public void Decode_Malformed_Fails(string cursor)
        => Cursor.TryDecode(cursor, _priceThenId, MaxLength, out _).Should().BeFalse();

    [Fact]
    public void Decode_TooLong_Fails()
    {
        string cursor = Cursor.Encode(_priceThenId, [19.99m, _id]);

        Cursor.TryDecode(cursor, _priceThenId, cursor.Length - 1, out _).Should().BeFalse();
    }
}
