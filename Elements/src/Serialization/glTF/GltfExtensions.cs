using Elements.Geometry;
using System;
using System.Collections.Generic;
// TODO: Get rid of System.Linq
using System.Linq;
using glTFLoader;
using glTFLoader.Schema;
using System.IO;
using System.Runtime.CompilerServices;
using Elements.Geometry.Solids;
using Elements.Geometry.Interfaces;
using SixLabors.ImageSharp.Processing;
using Elements.Collections.Generics;
using Elements.Interfaces;

[assembly: InternalsVisibleTo("Hypar.Elements.Tests")]
[assembly: InternalsVisibleTo("Elements.Benchmarks")]

namespace Elements.Serialization.glTF
{
    /// <summary>
    /// Extensions for glTF serialization.
    /// </summary>
    public static class GltfExtensions
    {
        private static int _currentId = -1;

        private static int GetNextId()
        {
            _currentId++;
            return _currentId;
        }

        private const string emptyGltf = @"{
    ""asset"": {""version"": ""2.0""},
    ""nodes"": [{""name"": ""empty""}],
    ""scenes"": [{""nodes"": [0]}],
    ""scene"": 0
}";

        /// <summary>
        /// Save a model to gltf.
        /// If there is no geometry, an empty GLTF will still be produced.
        /// </summary>
        /// <param name="model">The model to serialize.</param>
        /// <param name="path">The output path.</param>
        /// <param name="useBinarySerialization">Should binary serialization be used?</param>
        /// <param name="drawEdges">Should the solid edges be written to the gltf?</param>
        public static void ToGlTF(this Model model, string path, bool useBinarySerialization = true, bool drawEdges = false)
        {
            if (model.Elements.Count > 0)
            {
                if (useBinarySerialization)
                {
                    if (SaveGlb(model, path, drawEdges))
                    {
                        return;
                    }
                    // Else fall through to produce an empty GLTF.
                }
                else
                {
                    if (SaveGltf(model, path, drawEdges))
                    {
                        return;
                    }
                    // Else fall through to produce an empty GLTF.
                }
            }

            // There are no elements that produced geometry. Write an empty GLTF.
            File.WriteAllText(path, emptyGltf);
        }

        /// <summary>
        /// Convert the Model to a base64 encoded string.
        /// </summary>
        /// <returns>A Base64 string representing the Model.</returns>
        public static string ToBase64String(this Model model, bool drawEdges = false)
        {
            var tmp = Path.GetTempFileName();
            var gltf = InitializeGlTF(model, out var buffers, drawEdges);
            if (gltf == null)
            {
                return "";
            }
            var mergedBuffer = gltf.CombineBufferAndFixRefs(buffers.ToArray(buffers.Count));
            gltf.SaveBinaryModel(mergedBuffer, tmp);
            var bytes = File.ReadAllBytes(tmp);
            return Convert.ToBase64String(bytes);
        }

        internal static Dictionary<string, int> AddMaterials(this Gltf gltf, IList<Material> materials, List<byte> buffer, List<BufferView> bufferViews)
        {
            var materialDict = new Dictionary<string, int>();
            var newMaterials = new List<glTFLoader.Schema.Material>();

            var textureDict = new Dictionary<string, int>(); // the name of the texture image, the id of the texture
            var textures = new List<glTFLoader.Schema.Texture>();

            var images = new List<glTFLoader.Schema.Image>();
            var samplers = new List<glTFLoader.Schema.Sampler>();

            var matId = 0;
            var texId = 0;
            var imageId = 0;
            var samplerId = 0;

            foreach (var material in materials)
            {
                if (materialDict.ContainsKey(material.Name))
                {
                    continue;
                }

                var m = new glTFLoader.Schema.Material();
                newMaterials.Add(m);

                m.PbrMetallicRoughness = new MaterialPbrMetallicRoughness();
                m.PbrMetallicRoughness.BaseColorFactor = material.Color.ToArray();
                m.PbrMetallicRoughness.MetallicFactor = 1.0f;
                m.DoubleSided = material.DoubleSided;
                m.Name = material.Name;

                if (material.Unlit)
                {
                    m.Extensions = new Dictionary<string, object>{
                        {"KHR_materials_unlit", new Dictionary<string, object>{}}
                    };
                }
                else
                {
                    m.Extensions = new Dictionary<string, object>{
                        {"KHR_materials_pbrSpecularGlossiness", new Dictionary<string,object>{
                            {"diffuseFactor", new[]{material.Color.Red,material.Color.Green,material.Color.Blue,material.Color.Alpha}},
                            {"specularFactor", new[]{material.SpecularFactor, material.SpecularFactor, material.SpecularFactor}},
                            {"glossinessFactor", material.GlossinessFactor}
                        }}
                    };
                }

                if (material.Texture != null && File.Exists(material.Texture))
                {
                    // Add the texture
                    var ti = new TextureInfo();
                    m.PbrMetallicRoughness.BaseColorTexture = ti;
                    ti.Index = texId;
                    ti.TexCoord = 0;
                    ((Dictionary<string, object>)m.Extensions["KHR_materials_pbrSpecularGlossiness"])["diffuseTexture"] = ti;

                    if (textureDict.ContainsKey(material.Texture))
                    {
                        ti.Index = textureDict[material.Texture];
                    }
                    else
                    {
                        var tex = new Texture();
                        textures.Add(tex);

                        var image = new glTFLoader.Schema.Image();

                        using (var ms = new MemoryStream())
                        {
                            // Flip the texture image vertically
                            // to align with OpenGL convention.
                            // 0,1  1,1
                            // 0,0  1,0
                            using (var texImage = SixLabors.ImageSharp.Image.Load(material.Texture))
                            {
                                texImage.Mutate(x => x.Flip(FlipMode.Vertical));
                                texImage.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                            }
                            var imageData = ms.ToArray();
                            image.BufferView = AddBufferView(bufferViews, 0, buffer.Count, imageData.Length, null, null);
                            buffer.AddRange(imageData);
                        }

                        while (buffer.Count % 4 != 0)
                        {
                            // Console.WriteLine("Padding...");
                            buffer.Add(0);
                        }

                        image.MimeType = glTFLoader.Schema.Image.MimeTypeEnum.image_png;
                        tex.Source = imageId;
                        images.Add(image);

                        var sampler = new Sampler();
                        sampler.MagFilter = Sampler.MagFilterEnum.LINEAR;
                        sampler.MinFilter = Sampler.MinFilterEnum.LINEAR;
                        sampler.WrapS = Sampler.WrapSEnum.REPEAT;
                        sampler.WrapT = Sampler.WrapTEnum.REPEAT;
                        tex.Sampler = samplerId;
                        samplers.Add(sampler);

                        textureDict.Add(material.Texture, texId);

                        texId++;
                        imageId++;
                        samplerId++;
                    }
                }

                if (material.Color.Alpha < 1.0)
                {
                    m.AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.BLEND;
                }
                else
                {
                    m.AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE;
                }

                materialDict.Add(m.Name, matId);
                matId++;
            }

            if (materials.Count > 0)
            {
                gltf.Materials = newMaterials.ToArray(newMaterials.Count);
            }
            if (textures.Count > 0)
            {
                gltf.Textures = textures.ToArray(textures.Count);
            }
            if (images.Count > 0)
            {
                gltf.Images = images.ToArray(images.Count);
            }
            if (samplers.Count > 0)
            {
                gltf.Samplers = samplers.ToArray(samplers.Count);
            }

            return materialDict;
        }

