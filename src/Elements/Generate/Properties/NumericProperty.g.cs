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

namespace Elements.Properties
{
    #pragma warning disable // Disable all warnings

    /// <summary>A property with a numeric value.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class NumericProperty 
    {
        [Newtonsoft.Json.JsonConstructor]
        public NumericProperty(double @value, NumericPropertyUnitType @unitType)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<NumericProperty>();
            if(validator != null)
            {
                validator.Validate(new object[]{ @value, @unitType});
            }
        
            this.Value = @value;
            this.UnitType = @unitType;
        }
    
        /// <summary>The property's value</summary>
        [Newtonsoft.Json.JsonProperty("Value", Required = Newtonsoft.Json.Required.Always)]
        public double Value { get; set; }
    
        /// <summary>The property's unit type.</summary>
        [Newtonsoft.Json.JsonProperty("UnitType", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public NumericPropertyUnitType UnitType { get; set; }
    
    
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public enum NumericPropertyUnitType
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Length")]
        Length = 0,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Mass")]
        Mass = 1,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Force")]
        Force = 2,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Area")]
        Area = 3,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Pressure")]
        Pressure = 4,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Currency")]
        Currency = 5,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Time")]
        Time = 6,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Number")]
        Number = 7,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Volume")]
        Volume = 8,
    
    }
}