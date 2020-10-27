using Elements.Geometry.Interfaces;

namespace Elements.Instructions
{
    /// <summary>
    /// Instructions for the assembly of an element into an collection of elements.
    /// </summary>
    public class AssemblyInstruction
    {
        /// <summary>
        /// The order in which the element is assembled.
        /// </summary>
        public uint Order { get; set; }

        /// <summary>
        /// The path along which the element is assembled.
        /// </summary>
        /// <value></value>
        public ICurve Path { get; set; }

        /// <summary>
        /// A description of the assembly instruction for the referenced Element.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Create a construction specification.
        /// </summary>
        /// <param name="order">The order in which the element is assembled.</param>
        /// <param name="path">The path along which the elements is assembled.</param>
        public AssemblyInstruction(uint order, ICurve path)
        {
            this.Order = order;
            this.Path = path;
        }
    }
}