        internal static void AddLights(this Gltf gltf, List<DirectionalLight> lights, List<Node> nodes)
        {
            gltf.Extensions = new Dictionary<string, object>();
            var lightCount = 0;
            var lightsArr = new List<object>();
            foreach (var light in lights)
            {
                // Create the top level collection of lights.
                var gltfLight = new Dictionary<string, object>(){
                    {"color", new[]{light.Color.Red, light.Color.Green, light.Color.Blue}},
                    {"type", "directional"},
                    {"intensity", light.Intensity},
                    {"name", light.Name != null ? light.Name : string.Empty}
                };
                lightsArr.Add(gltfLight);

                // Create the light nodes
                var lightNode = new Node();
                lightNode.Extensions = new Dictionary<string, object>(){
                    {"KHR_lights_punctual", new Dictionary<string,object>(){
                        {"light", lightCount}
                    }}
                };

                var ml = light.Transform.Matrix;
                lightNode.Matrix = new float[]{
                (float)ml.m11, (float)ml.m12, (float)ml.m13, 0f,
                (float)ml.m21, (float)ml.m22, (float)ml.m23, 0f,
                (float)ml.m31, (float)ml.m32, (float)ml.m33, 0f,
                (float)ml.tx, (float)ml.ty, (float)ml.tz, 1f};

                gltf.AddNode(nodes, lightNode, 0);
                lightCount++;
            }

            if (lightsArr.Count > 0)
            {
                gltf.Extensions.Add("KHR_lights_punctual", new Dictionary<string, object>{
                    {"lights", lightsArr}
                });
            }
        }

        internal static void AddAnimations(this Gltf gltf, List<Element> elements, Dictionary<Guid, int> elementNodeMap, List<byte> buffer, List<BufferView> bufferViews, List<Accessor> accesors)
        {
            var groups = elements.Where(e => e.AssemblyInstruction != null).GroupBy(e => e.AssemblyInstruction.Order);
            var animations = new List<Animation>();
            // Create a buffer view representing all the animation input and output data.
            // Create a buffer including all the input data.
            // Create a buffer including all the output data.
            // Create accessors which grab the input and output data.
            var inputBuffer = new List<byte>();
            var outputBuffer = new List<byte>();

            var time = 0;
            foreach (var group in groups)
            {
                var bufferViewId = AddBufferView(bufferViews, 0,  )
                var timeMin = time;
                var timeMax = time + 2.0f;
                Vector3 transformMin = new Vector3();
                Vector3 transformMax = new Vector3();

                var inputAccessorId = AddAccessor(accesors, bufferViewId, 0, Accessor.ComponentTypeEnum.FLOAT, 10, new float[] { timeMin }, new float[] { timeMax }, Accessor.TypeEnum.SCALAR);
                var outputAccesorId = AddAccessor(accesors, bufferViewId, 10, Accessor.ComponentTypeEnum.FLOAT, 10, transformMin, transformMax, Accessor.TypeEnum.VEC3);

                var animation = new Animation();
                animation.Samplers = new AnimationSampler[] {
                    new AnimationSampler() {
                        Input = inputAccessorId,
                        Output = outputAccesorId,
                        Interpolation = AnimationSampler.InterpolationEnum.LINEAR
                    }
                };
                animation.Channels = new AnimationChannel[]{
                    new AnimationChannel() {
                        Sampler = samplerId,
                        Target = new AnimationChannelTarget() {
                            Node = elementNodeMap[group]
                        }
                    }
                }
            }
        }

        private static int AddAccessor(List<Accessor> accessors, int bufferView, int byteOffset, Accessor.ComponentTypeEnum componentType, int count, float[] min, float[] max, Accessor.TypeEnum accessorType)
        {
            var a = new Accessor();
            a.BufferView = bufferView;
            a.ByteOffset = byteOffset;
            a.ComponentType = componentType;
            a.Min = min;
            a.Max = max;
            a.Type = accessorType;
            a.Count = count;

            accessors.Add(a);

            return accessors.Count - 1;
        }

        private static int AddBufferView(List<BufferView> bufferViews, int buffer, int byteOffset, int byteLength, BufferView.TargetEnum? target, int? byteStride)
        {
            var b = new BufferView();
            b.Buffer = buffer;
            b.ByteLength = byteLength;
            b.ByteOffset = byteOffset;
            b.Target = target;
            b.ByteStride = byteStride;

            bufferViews.Add(b);

            return bufferViews.Count - 1;
        }

        private static int AddNode(this Gltf gltf, List<Node> nodes, Node n, int? parent)
        {
            nodes.Add(n);
            var id = nodes.Count - 1;

            if (parent != null)
            {
                if (nodes[(int)parent].Children == null)
                {
                    nodes[(int)parent].Children = new[] { id };
                }
                else
                {
                    // TODO: Get rid of this resizing.
                    var children = nodes[(int)parent].Children.ToList();
                    children.Add(id);
                    nodes[(int)parent].Children = children.ToArray(children.Count);
                }

            }

            return id;
        }

