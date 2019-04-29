using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    /**
    * An interface to calculate a distance measure between two objects. A distance
    * function maps pairs of points into the nonnegative reals and has to satisfy
    * <ul>
    * <li> non-negativity: d(x, y) ≥ 0
    * <li> isolation: d(x, y) = 0 if and only if x = y
    * <li> symmetry: d(x, y) = d(x, y)
    * </ul>.
    * Note that a distance function is not required to satisfy triangular inequality
    * |x - y| + |y - z| ≥ |x - z|, which is necessary for a metric.
    *
    */
    public interface Distance
    {
        /**
         * Returns the distance measure between two objects.
         */
        double D(VSM x, VSM y);
    }
}
