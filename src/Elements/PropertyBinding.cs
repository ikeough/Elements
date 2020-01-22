using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Elements
{
    /// <summary>
    /// A binding between a source property and a target property.
    /// </summary>
    public class PropertyBinding<T> 
    {
        /// <summary>
        /// The source object.
        /// </summary>
        public Element Source { get; }

        /// <summary>
        /// The source property.
        /// </summary>
        public string SourceProperty { get; }
        
        /// <summary>
        /// The value of the binding.
        /// </summary>
        [JsonIgnore]
        public Func<T> Value { get; }

        /// <summary>
        /// Construct a property binding.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceProperty"></param>
        public PropertyBinding(Element source, string sourceProperty)
        {
            this.Source = source;
            this.SourceProperty = sourceProperty;
            
            var subProperties = sourceProperty.Split(new char[]{'.'}, StringSplitOptions.RemoveEmptyEntries);
            var currentSource = source;
            Expression expr = Expression.Constant(currentSource);
            
            // Compile an expression which gets the property
            // value for the source object.
            foreach(var subProperty in subProperties)
            {
                expr = Expression.Property(
                    expr,
                    subProperty
                );
            }
            
            this.Value = Expression.Lambda<Func<T>>(expr).Compile();
        }
    }
}