        internal static void AddInstanceMesh(this Gltf gltf,
                                            List<glTFLoader.Schema.Node> nodes,
                                            List<int> meshIds,
                                            Transform transform)
        {
            var a = transform.XAxis;
            var b = transform.YAxis;
            var c = transform.ZAxis;


            var matrix = new[]{
                    (float)a.X, (float)a.Y, (float)a.Z, 0.0f,
                    (float)b.X, (float)b.Y, (float)b.Z, 0.0f,
                    (float)c.X, (float)c.Y, (float)c.Z, 0.0f,
                    (float)transform.Origin.X,(float)transform.Origin.Y,(float)transform.Origin.Z, 1.0f
                };

            foreach (var meshId in meshIds)
            {
                var node = new Node();
                node.Matrix = matrix;
                node.Mesh = meshId;
                gltf.AddNode(nodes, node, 0);
            }
        }

        internal static int AddTriangleMesh(this Gltf gltf,
                                            string name,
                                            List<byte> buffer,
                                            List<BufferView> bufferViews,
                                            List<Accessor> accessors,
                                            byte[] vertices,
                                            byte[] normals,
                                            byte[] indices,
                                            byte[] colors,
                                            byte[] uvs,
                                            double[] vMin,
                                            double[] vMax,
                                            double[] nMin,
                                            double[] nMax,
                                            ushort iMin,
                                            ushort iMax,
                                            double[] uvMin,
                                            double[] uvMax,
                                            int materialId,
                                            float[] cMin,
                                            float[] cMax,
                                            int? parent_index,
                                            List<glTFLoader.Schema.Mesh> meshes)
        {
            var m = new glTFLoader.Schema.Mesh();
            m.Name = name;

            var vBuff = AddBufferView(bufferViews, 0, buffer.Count, vertices.Length, null, null);
            buffer.AddRange(vertices);

            var nBuff = AddBufferView(bufferViews, 0, buffer.Count, normals.Length, null, null);
            buffer.AddRange(normals);

            var iBuff = AddBufferView(bufferViews, 0, buffer.Count, indices.Length, null, null);
            buffer.AddRange(indices);

            while (buffer.Count % 4 != 0)
            {
                // Console.WriteLine("Padding...");
                buffer.Add(0);
            }

            var vAccess = AddAccessor(accessors, vBuff, 0, Accessor.ComponentTypeEnum.FLOAT, vertices.Length / sizeof(float) / 3, new[] { (float)vMin[0], (float)vMin[1], (float)vMin[2] }, new[] { (float)vMax[0], (float)vMax[1], (float)vMax[2] }, Accessor.TypeEnum.VEC3);
            var nAccess = AddAccessor(accessors, nBuff, 0, Accessor.ComponentTypeEnum.FLOAT, normals.Length / sizeof(float) / 3, new[] { (float)nMin[0], (float)nMin[1], (float)nMin[2] }, new[] { (float)nMax[0], (float)nMax[1], (float)nMax[2] }, Accessor.TypeEnum.VEC3);
            var iAccess = AddAccessor(accessors, iBuff, 0, Accessor.ComponentTypeEnum.UNSIGNED_SHORT, indices.Length / sizeof(ushort), new[] { (float)iMin }, new[] { (float)iMax }, Accessor.TypeEnum.SCALAR);

            var prim = new MeshPrimitive();
            prim.Indices = iAccess;
            prim.Material = materialId;
            prim.Mode = MeshPrimitive.ModeEnum.TRIANGLES;
            prim.Attributes = new Dictionary<string, int>{
                {"NORMAL",nAccess},
                {"POSITION",vAccess}
            };

            if (uvs.Length > 0)
            {
                var uvBuff = AddBufferView(bufferViews, 0, buffer.Count, uvs.Length, null, null);
                buffer.AddRange(uvs);
                var uvAccess = AddAccessor(accessors, uvBuff, 0, Accessor.ComponentTypeEnum.FLOAT, uvs.Length / sizeof(float) / 2, new[] { (float)uvMin[0], (float)uvMin[1] }, new[] { (float)uvMax[0], (float)uvMax[1] }, Accessor.TypeEnum.VEC2);
                prim.Attributes.Add("TEXCOORD_0", uvAccess);
            }

            // TODO: Add to the buffer above instead of inside this block.
            // There's a chance the padding operation will put padding before
            // the color information.
            if (colors.Length > 0)
            {
                var cBuff = AddBufferView(bufferViews, 0, buffer.Count, colors.Length, null, null);
                buffer.AddRange(colors);
                var cAccess = AddAccessor(accessors, cBuff, 0, Accessor.ComponentTypeEnum.FLOAT, colors.Length / sizeof(float) / 3, cMin, cMax, Accessor.TypeEnum.VEC3);
                prim.Attributes.Add("COLOR_0", cAccess);
            }

            m.Primitives = new[] { prim };

            // Add mesh to gltf
            meshes.Add(m);

            return meshes.Count - 1;
        }

        internal static int CreateNodeForMesh(Gltf gltf, int meshId, int parentId, List<glTFLoader.Schema.Node> nodes)
        {
            // Add mesh node to gltf
            var node = new Node();
            node.Mesh = meshId;
            var nodeId = gltf.AddNode(nodes, node, parentId);
            return nodeId;
        }

        internal static int AddLineLoop(this Gltf gltf,
                                        string name,
                                        List<byte> buffer,
                                        List<BufferView> bufferViews,
                                        List<Accessor> accessors,
                                        byte[] vertices,
                                        byte[] indices,
                                        double[] vMin,
                                        double[] vMax,
                                        ushort iMin,
                                        ushort iMax,
                                        int materialId,
                                        MeshPrimitive.ModeEnum mode,
                                        List<glTFLoader.Schema.Mesh> meshes,
                                        List<glTFLoader.Schema.Node> nodes,
                                        int parentId)
        {
            var m = new glTFLoader.Schema.Mesh();
            m.Name = name;
            var vBuff = AddBufferView(bufferViews, 0, buffer.Count, vertices.Length, null, null);
            var iBuff = AddBufferView(bufferViews, 0, buffer.Count + vertices.Length, indices.Length, null, null);

            buffer.AddRange(vertices);
            buffer.AddRange(indices);

            while (buffer.Count % 4 != 0)
            {
                // Console.WriteLine("Padding...");
                buffer.Add(0);
            }

            var vAccess = AddAccessor(accessors, vBuff, 0, Accessor.ComponentTypeEnum.FLOAT, vertices.Length / sizeof(float) / 3, new[] { (float)vMin[0], (float)vMin[1], (float)vMin[2] }, new[] { (float)vMax[0], (float)vMax[1], (float)vMax[2] }, Accessor.TypeEnum.VEC3);
            var iAccess = AddAccessor(accessors, iBuff, 0, Accessor.ComponentTypeEnum.UNSIGNED_SHORT, indices.Length / sizeof(ushort), new[] { (float)iMin }, new[] { (float)iMax }, Accessor.TypeEnum.SCALAR);

            var prim = new MeshPrimitive();
            prim.Indices = iAccess;
            prim.Material = materialId;
            prim.Mode = mode;
            prim.Attributes = new Dictionary<string, int>{
                {"POSITION",vAccess}
            };

            m.Primitives = new[] { prim };

            // Add mesh to gltf
            meshes.Add(m);

            // Add mesh node to gltf
            var node = new Node();
            node.Mesh = meshes.Count - 1;
            gltf.AddNode(nodes, node, parentId);

            return meshes.Count - 1;
        }

