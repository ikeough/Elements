using Elements.Geometry;
using Xunit;

namespace Elements.Tests
{
    public class GeometryCreateTests : ModelTest
    {
        [Fact]
        public void Example()
        {
            var badLine = Geometry.Create.Line(new[] { 0.0, 0.0, 0.0 }, new[] { 0.0, 0.0, 0.0 }, out GeometryCreationException lineException);
            Assert.Equal(1, badLine.Length());

            var circle = Geometry.Create.Circle(Vector3.Origin, -1, out GeometryCreationException circleException);
            Assert.Equal(1, circle.Radius);

            var goodLine = Geometry.Create.Line(Vector3.Origin, new Vector3(5, 0, 0), out GeometryCreationException secondLineException);
            Assert.Equal(5, goodLine.Length());
        }
    }
}