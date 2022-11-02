using UnityEngine;

namespace AnttiStarterKit.Utils
{
    public static class Misc
    {
        public static int PlusMinusOne()
        {
            return Random.value < 0.5f ? 1 : -1;
        }
    }
}