        internal static void ToGlb(this Solid solid, string path)
        {
            var gltf = new Gltf();
            var asset = new Asset();
            asset.Version = "2.0";
            asset.Generator = "hypar-gltf";

            gltf.Asset = asset;

            var root = new Node();

            root.Translation = new[] { 0.0f, 0.0f, 0.0f };
            root.Scale = new[] { 1.0f, 1.0f, 1.0f };

            // Set Z up by rotating -90d around the X Axis
            var q = new Quaternion(new Vector3(1, 0, 0), -Math.PI / 2);
            root.Rotation = new[]{
                (float)q.X, (float)q.Y, (float)q.Z, (float)q.W
            };

            var meshes = new List<glTFLoader.Schema.Mesh>();
            var nodes = new List<glTFLoader.Schema.Node> { root };

            gltf.Scene = 0;
            var scene = new Scene();
            scene.Nodes = new[] { 0 };
            gltf.Scenes = new[] { scene };

            gltf.ExtensionsUsed = new[] { "KHR_materials_pbrSpecularGlossiness", "KHR_materials_unlit" };

            var buffer = new List<byte>();
            var bufferViews = new List<BufferView>();

            var materials = gltf.AddMaterials(new[] { BuiltInMaterials.Default, BuiltInMaterials.Edges, BuiltInMaterials.EdgesHighlighted },
                                              buffer,
                                              bufferViews);

            var mesh = new Elements.Geometry.Mesh();
            solid.Tessellate(ref mesh);
            mesh.ComputeNormals();

            byte[] vertexBuffer;
            byte[] normalBuffer;
            byte[] indexBuffer;
            byte[] colorBuffer;
            byte[] uvBuffer;

            double[] vmin; double[] vmax;
            double[] nmin; double[] nmax;
            float[] cmin; float[] cmax;
            ushort imin; ushort imax;
            double[] uvmin; double[] uvmax;

            mesh.GetBuffers(out vertexBuffer, out indexBuffer, out normalBuffer, out colorBuffer, out uvBuffer,
                            out vmax, out vmin, out nmin, out nmax, out cmin,
                            out cmax, out imin, out imax, out uvmax, out uvmin);


            var accessors = new List<Accessor>();

            var parentId = CreateNodeForTransform(gltf, new Transform(), nodes);
            var meshId = gltf.AddTriangleMesh("mesh", buffer, bufferViews, accessors, vertexBuffer, normalBuffer,
                                        indexBuffer, colorBuffer, uvBuffer, vmin, vmax, nmin, nmax,
                                        imin, imax, uvmin, uvmax, materials[BuiltInMaterials.Default.Name], cmin, cmax, null, meshes);


            CreateNodeForMesh(gltf, meshId, parentId, nodes);

            var edgeCount = 0;
            var vertices = new List<Vector3>();
            var verticesHighlighted = new List<Vector3>();
            foreach (var e in solid.Edges.Values)
            {
                if (e.Left.Loop == null || e.Right.Loop == null)
                {
                    verticesHighlighted.AddRange(new[] { e.Left.Vertex.Point, e.Right.Vertex.Point });
                }
                else
                {
                    vertices.AddRange(new[] { e.Left.Vertex.Point, e.Right.Vertex.Point });
                }

                edgeCount++;
            }

            if (vertices.Count > 0)
            {
                // Draw standard edges
                AddLines(100000, vertices.ToArray(vertices.Count), gltf, materials[BuiltInMaterials.Edges.Name], buffer, bufferViews, accessors, meshes, nodes, false, parentId);
            }

            if (verticesHighlighted.Count > 0)
            {
                // Draw highlighted edges
                AddLines(100001, verticesHighlighted.ToArray(verticesHighlighted.Count), gltf, materials[BuiltInMaterials.EdgesHighlighted.Name], buffer, bufferViews, accessors, meshes, nodes, false, parentId);
            }

            var buff = new glTFLoader.Schema.Buffer();
            buff.ByteLength = buffer.Count;
            gltf.Buffers = new[] { buff };

            gltf.BufferViews = bufferViews.ToArray(bufferViews.Count);
            gltf.Accessors = accessors.ToArray(accessors.Count);
            gltf.Nodes = nodes.ToArray(nodes.Count);
            if (meshes.Count > 0)
            {
                gltf.Meshes = meshes.ToArray(meshes.Count);
            }

            gltf.SaveBinaryModel(buffer.ToArray(buffer.Count), path);
        }

        /// <returns>Whether a Glb was successfully saved. False indicates that there was no geometry to save.</returns>
        private static bool SaveGlb(Model model, string path, bool drawEdges = false)
        {
            var gltf = InitializeGlTF(model, out var buffers, drawEdges);
            if (gltf == null)
            {
                return false;
            }

            //TODO handle initializing multiple gltf buffers at once.
            var mergedBuffer = gltf.CombineBufferAndFixRefs(buffers.ToArray(buffers.Count));
            gltf.SaveBinaryModel(mergedBuffer, path);
            return true;
        }

        /// <returns>Whether a Glb was successfully saved. False indicates that there was no geometry to save.</returns>
        private static bool SaveGltf(Model model, string path, bool drawEdges = false)
        {
            var gltf = InitializeGlTF(model, out List<byte[]> buffers, drawEdges);
            if (gltf == null)
            {
                return false;
            }

            // Buffers must be saved first, URIs may be set or modified inside this method.
            gltf.SaveBuffersAndAddUris(path, buffers);
            gltf.SaveModel(path);
            return true;
        }

