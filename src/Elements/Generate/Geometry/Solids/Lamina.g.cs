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

namespace Elements.Geometry.Solids
{
    #pragma warning disable // Disable all warnings

    /// <summary>A zero-thickness solid defined by a profile.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Lamina : SolidOperation
    {
        [Newtonsoft.Json.JsonConstructor]
        public Lamina(Polygon @perimeter, bool @isVoid)
            : base(isVoid)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Lamina>();
            if(validator != null)
            {
                validator.Validate(new object[]{ @perimeter, @isVoid});
            }
        
            this.Perimeter = @perimeter;
        }
    
        /// <summary>The perimeter.</summary>
        [Newtonsoft.Json.JsonProperty("Perimeter", Required = Newtonsoft.Json.Required.AllowNull)]
        public Polygon Perimeter { get; set; }
    
    
    }
}