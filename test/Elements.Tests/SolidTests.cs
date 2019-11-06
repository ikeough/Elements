using Xunit;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Geometry.Profiles;
using Newtonsoft.Json;
using Xunit.Abstractions;
using System.Collections.Generic;
using System;
using System.IO;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;

namespace Elements.Tests
{
    public class SolidTests
    {
        private readonly ITestOutputHelper output;
        
        public SolidTests(ITestOutputHelper output)
        {
            this.output = output;

            if(!Directory.Exists("models"))
            {
                Directory.CreateDirectory("models");
            }
        }

        [Fact]
        public void SweptSolid()
        {
            var n = 4;
            var outer = Polygon.Ngon(n, 2);
            var inner = Polygon.Ngon(n, 1.75).Reversed();

            var solid = Solid.SweepFace(outer, new[]{inner}, 5);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            Assert.Equal(2 * n + 2, solid.Faces.Count);
            Assert.Equal(n * 6, solid.Edges.Count);
            Assert.Equal(n * 4, solid.Vertices.Count);
            solid.ToGlb("models/SweptSolid.glb");
        }

        [Fact]
        public void Slice()
        {
            var n = 3;
            var outer = Polygon.Ngon(n, 2);
            var solid = Solid.SweepFace(outer, new Polygon[]{}, 5);
            var slicePlane = new Plane(new Vector3(0,0,2.5), new Vector3(0.1,0.1,1.0));
            solid.Slice(slicePlane);

            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }

            // Console.WriteLine(solid.ToString());
            // Assert.Equal(2 * n + 2, solid.Faces.Count);
            // Assert.Equal(n * 6, solid.Edges.Count);
            // Assert.Equal(n * 4, solid.Vertices.Count);
            solid.ToGlb("models/SliceSolid.glb");
        }

        [Fact]
        public void SweptSolidAngle()
        {
            var n = 4;
            var outer = Polygon.Ngon(n, 2);
            var inner = Polygon.Ngon(n, 1.75).Reversed();

            var solid = Solid.SweepFace(outer, new[]{inner}, new Vector3(0.5,0.5,0.5), 5);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            Assert.Equal(2 * n + 2, solid.Faces.Count);
            Assert.Equal(n * 6, solid.Edges.Count);
            Assert.Equal(n * 4, solid.Vertices.Count);
            solid.ToGlb("models/SweptSolidAngle.glb");
        }

        [Fact]
        public void SweptSolidTransformToStart()
        {
            var n = 4;
            var outer = Polygon.Ngon(n, 2);
            var inner = Polygon.Ngon(n, 1.75).Reversed();

            var solid = Solid.SweepFace(outer, new[]{inner}, new Vector3(0.5,0.5,0.5), 5);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            Assert.Equal(2 * n + 2, solid.Faces.Count);
            Assert.Equal(n * 6, solid.Edges.Count);
            Assert.Equal(n * 4, solid.Vertices.Count);
            solid.ToGlb("models/SweptSolidTransformToStart.glb");
        }

        [Fact]
        public void SweptSolidPolyline()
        {
            var profile = WideFlangeProfileServer.Instance.GetProfileByName("W44x335");
            var path = new Polyline(new []{new Vector3(0,0), new Vector3(0,2), new Vector3(0,3,1), new Vector3(0,5,1)});
            var solid = Solid.SweepFaceAlongCurve(profile.Perimeter, null, path);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            solid.ToGlb("models/SweptSolidPolyline.glb");
        }

        [Fact]
        public void SweptSolidArc()
        {
            var profile = WideFlangeProfileServer.Instance.GetProfileByName("W44x335");
            var path = new Arc(Vector3.Origin, 5, 0, 90);
            var solid = Solid.SweepFaceAlongCurve(profile.Perimeter, null, path);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            solid.ToGlb("models/SweptSolidArc.glb");
        }

        [Fact]
        public void SweptSolidPolygon()
        {
            var profile = WideFlangeProfileServer.Instance.GetProfileByName("W44x335");
            var path = Polygon.Ngon(12, 5);
            var solid = Solid.SweepFaceAlongCurve(profile.Perimeter, null, path);
            foreach(var e in solid.Edges)
            {
                Assert.NotNull(e.Left);
                Assert.NotNull(e.Right);
                Assert.NotSame(e.Left, e.Right);
            }
            solid.ToGlb("models/SweptSolidPolygon.glb");
        }
    