        internal static Gltf InitializeGlTF(Model model, out List<byte[]> allBuffers, bool drawEdges = false)
        {
            var schemaBuffer = new glTFLoader.Schema.Buffer();
            var schemaBuffers = new List<glTFLoader.Schema.Buffer> { schemaBuffer };
            var buffers = new List<byte>();
            allBuffers = new List<byte[]> { Array.Empty<byte>() };

            var gltf = new Gltf();
            var asset = new Asset();
            asset.Version = "2.0";
            asset.Generator = "hypar-gltf";

            gltf.Asset = asset;

            var root = new Node();

            var rootTransform = new Transform(model.Transform);

            // Rotate the transform for +Z up.
            rootTransform.Rotate(new Vector3(1, 0, 0), -90.0);
            var m = rootTransform.Matrix;
            root.Matrix = new float[]{
                (float) m.m11, (float) m.m12, (float) m.m13, 0f,
                (float) m.m21, (float) m.m22, (float) m.m23, 0f,
                (float) m.m31, (float) m.m32, (float) m.m33, 0f,
                (float) m.tx, (float) m.ty, (float) m.tz, 1f};

            var nodes = new List<glTFLoader.Schema.Node> { root };
            var meshes = new List<glTFLoader.Schema.Mesh>();

            gltf.Scene = 0;
            var scene = new Scene();
            scene.Nodes = new[] { 0 };
            gltf.Scenes = new[] { scene };

            var lights = model.AllElementsOfType<DirectionalLight>().ToList();
            gltf.ExtensionsUsed = lights.Any() ? new[] {
                "KHR_materials_pbrSpecularGlossiness",
                "KHR_materials_unlit",
                "KHR_lights_punctual"
            } : new[] {
                "KHR_materials_pbrSpecularGlossiness",
                "KHR_materials_unlit"};

            var bufferViews = new List<BufferView>();

            var materialsToAdd = model.AllElementsOfType<Material>().ToList();
            if (drawEdges)
            {
                materialsToAdd.Add(BuiltInMaterials.Edges);
            }
            var materialIndexMap = gltf.AddMaterials(materialsToAdd, buffers, bufferViews);

            var elements = model.Elements.Where(e =>
            {
                return e.Value is GeometricElement || e.Value is ElementInstance;
            }).Select(e => e.Value).ToList();


            if (lights.Any())
            {
                gltf.AddLights(lights, nodes);
            }

            // Lines are stored in a list of lists
            // according to the max available index size.
            var lines = new List<List<Vector3>>();
            var currLines = new List<Vector3>();
            lines.Add(currLines);

            var accessors = new List<Accessor>();
            var textures = new List<Texture>();
            var images = new List<Image>();
            var samplers = new List<Sampler>();
            var materials = gltf.Materials != null ? gltf.Materials.ToList() : new List<glTFLoader.Schema.Material>();

            var meshElementMap = new Dictionary<Guid, List<int>>();
            var nodeElementMap = new Dictionary<Guid, int>();

            var meshTransformMap = new Dictionary<Guid, Transform>();
            foreach (var e in elements)
            {
                // Check if we'll overrun the index size
                // for the current line array. If so,
                // create a new line array.
                if (currLines.Count * 2 > ushort.MaxValue)
                {
                    currLines = new List<Vector3>();
                    lines.Add(currLines);
                }

                GetRenderDataForElement(e,
                                        gltf,
                                        materialIndexMap,
                                        buffers,
                                        allBuffers,
                                        schemaBuffers,
                                        bufferViews,
                                        accessors,
                                        materials,
                                        textures,
                                        images,
                                        samplers,
                                        meshes,
                                        nodes,
                                        meshElementMap,
                                        meshTransformMap,
                                        currLines,
                                        drawEdges,
                                        nodeElementMap);
            }
            if (allBuffers.Sum(b => b.Count()) + buffers.Count == 0 && lights.Count == 0)
            {
                return null;
            }

            gltf.AddAnimations(elements, nodeElementMap);

            if (drawEdges && lines.Count() > 0)
            {
                foreach (var lineSet in lines)
                {
                    if (lineSet.Count == 0)
                    {
                        continue;
                    }
                    var parentId = CreateNodeForTransform(gltf, new Transform(), nodes);
                    AddLines(GetNextId(), lineSet, gltf, materialIndexMap[BuiltInMaterials.Edges.Name], buffers, bufferViews, accessors, meshes, nodes, false, parentId);
                }
            }

            if (buffers.Count > 0)
            {
                schemaBuffers[0].ByteLength = buffers.Count;
            }
            gltf.Buffers = schemaBuffers.ToArray(schemaBuffers.Count);
            gltf.BufferViews = bufferViews.ToArray(bufferViews.Count);
            gltf.Accessors = accessors.ToArray(accessors.Count);
            gltf.Materials = materials.ToArray(materials.Count);
            if (textures.Count > 0)
            {
                gltf.Textures = textures.ToArray(textures.Count);
            }
            if (images.Count > 0)
            {
                gltf.Images = images.ToArray(images.Count);
            }
            if (samplers.Count > 0)
            {
                gltf.Samplers = samplers.ToArray(samplers.Count);
            }
            gltf.Nodes = nodes.ToArray(nodes.Count);
            if (meshes.Count > 0)
            {
                gltf.Meshes = meshes.ToArray(meshes.Count);
            }

            allBuffers[0] = buffers.ToArray(buffers.Count);

            return gltf;
        }

