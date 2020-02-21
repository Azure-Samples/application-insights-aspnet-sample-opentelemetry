using System;
namespace Sample.Common
{
    public static class FailGenerator
    {
        static Random random = new Random();

        public static void FailIfNeeded(int failRate)
        {
            var v = 0;
            lock (random)
            {
                // between 0 and 99
                v = random.Next(100);
            }

            v++;

            if (v <= failRate)
            {
                throw new GeneratedFailureException($"Failed ({failRate}% chance)");
            }
        }
    }

}
