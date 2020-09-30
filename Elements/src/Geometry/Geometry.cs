using System;
using System.Collections.Generic;

namespace Elements.Geometry
{
    /// <summary>
    /// 
    /// </summary>
    public class GeometryCreationException : Exception
    {
        /// <summary>
        /// The location where the creation exception occured.
        /// </summary>
        public Vector3 Location { get; }

        /// <summary>
        /// Create a GeometryCreationException.
        /// </summary>
        public GeometryCreationException(string message, Vector3 location) : base(message)
        {
            this.Location = location;
        }
    }

    /// <summary>
    /// A provider of GeometryCreationExceptions
    /// </summary>
    public sealed class GeometryCreationExceptionProvider
    {
        private static GeometryCreationExceptionProvider _instance = null;
        private List<GeometryCreationException> _exceptions = new List<GeometryCreationException>();

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static GeometryCreationExceptionProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GeometryCreationExceptionProvider();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Add an exception.
        /// </summary>
        /// <param name="exception">The exception to add.</param>
        public void AddException(GeometryCreationException exception)
        {
            this._exceptions.Add(exception);
        }
    }

    /// <summary>
    /// Geometry creation methods.
    /// </summary>
    public static class Create
    {
        /// <summary>
        /// Create a line.
        /// </summary>
        /// <param name="start">The start of the line.</param>
        /// <param name="end">The end of the line.</param>
        /// <param name="exception">An exception thrown when the line cannot be created.</param>
        /// <returns></returns>
        public static Line Line(Vector3 start, Vector3 end, out GeometryCreationException exception)
        {
            exception = null;
            try
            {
                if (start.IsAlmostEqualTo(end))
                {
                    throw new GeometryCreationException("The line could not be created. The start and end points are almost equal.", start);
                }
                return new Line(start, end);
            }
            catch (GeometryCreationException ex)
            {
                exception = ex;
                GeometryCreationExceptionProvider.Instance.AddException(ex);
            }
            return new Line(Vector3.Origin, new Vector3(1, 0, 0));
        }

        /// <summary>
        /// Create a circle.
        /// </summary>
        /// <param name="origin">The origin of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="exception">An exception thrown when the circle cannot be created.</param>
        /// <returns></returns>
        public static Circle Circle(Vector3 origin, double radius, out GeometryCreationException exception)
        {
            exception = null;
            try
            {
                if (radius <= 0)
                {
                    throw new GeometryCreationException("The circle could not be created. The radius must be greater than or equal to zero.", origin);
                }
            }
            catch (GeometryCreationException ex)
            {
                exception = ex;
                GeometryCreationExceptionProvider.Instance.AddException(ex);
            }
            return new Circle(Vector3.Origin, 1);
        }
    }

}