        [Fact]
        public void Serialization()
        {
            var n = 4;
            var outer = Polygon.Ngon(n, 2);
            var solid = Solid.SweepFace(outer, null, 2.0);
            var materials = new Dictionary<Guid,Material>();
            var defMaterial = BuiltInMaterials.Default;
            materials.Add(defMaterial.Id, defMaterial);
            var json = JsonConvert.SerializeObject(solid, new JsonSerializerSettings(){
                Converters = new []{new SolidConverter(materials)},
                Formatting = Formatting.Indented
            });
            var newSolid = JsonConvert.DeserializeObject<Solid>(json, new JsonSerializerSettings(){
                Converters = new []{new SolidConverter(materials)}
            });
            Assert.Equal(8, newSolid.Vertices.Count);
            Assert.Equal(12, newSolid.Edges.Count);
            Assert.Equal(6, newSolid.Faces.Count);
            newSolid.ToGlb("models/SweptSolidDeserialized.glb");
        }

        [Fact]
        public void SplitQuadDiamond()
        {
            var solid = new Solid();
            var poly = Polygon.Ngon(4,5);
            var f = solid.AddFace(poly);
            var p = new Plane(Vector3.Origin, Vector3.XAxis);
        
            solid.SplitFace(f, p);
            Assert.Equal(5, solid.Edges.Count);
            Assert.Equal(4, solid.Vertices.Count);
            Assert.Equal(2, solid.Faces.Count);
            
            solid.ToGlb("models/SplitQuadDiamond.glb");
        }

        [Fact]
        public void SplitTriangle()
        {
            var solid = new Solid();
            var poly = Polygon.Ngon(3,5);
            var f = solid.AddFace(poly);
            var p = new Plane(Vector3.Origin, Vector3.XAxis);
        
            solid.SplitFace(f, p);
            Assert.Equal(6, solid.Edges.Count);
            Assert.Equal(5, solid.Vertices.Count);
            Assert.Equal(2, solid.Faces.Count);
            
            solid.ToGlb("models/SplitTriangle.glb");
        }

        [Fact]
        public void SplitCrown()
        {
            var solid = new Solid();
            var a = new Vector3(5,0,0);
            var b = new Vector3(5,3,0);
            var c = new Vector3(0,1,0);
            var d = new Vector3(-5,3,0);
            var e = new Vector3(-5,0,0);
            var poly = new Polygon(new[]{a,b,c,d,e});
            var f = solid.AddFace(poly);

            var p = new Plane(new Vector3(0,2,0), Vector3.YAxis);

            solid.SplitFace(f, p);

            Assert.Equal(9, solid.Vertices.Count);
            Assert.Equal(11, solid.Edges.Count);
            Assert.Equal(3, solid.Faces.Count);

            solid.ToGlb("models/SplitCrown.glb");
        }

        [Fact]
        public void DoNotSplitOnCoincidentEdge()
        {
            var solid = new Solid();
            var p = Polygon.Rectangle(10, 10);
            solid.AddFace(p);
            // A plane that will be parallel to the left 
            // edge of the rectangle.
            var s = new Plane(new Vector3(-5,0,0), Vector3.XAxis);
            solid.Slice(s);
            Assert.Equal(1, solid.Faces.Count);
        }

        [Fact]
        public void MultiSliceTest()
        {
            var solid = new Solid();
            var p = Polygon.Rectangle(10, 10);
            solid.AddFace(p);
            var r = new Random();
            for(var x = -5;x < 5; x++)
            {
                var s = new Plane(new Vector3(x,0,0), Vector3.XAxis);
                solid.Slice(s);
            }

            // for(var y=-5; y<5; y++)
            // {
            //     var s = new Plane(new Vector3(y,0,0), Vector3.YAxis);
            //     solid.Slice(s);
            // }

            solid.ToGlb("models/SliceMulti.glb");
        }
    }

}