        private static void GetRenderDataForElement(Element e,
                                                    Gltf gltf,
                                                    Dictionary<string, int> materialIndexMap,
                                                    List<byte> buffers,
                                                    List<byte[]> allBuffers,
                                                    List<glTFLoader.Schema.Buffer> schemaBuffers,
                                                    List<BufferView> bufferViews,
                                                    List<Accessor> accessors,
                                                    List<glTFLoader.Schema.Material> materials,
                                                    List<Texture> textures,
                                                    List<Image> images,
                                                    List<Sampler> samplers,
                                                    List<glTFLoader.Schema.Mesh> meshes,
                                                    List<glTFLoader.Schema.Node> nodes,
                                                    Dictionary<Guid, List<int>> meshElementMap,
                                                    Dictionary<Guid, Transform> meshTransformMap,
                                                    List<Vector3> lines,
                                                    bool drawEdges,
                                                    Dictionary<Guid, int> nodeElementMap)
        {
            var materialName = BuiltInMaterials.Default.Name;
            int meshId = -1;

            if (e is ElementInstance)
            {
                var i = (ElementInstance)e;
                var transform = new Transform();
                if (i.BaseDefinition is ContentElement contentBase)
                {
                    // If there is a transform stored for the content base definition we 
                    // should apply it when creating instances.
                    if (meshTransformMap.TryGetValue(i.BaseDefinition.Id, out var baseTransform))
                    {
                        transform.Concatenate(baseTransform);
                    }
                }
                transform.Concatenate(i.Transform);
                // Lookup the corresponding mesh in the map.
                AddInstanceMesh(gltf, nodes, meshElementMap[i.BaseDefinition.Id], transform);

                if (drawEdges)
                {
                    // Get the edges for the solid
                    var geom = i.BaseDefinition;
                    if (geom.Representation != null)
                    {
                        foreach (var solidOp in geom.Representation.SolidOperations)
                        {
                            if (solidOp.Solid != null)
                            {
                                foreach (var edge in solidOp.Solid.Edges.Values)
                                {
                                    lines.AddRange(new[] { i.Transform.OfVector(edge.Left.Vertex.Point), i.Transform.OfVector(edge.Right.Vertex.Point) });
                                }
                            }
                        }
                    }
                }

                return;
            }

            var parentId = CreateNodeForTransform(gltf, ((GeometricElement)e).Transform, nodes);
            nodeElementMap.Add(e.Id, parentId);

            if (e is ModelCurve)
            {
                var mc = (ModelCurve)e;
                AddLines(GetNextId(), mc.Curve.RenderVertices(), gltf, materialIndexMap[mc.Material.Name], buffers, bufferViews, accessors, meshes, nodes, true, parentId);
                return;
            }

            if (e is ModelPoints)
            {
                var mp = (ModelPoints)e;
                if (mp.Locations.Count != 0)
                {
                    AddPoints(GetNextId(), mp.Locations, gltf, materialIndexMap[mp.Material.Name], buffers, bufferViews, accessors, meshes, nodes, parentId);
                }
                return;
            }

            if (e is ITessellate)
            {
                var geo = (ITessellate)e;
                var mesh = new Elements.Geometry.Mesh();
                geo.Tessellate(ref mesh);
                if (mesh == null)
                {
                    return;
                }

                byte[] vertexBuffer;
                byte[] normalBuffer;
                byte[] indexBuffer;
                byte[] colorBuffer;
                byte[] uvBuffer;

                double[] vmin; double[] vmax;
                double[] nmin; double[] nmax;
                float[] cmin; float[] cmax;
                ushort imin; ushort imax;
                double[] uvmin; double[] uvmax;

                mesh.GetBuffers(out vertexBuffer,
                                out indexBuffer,
                                out normalBuffer,
                                out colorBuffer,
                                out uvBuffer,
                                out vmax,
                                out vmin,
                                out nmin,
                                out nmax,
                                out cmin,
                                out cmax,
                                out imin,
                                out imax,
                                out uvmax,
                                out uvmin);

                // TODO(Ian): Remove this cast to GeometricElement when we
                // consolidate mesh under geometric representations.
                meshId = gltf.AddTriangleMesh(e.Id + "_mesh",
                                     buffers,
                                     bufferViews,
                                     accessors,
                                     vertexBuffer,
                                     normalBuffer,
                                     indexBuffer,
                                     colorBuffer,
                                     uvBuffer,
                                     vmin,
                                     vmax,
                                     nmin,
                                     nmax,
                                     imin,
                                     imax,
                                     uvmax,
                                     uvmin,
                                     materialIndexMap[materialName],
                                     cmin,
                                     cmax,
                                     null,
                                     meshes);

                if (!meshElementMap.ContainsKey(e.Id))
                {
                    meshElementMap.Add(e.Id, new List<int>());
                }
                meshElementMap[e.Id].Add(meshId);

                var geom = (GeometricElement)e;
                if (!geom.IsElementDefinition)
                {
                    CreateNodeForMesh(gltf, meshId, parentId, nodes);
                }
                return;
            }

            if (e is GeometricElement)
            {
                if (typeof(ContentElement).IsAssignableFrom(e.GetType()))
                {
                    var content = e as ContentElement;
                    if (File.Exists(content.GltfLocation))
                    {
                        var meshIndices = GltfMergingUtils.AddAllMeshesFromFromGlb(content.GltfLocation,
                                                                schemaBuffers,
                                                                allBuffers,
                                                                bufferViews,
                                                                accessors,
                                                                meshes,
                                                                materials,
                                                                textures,
                                                                images,
                                                                samplers
                                                                );


                        if (!meshElementMap.ContainsKey(e.Id))
                        {
                            meshElementMap.Add(e.Id, meshIndices);
                        }
                        if (!content.IsElementDefinition)
                        {
                            // This element is not used for instancing.
                            // apply scale transform here to bring the content glb into meters
                            var transform = content.Transform.Scaled(content.GltfScaleToMeters);
                            CreateNodeForMesh(gltf, meshId, parentId, nodes);
                        }
                        else
                        {
                            // This element will be used for instancing.  Save the transform of the
                            // content element base that will be needed when instances are placed.
                            // The scaled transform is only necessary because we are using the glb.
                            if (!meshTransformMap.ContainsKey(e.Id))
                            {
                                meshTransformMap[e.Id] = content.Transform.Scaled(content.GltfScaleToMeters);
                            }
                        }
                    }
                    else
                    {
                        ProcessGeometricRepresentation(e,
                                                       ref gltf,
                                                       ref materialIndexMap,
                                                       ref buffers,
                                                       bufferViews,
                                                       accessors,
                                                       meshes,
                                                       nodes,
                                                       meshElementMap,
                                                       lines,
                                                       drawEdges,
                                                       materialName,
                                                       ref meshId,
                                                       content,
                                                       parentId);
                    }
                }
                else
                {
                    var geometricElement = (GeometricElement)e;
                    materialName = geometricElement.Material.Name;

                    ProcessGeometricRepresentation(e,
                                                   ref gltf,
                                                   ref materialIndexMap,
                                                   ref buffers,
                                                   bufferViews,
                                                   accessors,
                                                   meshes,
                                                   nodes,
                                                   meshElementMap,
                                                   lines,
                                                   drawEdges,
                                                   materialName,
                                                   ref meshId,
                                                   geometricElement,
                                                   parentId);
                }
            }
        }

