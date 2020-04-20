using System;
using System.Collections.Generic;
using System.Text;

namespace Elements.Geometry.Solids
{
    internal class DistanceFromOriginComparer : IComparer<(Edge Edge, Vector3 Location, Vertex Vertex)>
    {
        private Vector3 _origin;
        public DistanceFromOriginComparer(Vector3 origin)
        {
            this._origin = origin;
        }

        public int Compare((Edge Edge, Vector3 Location, Vertex Vertex) x, (Edge Edge, Vector3 Location, Vertex Vertex) y)
        {
            var a = x.Location.DistanceTo(_origin);
            var b = y.Location.DistanceTo(_origin);

            if (a < b)
            {
                return -1;
            }
            else if (a > b)
            {
                return 1;
            }
            return 0;
        }
    }

    /// <summary>
    /// A Solid Face.
    /// </summary>
    public class Face
    {
        /// <summary>
        /// The Id of the Face.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// A CCW wound list of Edges.
        /// </summary>
        public Loop Outer { get; internal set; }

        /// <summary>
        /// A collection of CW wound Edges.
        /// </summary>
        public Loop[] Inner { get; internal set; }

        /// <summary>
        /// Construct a Face.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="outer">The outer loop of the Face.</param>
        /// <param name="inner">The inner loops of the Face.</param>
        internal Face(long id, Loop outer, Loop[] inner)
        {
            this.Id = id;
            this.Outer = outer;
            outer.Face = this;
            this.Inner = inner;
            if (this.Inner != null)
            {
                foreach (var loop in inner)
                {
                    loop.Face = this;
                }
            }
        }

        /// <summary>
        /// The string representation of the Face.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Outer:");
            foreach (var he in this.Outer.Edges)
            {
                sb.AppendLine($"\t{he}");
            }

            if (this.Inner != null)
            {
                sb.AppendLine("Inner:");
                foreach (var loop in this.Inner)
                {
                    foreach (var he in loop.Edges)
                    {
                        sb.AppendLine($"\t{he}");
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private void SutherlandHodgman(List<Vector3> input, ref List<Vector3> inside, ref List<Vector3> outside, Plane p)
        {
            // Implement Sutherland-Hodgman clipping
            // https://www.cs.drexel.edu/~david/Classes/CS430/Lectures/L-05_Polygons.6.pdf
            outside = new List<Vector3>();
            inside = new List<Vector3>();

            for (var j = 0; j < input.Count; j++)
            {
                // edge of the triangle
                var start = input[j];
                var end = input[j == input.Count - 1 ? 0 : j + 1];
                var d1 = p.Normal.Dot(start - p.Origin);
                var d2 = p.Normal.Dot(end - p.Origin);

                if (d1 < 0 && d2 < 0)
                {
                    //both inside
                    inside.Add(start);
                }
                else if (d1 < 0 && d2 > 0)
                {
                    //start inside
                    //end outside
                    //add intersection
                    if (new Line(start, end).Intersects(p, out Vector3 result))
                    {
                        inside.Add(start);
                        inside.Add(result);
                        outside.Add(result);
                    }

                }
                else if (d1 > 0 && d2 > 0)
                {
                    //both outside
                    outside.Add(start);
                }
                else if (d1 > 0 && d2 < 0)
                {
                    //start outside
                    //end inside
                    //add intersection
                    if (new Line(start, end).Intersects(p, out Vector3 result))
                    {
                        inside.Add(result);
                        outside.Add(start);
                        outside.Add(result);
                    }
                }
            }
        }
    }
}