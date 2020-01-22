using System;
using Elements.Geometry;
using Elements.Serialization.JSON;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elements.Tests
{
    public class PropertyBindingTests
    {
        private ITestOutputHelper _output;
        public PropertyBindingTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public void PropertyBinding()
        {
            var line = new Line(Vector3.Origin, new Vector3(5,5,5));
            var beam = new Beam(line, Polygon.Rectangle(1,1));
            var binding1 = new PropertyBinding<Polygon>(beam, "Profile.Perimeter");
            Assert.Equal(beam.Profile.Perimeter, binding1.Value());

            Assert.Throws<ArgumentException>(() => new PropertyBinding<Polygon>(beam, "Foo"));

            var json = JsonConvert.SerializeObject(binding1);
            _output.WriteLine(json);
        }
    }
}