        internal static int CreateNodeForTransform(Gltf gltf, Transform transform, List<Node> nodes)
        {
            var a = transform.XAxis;
            var b = transform.YAxis;
            var c = transform.ZAxis;

            var transNode = new Node();

            transNode.Matrix = new[]{
                (float)a.X, (float)a.Y, (float)a.Z, 0.0f,
                (float)b.X, (float)b.Y, (float)b.Z, 0.0f,
                (float)c.X, (float)c.Y, (float)c.Z, 0.0f,
                (float)transform.Origin.X,(float)transform.Origin.Y,(float)transform.Origin.Z, 1.0f
            };

            return gltf.AddNode(nodes, transNode, 0);
        }

        private static void ProcessGeometricRepresentation(Element e,
                                                           ref Gltf gltf,
                                                           ref Dictionary<string, int> materialIndexMap,
                                                           ref List<byte> buffers,
                                                           List<BufferView> bufferViews,
                                                           List<Accessor> accessors,
                                                           List<glTFLoader.Schema.Mesh> meshes,
                                                           List<Node> nodes,
                                                           Dictionary<Guid, List<int>> meshElementMap,
                                                           List<Vector3> lines,
                                                           bool drawEdges,
                                                           string materialName,
                                                           ref int meshId,
                                                           GeometricElement geometricElement,
                                                           int parentId)
        {
            geometricElement.UpdateRepresentations();

            // TODO: Remove this when we get rid of UpdateRepresentation.
            // The only reason we don't fully exclude openings from processing 
            // is to ensure that openings have some geometry that will be used 
            // to compute csgs for their hosts.
            if (e.GetType() == typeof(Opening))
            {
                return;
            }

            if (geometricElement.Representation != null)
            {
                meshId = ProcessSolidsAsCSG(geometricElement,
                                    e.Id.ToString(),
                                    materialName,
                                    ref gltf,
                                    ref materialIndexMap,
                                    ref buffers,
                                    bufferViews,
                                    accessors,
                                    meshes,
                                    lines,
                                    geometricElement.Transform);

                // If the id == -1, the mesh is malformed.
                // It may have no geometry.
                if (meshId == -1)
                {
                    return;
                }

                if (!meshElementMap.ContainsKey(e.Id))
                {
                    meshElementMap.Add(e.Id, new List<int>());
                }
                meshElementMap[e.Id].Add(meshId);

                if (!geometricElement.IsElementDefinition)
                {
                    CreateNodeForMesh(gltf, meshId, parentId, nodes);
                }
            }
        }

        private static int ProcessSolidsAsCSG(GeometricElement geometricElement,
                                      string id,
                                      string materialName,
                                      ref Gltf gltf,
                                      ref Dictionary<string, int> materials,
                                      ref List<byte> buffer,
                                      List<BufferView> bufferViews,
                                      List<Accessor> accessors,
                                      List<glTFLoader.Schema.Mesh> meshes,
                                      List<Vector3> lines,
                                      Transform t = null)
        {
            // To properly compute csgs, all solid operation csgs need
            // to be transformed into their final position. Then the csgs
            // can be computed and the final csg can have the inverse of the
            // geometric element's transform applied to "reset" it. 
            // The transforms applied to each node in the glTF will then 
            // ensure that the elements are correctly transformed.
            Csg.Solid csg = new Csg.Solid();

            var solids = geometricElement.Representation.SolidOperations.Where(op => op.IsVoid == false)
                                                                        .Select(op => op.LocalTransform != null ?
                                                                            op._csg.Transform(geometricElement.Transform.Concatenated(op.LocalTransform).ToMatrix4x4()) :
                                                                            op._csg.Transform(geometricElement.Transform.ToMatrix4x4()))
                                                                        .ToArray();
            var voids = geometricElement.Representation.SolidOperations.Where(op => op.IsVoid == true)
                                                                       .Select(op => op.LocalTransform != null ?
                                                                            op._csg.Transform(geometricElement.Transform.Concatenated(op.LocalTransform).ToMatrix4x4()) :
                                                                            op._csg.Transform(geometricElement.Transform.ToMatrix4x4()))
                                                                       .ToArray();

            if (geometricElement is IHasOpenings)
            {
                var openingContainer = (IHasOpenings)geometricElement;
                voids = voids.Concat(openingContainer.Openings.SelectMany(o => o.Representation.SolidOperations
                                                      .Where(op => op.IsVoid == true)
                                                      .Select(op => op._csg.Transform(o.Transform.ToMatrix4x4())))).ToArray();
            }

            csg = csg.Union(solids);
            csg = csg.Substract(voids);
            var inverse = new Transform(geometricElement.Transform);
            inverse.Invert();

            csg = csg.Transform(inverse.ToMatrix4x4());

            byte[] vertexBuffer;
            byte[] normalBuffer;
            byte[] indexBuffer;
            byte[] colorBuffer;
            byte[] uvBuffer;

            double[] vmin; double[] vmax;
            double[] nmin; double[] nmax;
            float[] cmin; float[] cmax;
            ushort imin; ushort imax;
            double[] uvmin; double[] uvmax;

            csg.Tessellate(out vertexBuffer, out indexBuffer, out normalBuffer, out colorBuffer, out uvBuffer,
                            out vmax, out vmin, out nmin, out nmax, out cmin,
                            out cmax, out imin, out imax, out uvmax, out uvmin);

            if (vertexBuffer.Length == 0)
            {
                return -1;
            }

            return gltf.AddTriangleMesh(id + "_mesh", buffer, bufferViews, accessors, vertexBuffer, normalBuffer,
                                indexBuffer, colorBuffer, uvBuffer, vmin, vmax, nmin, nmax,
                                imin, imax, uvmin, uvmax, materials[materialName], cmin, cmax, null, meshes);
        }

