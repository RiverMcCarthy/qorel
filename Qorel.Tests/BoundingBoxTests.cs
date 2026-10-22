namespace Qorel.Tests;

public class BoundingBoxTests
{
    [Fact]
    public void Create_ValidCoordinates_ReturnsBox()
    {
        var box = BoundingBox.Create(10.0, -20.0, 50.0, 30.0);
        box.MinLatitude.Should().Be(10.0);
        box.MinLongitude.Should().Be(-20.0);
        box.MaxLatitude.Should().Be(50.0);
        box.MaxLongitude.Should().Be(30.0);
    }

    [Theory]
    [InlineData(-91, 0, 10, 10)]    // minLat below -90
    [InlineData(10, 0, 91, 10)]     // maxLat above 90
    [InlineData(50, 0, 10, 10)]     // minLat > maxLat
    [InlineData(10, -181, 50, 10)]  // minLng below -180
    [InlineData(10, 0, 50, 181)]    // maxLng above 180
    [InlineData(10, 30, 50, 10)]    // minLng > maxLng
    [InlineData(double.NaN, 0, 10, 10)]    // NaN latitude
    [InlineData(10, 0, 50, double.NaN)]    // NaN longitude
    public void Create_InvalidCoordinates_ThrowsArgumentOutOfRangeException(
        double minLat, double minLng, double maxLat, double maxLng)
    {
        Func<BoundingBox> act = () => BoundingBox.Create(minLat, minLng, maxLat, maxLng);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_EqualMinMax_IsValid()
    {
        Func<BoundingBox> act = () => BoundingBox.Create(10.0, 20.0, 10.0, 20.0);
        act.Should().NotThrow();
    }
}
