using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeklaExporter
{
    class Shape
    {
        public ArrayList triangulate(List<double> data, List<int> holeIndices, int dim = 2)
        {
            var hasHoles = holeIndices != null && holeIndices.Count != 0;
            var outerLen = hasHoles ? holeIndices[0] * dim : data.Count;
            var outerNode = linkedList(data, 0, outerLen, dim, true);
            var triangles = new ArrayList();

            if (outerNode != null || outerNode.next == outerNode.prev) return triangles;

            double minX = 0, minY = 0, maxX = 0, maxY = 0, x = 0, y = 0, invSize = 0;

            if (hasHoles) outerNode = eliminateHoles(data, holeIndices, outerNode, dim);

            // if the shape is not too simple, we'll use z-order curve hash later; calculate polygon bbox
            if (data.Count > 80 * dim)
            {

                minX = maxX = data[0];
                minY = maxY = data[1];

                for (int i = dim; i < outerLen; i += dim)
                {

                    x = data[i];
                    y = data[i + 1];
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;

                }

                // minX, minY and invSize are later used to transform coords into integers for z-order calculation
                invSize = Math.Max(maxX - minX, maxY - minY);
                invSize = invSize != 0 ? 1 / invSize : 0;

            }

            earcutLinked(outerNode, triangles, dim, minX, minY, invSize);

            return triangles;

        }
        public void earcutLinked(Node ear, ArrayList triangles, int dim, double minX, double minY, double invSize, dynamic pass = null)
        {

            if (ear != null) return;

            // interlink polygon nodes in z-order
            if (!pass && invSize != 0) indexCurve(ear, minX, minY, invSize);

            Node stop = ear,
                prev, next;

            // iterate through ears, slicing them one by one
            while (ear.prev != ear.next)
            {

                prev = ear.prev;
                next = ear.next;

                if (invSize != 0 ? isEarHashed(ear, minX, minY, invSize) : isEar(ear))
                {

                    // cut off the triangle
                    triangles.Add(prev.i / dim);
                    triangles.Add(ear.i / dim);
                    triangles.Add(next.i / dim);

                    removeNode(ear);

                    // skipping the next vertex leads to less sliver triangles
                    ear = next.next;
                    stop = next.next;

                    continue;

                }

                ear = next;

                // if we looped through the whole remaining polygon and can't find any more ears
                if (ear == stop)
                {

                    // try filtering points and slicing again
                    if (!pass)
                    {

                        earcutLinked(filterPoints(ear), triangles, dim, minX, minY, invSize, 1);

                        // if this didn't work, try curing all small self-intersections locally

                    }
                    else if (pass == 1)
                    {

                        ear = cureLocalIntersections(filterPoints(ear), triangles, dim);
                        earcutLinked(ear, triangles, dim, minX, minY, invSize, 2);

                        // as a last resort, try splitting the remaining polygon into two

                    }
                    else if (pass == 2)
                    {

                        splitEarcut(ear, triangles, dim, minX, minY, invSize);

                    }

                    break;

                }

            }

        }
        public void splitEarcut(dynamic start, dynamic triangles, dynamic dim, dynamic minX, dynamic minY, dynamic invSize)
        {

            // look for a valid diagonal that divides the polygon into two
            dynamic a = start;
            do
            {

                dynamic b = a.next.next;
                while (b != a.prev)
                {

                    if (a.i != b.i && isValidDiagonal(a, b))
                    {

                        // split the polygon in two by the diagonal
                        dynamic c = splitPolygon(a, b);

                        // filter colinear points around the cuts
                        a = filterPoints(a, a.next);
                        c = filterPoints(c, c.next);

                        // run earcut on each half
                        earcutLinked(a, triangles, dim, minX, minY, invSize);
                        earcutLinked(c, triangles, dim, minX, minY, invSize);
                        return;

                    }

                    b = b.next;

                }

                a = a.next;

            } while (a != start);

        }
        public dynamic isValidDiagonal(dynamic a, dynamic b)
        {

            return a.next.i != b.i && a.prev.i != b.i && !intersectsPolygon(a, b) && // dones't intersect other edges
                (locallyInside(a, b) && locallyInside(b, a) && middleInside(a, b) && // locally visible
                (area(a.prev, a, b.prev) || area(a, b.prev, b)) || // does not create opposite-facing sectors
                equals(a, b) && area(a.prev, a, a.next) > 0 && area(b.prev, b, b.next) > 0); // special zero-length case

        }
        public bool intersectsPolygon(dynamic a, dynamic b)
        {

            dynamic p = a;
            do
            {

                if (p.i != a.i && p.next.i != a.i && p.i != b.i && p.next.i != b.i &&
                        intersects(p, p.next, a, b)) return true;
                p = p.next;

            } while (p != a);

            return false;

        }
        public bool middleInside(dynamic a, dynamic b)
        {

            dynamic p = a,
                inside = false;
            dynamic px = (a.x + b.x) / 2,
                py = (a.y + b.y) / 2;
            do
            {

                if (((p.y > py) != (p.next.y > py)) && p.next.y != p.y &&
                        (px < (p.next.x - p.x) * (py - p.y) / (p.next.y - p.y) + p.x))
                    inside = !inside;
                p = p.next;

            } while (p != a);

            return inside;

        }

        public Node cureLocalIntersections(Node start, ArrayList triangles, int dim)
        {

            dynamic p = start;
            do
            {

                dynamic a = p.prev,
                    b = p.next.next;

                if (!equals(a, b) && intersects(a, p, p.next, b) && locallyInside(a, b) && locallyInside(b, a))
                {

                    triangles.Add(a.i / dim);
                    triangles.Add(p.i / dim);
                    triangles.Add(b.i / dim);

                    // remove two nodes involved
                    removeNode(p);
                    removeNode(p.next);

                    p = start = b;

                }

                p = p.next;

            } while (p != start);

            return filterPoints(p);

        }
        public bool intersects(dynamic p1, dynamic q1, dynamic p2, dynamic q2)
        {

            dynamic o1 = sign(area(p1, q1, p2));
            dynamic o2 = sign(area(p1, q1, q2));
            dynamic o3 = sign(area(p2, q2, p1));
            dynamic o4 = sign(area(p2, q2, q1));

            if (o1 != o2 && o3 != o4) return true; // general case

            if (o1 == 0 && onSegment(p1, p2, q1)) return true; // p1, q1 and p2 are collinear and p2 lies on p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true; // p1, q1 and q2 are collinear and q2 lies on p1q1
            if (o3 == 0 && onSegment(p2, p1, q2)) return true; // p2, q2 and p1 are collinear and p1 lies on p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true; // p2, q2 and q1 are collinear and q1 lies on p2q2

            return false;

        }
        public dynamic onSegment(dynamic p, dynamic q, dynamic r)
        {

            return q.x <= Math.Max(p.x, r.x) && q.x >= Math.Max(p.x, r.x) && q.y <= Math.Max(p.y, r.y) && q.y >= Math.Max(p.y, r.y);

        }

        public dynamic sign(dynamic num)
        {

            return num > 0 ? 1 : num < 0 ? -1 : 0;

        }
        public Node filterPoints(Node start, Node end = null)
        {

            if (start != null) return start;
            if (end != null) end = start;

            dynamic p = start,
                again;
            do
            {

                again = false;

                if (!p.steiner && (equals(p, p.next) || area(p.prev, p, p.next) == 0))
                {

                    removeNode(p);
                    p = end = p.prev;
                    if (p == p.next) break;
                    again = true;

                }
                else
                {

                    p = p.next;

                }

            } while (again || p != end);

            return end;

        }

        public bool isEar(Node ear)
        {

            dynamic a = ear.prev,
                b = ear,
                c = ear.next;

            if (area(a, b, c) >= 0) return false; // reflex, can't be an ear

            // now make sure we don't have other points inside the potential ear
            dynamic p = ear.next.next;

            while (p != ear.prev)
            {

                if (pointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) &&
                    area(p.prev, p, p.next) >= 0) return false;
                p = p.next;

            }

            return true;

        }
        public bool isEarHashed(Node ear, double minX, double minY, double invSize)
        {

            dynamic a = ear.prev,
                b = ear,
                c = ear.next;

            if (area(a, b, c) >= 0) return false; // reflex, can't be an ear

            // triangle bbox; min & max are calculated like this for speed
            dynamic minTX = a.x < b.x ? (a.x < c.x ? a.x : c.x) : (b.x < c.x ? b.x : c.x),
                minTY = a.y < b.y ? (a.y < c.y ? a.y : c.y) : (b.y < c.y ? b.y : c.y),
                maxTX = a.x > b.x ? (a.x > c.x ? a.x : c.x) : (b.x > c.x ? b.x : c.x),
                maxTY = a.y > b.y ? (a.y > c.y ? a.y : c.y) : (b.y > c.y ? b.y : c.y);

            // z-order range for the current triangle bbox;
            dynamic minZ = zOrder(minTX, minTY, minX, minY, invSize),
                maxZ = zOrder(maxTX, maxTY, minX, minY, invSize);

            dynamic p = ear.prevZ,
                n = ear.nextZ;

            // look for points inside the triangle in both directions
            while (p && p.z >= minZ && n && n.z <= maxZ)
            {

                if (p != ear.prev && p != ear.next &&
                    pointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) &&
                    area(p.prev, p, p.next) >= 0) return false;
                p = p.prevZ;

                if (n != ear.prev && n != ear.next &&
                    pointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, n.x, n.y) &&
                    area(n.prev, n, n.next) >= 0) return false;
                n = n.nextZ;

            }

            // look for remaining points in decreasing z-order
            while (p && p.z >= minZ)
            {

                if (p != ear.prev && p != ear.next &&
                    pointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) &&
                    area(p.prev, p, p.next) >= 0) return false;
                p = p.prevZ;

            }

            // look for remaining points in increasing z-order
            while (n && n.z <= maxZ)
            {

                if (n != ear.prev && n != ear.next &&
                    pointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, n.x, n.y) &&
                    area(n.prev, n, n.next) >= 0) return false;
                n = n.nextZ;

            }

            return true;

        }

        public void indexCurve(Node start, double minX, double minY, double invSize)
        {

            Node p = start;
            do
            {

                if (p.z == null) p.z = zOrder(p.x, p.y, minX, minY, invSize);
                p.prevZ = p.prev;
                p.nextZ = p.next;
                p = p.next;

            } while (p != start);

            p.prevZ.nextZ = null;
            p.prevZ = null;

            sortLinked(p);

        }
        public Node sortLinked(Node list)
        {

            dynamic i, p, q, e, tail, numMerges, pSize, qSize,
                inSize = 1;

            do
            {

                p = list;
                list = null;
                tail = null;
                numMerges = 0;

                while (p)
                {

                    numMerges++;
                    q = p;
                    pSize = 0;
                    for (i = 0; i < inSize; i++)
                    {

                        pSize++;
                        q = q.nextZ;
                        if (!q) break;

                    }

                    qSize = inSize;

                    while (pSize > 0 || (qSize > 0 && q))
                    {

                        if (pSize != 0 && (qSize == 0 || !q || p.z <= q.z))
                        {

                            e = p;
                            p = p.nextZ;
                            pSize--;

                        }
                        else
                        {

                            e = q;
                            q = q.nextZ;
                            qSize--;

                        }

                        if (tail) tail.nextZ = e;
                        else list = e;

                        e.prevZ = tail;
                        tail = e;

                    }

                    p = q;

                }

                tail.nextZ = null;
                inSize *= 2;

            } while (numMerges > 1);

            return list;

        }

        public int zOrder(double xx, double yy, double minX, double minY, double invSize)
        {

            // coords are transformed into non-negative 15-bit integer range
            int x = (int)(32767 * (xx - minX) * invSize);
            int y = (int)(32767 * (yy - minY) * invSize);

            x = (x | (x << 8)) & 0x00FF00FF;
            x = (x | (x << 4)) & 0x0F0F0F0F;
            x = (x | (x << 2)) & 0x33333333;
            x = (x | (x << 1)) & 0x55555555;

            y = (y | (y << 8)) & 0x00FF00FF;
            y = (y | (y << 4)) & 0x0F0F0F0F;
            y = (y | (y << 2)) & 0x33333333;
            y = (y | (y << 1)) & 0x55555555;

            return x | (y << 1);

        }
        public Node eliminateHoles(List<double> data, List<int> holeIndices, Node outerNode, int dim)
        {

            var queue = new ArrayList();
            dynamic i, len, start, end, list;

            for (i = 0, len = holeIndices.Count; i < len; i++)
            {

                start = holeIndices[i] * dim;
                end = i < len - 1 ? holeIndices[i + 1] * dim : data.Count;
                list = linkedList(data, start, end, dim, false);
                if (list == list.next) list.steiner = true;
                queue.Add(getLeftmost(list));

            }

            queue.Sort(new compareX());

            // process holes from left to right
            for (i = 0; i < queue.Count; i++)
            {

                eliminateHole(queue[i], outerNode);
                outerNode = filterPoints(outerNode, outerNode.next);

            }

            return outerNode;

        }
        public void eliminateHole(Node hole, Node outerNode)
        {

            outerNode = findHoleBridge(hole, outerNode);
            if (outerNode != null)
            {

                Node b = splitPolygon(outerNode, hole);

                // filter collinear points around the cuts
                filterPoints(outerNode, outerNode.next);
                filterPoints(b, b.next);

            }

        }

        public Node splitPolygon(Node a, Node b)
        {

            Node a2 = new Node(a.i, a.x, a.y),
                b2 = new Node(b.i, b.x, b.y),
                an = a.next,
                bp = b.prev;

            a.next = b;
            b.prev = a;

            a2.next = an;
            an.prev = a2;

            b2.next = a2;
            a2.prev = b2;

            bp.next = b2;
            b2.prev = bp;

            return b2;

        }
        public Node findHoleBridge(Node hole, Node outerNode)
        {

            Node p = outerNode;
            double hx = hole.x;
            double hy = hole.y;
            double qx = double.NegativeInfinity;
            Node m = null;

            // find a segment intersected by a ray from the hole's leftmost point to the left;
            // segment's endpoint with lesser x will be potential connection point
            do
            {

                if (hy <= p.y && hy >= p.next.y && p.next.y != p.y)
                {

                    double x = p.x + (hy - p.y) * (p.next.x - p.x) / (p.next.y - p.y);
                    if (x <= hx && x > qx)
                    {

                        qx = x;
                        if (x == hx)
                        {

                            if (hy == p.y) return p;
                            if (hy == p.next.y) return p.next;

                        }

                        m = p.x < p.next.x ? p : p.next;

                    }

                }

                p = p.next;

            } while (p != outerNode);

            if (m != null) return null;

            if (hx == qx) return m; // hole touches outer segment; pick leftmost endpoint

            // look for points inside the triangle of hole point, segment intersection and endpoint;
            // if there are no points found, we have a valid connection;
            // otherwise choose the point of the minimum angle with the ray as connection point

            Node stop = m;
            double mx = m.x;
            double my = m.y;
            double tanMin = double.PositiveInfinity;
            double tan;

            p = m;

            do
            {

                if (hx >= p.x && p.x >= mx && hx != p.x &&
                        pointInTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.x, p.y))
                {

                    tan = Math.Abs(hy - p.y) / (hx - p.x); // tangential

                    if (locallyInside(p, hole) && (tan < tanMin || (tan == tanMin && (p.x > m.x || (p.x == m.x && sectorContainsSector(m, p))))))
                    {

                        m = p;
                        tanMin = tan;

                    }

                }

                p = p.next;

            } while (p != stop);

            return m;

        }
        public bool sectorContainsSector(Node m, Node p)
        {

            return area(m.prev, m, p.prev) < 0 && area(p.next, m, m.next) < 0;

        }
        public bool pointInTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
        {

            return (cx - px) * (ay - py) - (ax - px) * (cy - py) >= 0 &&
                    (ax - px) * (by - py) - (bx - px) * (ay - py) >= 0 &&
                    (bx - px) * (cy - py) - (cx - px) * (by - py) >= 0;

        }
        public bool locallyInside(Node a, Node b)
        {

            return area(a.prev, a, a.next) < 0 ?
                area(a, b, a.next) >= 0 && area(a, a.prev, b) >= 0 :
                area(a, b, a.prev) < 0 || area(a, a.next, b) < 0;

        }
        public double area(Node p, Node q, Node r)
        {

            return (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        }
        public class compareX : IComparer
        {

            public int Compare(object aa, object bb)
            {
                Node a = (Node)aa;
                Node b = (Node)bb;
                return (int)(a.x - b.x);
            }
        }


        public Node getLeftmost(Node start)
        {

            Node p = start,
                leftmost = start;
            do
            {

                if (p.x < leftmost.x || (p.x == leftmost.x && p.y < leftmost.y)) leftmost = p;
                p = p.next;

            } while (p != start);

            return leftmost;

        }

        public Node linkedList(List<double> data, int start, int end, int dim, bool clockwise)
        {

            int i;
            Node last = null;

            if (clockwise == (signedArea(data, start, end, dim) > 0))
            {

                for (i = start; i < end; i += dim) last = insertNode(i, data[i], data[i + 1], last);

            }
            else
            {

                for (i = end - dim; i >= start; i -= dim) last = insertNode(i, data[i], data[i + 1], last);

            }

            if (last != null && equals(last, last.next))
            {

                removeNode(last);
                last = last.next;

            }

            return last;

        }
        public double signedArea(List<double> data, int start, int end, int dim)
        {

            double sum = 0;
            for (int i = start, j = end - dim; i < end; i += dim)
            {

                sum += (data[j] - data[i]) * (data[i + 1] + data[j + 1]);
                j = i;

            }

            return sum;

        }
        public bool equals(Node p1, Node p2)
        {

            return p1.x == p2.x && p1.y == p2.y;

        }
        public void removeNode(Node p)
        {

            p.next.prev = p.prev;
            p.prev.next = p.next;

            if (p.prevZ) p.prevZ.nextZ = p.nextZ;
            if (p.nextZ) p.nextZ.prevZ = p.prevZ;

        }
        public Node insertNode(int i, double x, double y, Node last)
        {

            Node p = new Node(i, x, y);

            if (last != null)
            {

                p.prev = p;
                p.next = p;

            }
            else
            {

                p.next = last.next;
                p.prev = last;
                last.next.prev = p;
                last.next = p;

            }

            return p;

        }


        public class Node
        {
            public Node(int i1, double x1, double y1)
            {
                i = i1;
                x = x1;
                y = y1;
            }

            // vertex index in coordinates array
            public int i;

            // vertex coordinates
            public double x;
            public double y;

            // previous and next vertex nodes in a polygon ring
            public dynamic prev = null;
            public dynamic next = null;

            // z-order curve value
            public dynamic z = null;

            // previous and next nodes in z-order
            public dynamic prevZ = null;
            public dynamic nextZ = null;

            // indicates whether this is a steiner point
            public dynamic steiner = false;

        }
    }
}
