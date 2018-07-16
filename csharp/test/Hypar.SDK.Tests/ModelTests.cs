using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Hypar;
using Hypar.Elements;
using Hypar.Geometry;
using Xunit.Abstractions;

namespace Hypar.Tests
{
    public class ModelTests
    {
        [Fact]
        public void Default_Construct_NoParameters_Success()
        {
            var model = new Model();
            Assert.NotNull(model);
        }

        [Fact]
        public void TestModel_SaveToGltf_Success()
        {
            var model = QuadPanelModel();
            model.SaveGltf("saveToGltf.gltf");
            Assert.True(File.Exists("saveToGltf.gltf"));
        }

        [Fact]
        public void TestModel_SaveToGlb_Success()
        {
            var model = QuadPanelModel();
            model.SaveGlb("saveToGlb.glb");
            Assert.True(File.Exists("saveToGlb.glb"));
        }

        [Fact]
        public void Box_SaveToGltf_Success()
        {
            var model = new Model();
            model.AddElement(new Box());
            model.SaveGltf("box.gltf");
            Assert.True(File.Exists("box.gltf"));
        }

        private Model QuadPanelModel()
        {
            var model = new Model();
            var a = new Vector3(0,0,0);
            var b = new Vector3(1,0,0);
            var c = new Vector3(1,0,1);
            var d = new Vector3(0,0,1);
            var panel = Panel.WithinPerimeter(new Polyline(new[]{a,b,c,d}))
                                .OfMaterial(BuiltInMaterials.Glass);
            model.AddElement(panel);
            return model;
        }
    }
}