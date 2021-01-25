using UnityEngine;

namespace Constants
{
    public static class DirectionConstants
    {
        public static readonly Vector3 NORTH_VECTOR = new Vector3(0, 0, 1);
        public static readonly Vector3 SOUTH_VECTOR = new Vector3(0, 0, -1);
        public static readonly Vector3 EAST_VECTOR = new Vector3(1, 0, 0);
        public static readonly Vector3 WEST_VECTOR = new Vector3(-1, 0, 0);

        public static readonly int NORTH_BEARING = 0;
        public static readonly int SOUTH_BEARING = 180;
        public static readonly int EAST_BEARING = 90;
        public static readonly int WEST_BEARING = 270;
    }
}