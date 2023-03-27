using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class PathHelpers
    {
        public static int[] GetPrevious(int[] path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path.Length == 0) throw new ArgumentException("Cannot get previous path of root path");

            var last = path.Last();

            if (last <= 0) throw new Exception("Cannot get previous path of first child path");

            return path.SkipLast(1).Concat(new[] { last - 1 }).ToArray();
        }

        public static int Compare(int[] a, int[] b)
        {
            var minLength = Math.Min(a.Length, b.Length);

            for (var i = 0; i < minLength; i++)
            {
                if (a[i] < b[i]) return -1;
                if (a[i] > b[i]) return 1;
            }

            return 0;
        }

        public static bool Equals(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;

            for (var i = 0; i < b.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        public static bool IsAncestor(int[] path, int[] another)
        {
            return path.Length < another.Length && Compare(path, another) == 0;
        }

        public static bool IsSibling(int[] path, int[] another)
        {
            if (path.Length != another.Length) return false;

            return path.Last() != another.Last() && Equals(path.SkipLast(1).ToArray(), another.SkipLast(1).ToArray());

        }

        public static bool EndsBefore(int[] path, int[] another)
        {
            if (another.Length < path.Length) return false;

            var i = path.Length - 1;
            var pathCommon = path.Take(i);
            var anotherCommon = another.Take(i);
            var pathLastItem = path[i];
            var anotherSibling = another[i];

            return Equals(pathCommon, anotherCommon) && pathLastItem < anotherSibling;
        }

        /// <summary>
        /// Transform path by operation
        /// Modifies path argument!
        /// </summary>
        /// <param name="path"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static int[]? Transform(int[] path, NodeOperationBase operation, bool isBackwardAffinity = false)
        {
            if (path.Length == 0)
            {
                return path;
            }

            var op = operation.Path;

            if (operation is InsertNodeOperation)
            {
                if (Equals(op, path) || EndsBefore(op, path) || IsAncestor(op, path))
                {
                    path[op.Length - 1] += 1;
                }
            }
            else if (operation is RemoveNodeOperation)
            {
                if (Equals(op, path) || IsAncestor(op, path))
                {
                    return null;
                }
                else if (EndsBefore(op, path))
                {
                    path[op.Length - 1] -= 1;
                }
            }
            else if (operation is MergeNodeOperation mergeNodeOperation)
            {
                if (Equals(op, path) || EndsBefore(op, path))
                {
                    path[op.Length - 1] -= 1;
                }
                else if (IsAncestor(op, path))
                {
                    path[op.Length - 1] -= 1;
                    path[op.Length] += mergeNodeOperation.Position;
                }
            }
            else if (operation is SplitNodeOperation splitNodeOperation)
            {
                if ((Equals(op, path) && !isBackwardAffinity) || EndsBefore(op, path))
                {
                    path[op.Length - 1] += 1;
                }
                else if (IsAncestor(op, path) && path[op.Length] >= splitNodeOperation.Position)
                {
                    path[op.Length - 1] += 1;
                    path[op.Length] -= splitNodeOperation.Position;
                }
            }
            else if (operation is MoveNodeOperation moveNodeOperation)
            {
                var onp = moveNodeOperation.NewPath;
                if (Equals(op, onp))
                {
                    return path;
                }

                if (IsAncestor(op, path) || Equals(op, path))
                {
                    var copy = onp.Skip(0).ToArray();

                    if (EndsBefore(op, onp) && op.Length < onp.Length)
                    {
                        copy[op.Length - 1] -= 1;
                    }

                    return copy.Concat(path.Skip(op.Length)).ToArray();
                }
                else if (IsSibling(op, onp) && (IsAncestor(onp, path) || Equals(onp, path)))
                {
                    if (EndsBefore(op, path))
                    {
                        path[op.Length - 1] -= 1;
                    }
                    else
                    {
                        path[op.Length - 1] += 1;
                    }
                }
                else if (EndsBefore(onp, path) || Equals(onp, path) || IsAncestor(onp, path))
                {
                    if (EndsBefore(op, path))
                    {
                        path[op.Length - 1] -= 1;
                    }

                    path[onp.Length - 1] += 1;
                }
                else if (EndsBefore(op, path))
                {
                    if (Equals(onp, path))
                    {
                        path[onp.Length - 1] += 1;
                    }

                    path[op.Length - 1] -= 1;
                }
            }

            return path;
        }
    }
}