        private static int ProcessSolid(Solid solid,
                                         string id,
                                         string materialName,
                                         ref Gltf gltf,
                                         ref Dictionary<string, int> materials,
                                         ref List<byte> buffer,
                                         List<BufferView> bufferViews,
                                         List<Accessor> accessors,
                                         List<glTFLoader.Schema.Mesh> meshes,
                                         List<Vector3> lines,
                                         bool drawEdges,
                                         Transform t = null)
        {
            byte[] vertexBuffer;
            byte[] normalBuffer;
            byte[] indexBuffer;
            byte[] colorBuffer;
            byte[] uvBuffer;

            double[] vmin; double[] vmax;
            double[] nmin; double[] nmax;
            float[] cmin; float[] cmax;
            ushort imin; ushort imax;
            double[] uvmin; double[] uvmax;

            solid.Tessellate(out vertexBuffer, out indexBuffer, out normalBuffer, out colorBuffer, out uvBuffer,
                            out vmax, out vmin, out nmin, out nmax, out cmin,
                            out cmax, out imin, out imax, out uvmax, out uvmin);

            if (drawEdges)
            {
                foreach (var edge in solid.Edges.Values)
                {
                    if (t != null)
                    {
                        lines.AddRange(new[] { t.OfVector(edge.Left.Vertex.Point), t.OfVector(edge.Right.Vertex.Point) });
                    }
                    else
                    {
                        lines.AddRange(new[] { edge.Left.Vertex.Point, edge.Right.Vertex.Point });
                    }
                }
            }

            return gltf.AddTriangleMesh(id + "_mesh", buffer, bufferViews, accessors, vertexBuffer, normalBuffer,
                                indexBuffer, colorBuffer, uvBuffer, vmin, vmax, nmin, nmax,
                                imin, imax, uvmin, uvmax, materials[materialName], cmin, cmax, null, meshes);
        }

        private static void AddLines(long id,
                                     IList<Vector3> vertices,
                                     Gltf gltf,
                                     int material,
                                     List<byte> buffer,
                                     List<BufferView> bufferViews,
                                     List<Accessor> accessors,
                                     List<glTFLoader.Schema.Mesh> meshes,
                                     List<glTFLoader.Schema.Node> nodes,
                                     bool lineLoop,
                                     int parentId)
        {
            var floatSize = sizeof(float);
            var ushortSize = sizeof(ushort);
            var vBuff = new byte[vertices.Count * 3 * floatSize];
            var indices = new byte[vertices.Count * 2 * ushortSize];

            var vi = 0;
            var ii = 0;
            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.X), 0, vBuff, vi, floatSize);
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.Y), 0, vBuff, vi + floatSize, floatSize);
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.Z), 0, vBuff, vi + 2 * floatSize, floatSize);
                vi += 3 * floatSize;

                var write = lineLoop ? (i < vertices.Count - 1) : (i % 2 == 0 && i < vertices.Count - 1);
                if (write)
                {
                    System.Buffer.BlockCopy(BitConverter.GetBytes((ushort)i), 0, indices, ii, ushortSize);
                    System.Buffer.BlockCopy(BitConverter.GetBytes((ushort)(i + 1)), 0, indices, ii + ushortSize, ushortSize);
                    ii += 2 * ushortSize;
                }
            }

            var bbox = new BBox3(vertices);
            gltf.AddLineLoop($"{id}_curve", buffer, bufferViews, accessors, vBuff, indices, bbox.Min.ToArray(),
                            bbox.Max.ToArray(), 0, (ushort)(vertices.Count - 1), material, MeshPrimitive.ModeEnum.LINES, meshes, nodes, parentId);
        }

        private static void AddPoints(long id,
                                      IList<Vector3> vertices,
                                      Gltf gltf,
                                      int material,
                                      List<byte> buffer,
                                      List<BufferView> bufferViews,
                                      List<Accessor> accessors,
                                      List<glTFLoader.Schema.Mesh> meshes,
                                      List<glTFLoader.Schema.Node> nodes,
                                      int parentId)
        {
            var floatSize = sizeof(float);
            var ushortSize = sizeof(ushort);
            var vBuff = new byte[vertices.Count * 3 * floatSize];
            var indices = new byte[vertices.Count * ushortSize];

            var vi = 0;
            var ii = 0;
            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.X), 0, vBuff, vi, floatSize);
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.Y), 0, vBuff, vi + floatSize, floatSize);
                System.Buffer.BlockCopy(BitConverter.GetBytes((float)v.Z), 0, vBuff, vi + 2 * floatSize, floatSize);
                vi += 3 * floatSize;

                System.Buffer.BlockCopy(BitConverter.GetBytes((ushort)i), 0, indices, ii, ushortSize);
                ii += ushortSize;
            }

            var bbox = new BBox3(vertices);
            gltf.AddLineLoop($"{id}_curve", buffer, bufferViews, accessors, vBuff, indices, bbox.Min.ToArray(),
                            bbox.Max.ToArray(), 0, (ushort)(vertices.Count - 1), material, MeshPrimitive.ModeEnum.POINTS, meshes, nodes, parentId);
        }

        // private static void AddArrow(long id,
        //                              Vector3 origin,
        //                              Vector3 direction,
        //                              Gltf gltf,
        //                              int material,
        //                              Transform t,
        //                              List<byte> buffer,
        //                              List<BufferView> bufferViews,
        //                              List<Accessor> accessors)
        // {
        //     var scale = 0.5;
        //     var end = origin + direction * scale;
        //     var up = direction.IsParallelTo(Vector3.ZAxis) ? Vector3.YAxis : Vector3.ZAxis;
        //     var tr = new Transform(Vector3.Origin, direction.Cross(up), direction);
        //     tr.Rotate(up, -45.0);
        //     var arrow1 = tr.OfPoint(Vector3.XAxis * 0.1);
        //     var pts = new[] { origin, end, end + arrow1 };
        //     var vBuff = pts.ToArray();
        //     var vCount = 3;
        //     var indices = Enumerable.Range(0, vCount).Select(i => (ushort)i).ToArray();
        //     var bbox = new BBox3(pts);
        //     gltf.AddLineLoop($"{id}_curve", buffer, bufferViews, accessors, vBuff, indices, bbox.Min.ToArray(),
        //                     bbox.Max.ToArray(), 0, (ushort)(vCount - 1), material, MeshPrimitive.ModeEnum.LINE_STRIP, t);

        // }
    }
}
