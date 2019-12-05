//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.0.27.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Properties;
using Elements.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>A container of elements.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Model 
    {
        [Newtonsoft.Json.JsonConstructor]
        public Model(Position @origin, System.Collections.Generic.IDictionary<Guid, Element> @elements)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Model>();
            if(validator != null)
            {
                validator.Validate(new object[]{ @origin, @elements});
            }
        
            this.Origin = @origin;
            this.Elements = @elements;
        }
    
        /// <summary>The origin of the model.</summary>
        [Newtonsoft.Json.JsonProperty("Origin", Required = Newtonsoft.Json.Required.AllowNull)]
        public Position Origin { get; set; }
    
        /// <summary>A collection of Elements keyed by their identifiers.</summary>
        [Newtonsoft.Json.JsonProperty("Elements", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public System.Collections.Generic.IDictionary<Guid, Element> Elements { get; set; } = new System.Collections.Generic.Dictionary<Guid, Element>();
    
    
    }
}