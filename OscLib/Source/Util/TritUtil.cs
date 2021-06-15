
namespace OscLib
{
    /// <summary>
    /// A basic implementation of a yes-no-maybe boolean-like ternary logic thingamajig. 
    /// </summary>
    internal enum Trit
    {
        True = 1,
        False = -1,
        Maybe = 0
    }

    /// <summary>
    /// Utility class for methods dealing with trits (ternary bits).
    /// </summary>
    internal static class TritUtil
    {
        /// <summary>
        /// Converts a bool into a trit.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static Trit ToTrit(this bool input)
        {
            if (input)
            {
                return Trit.True;
            }
            else
            {
                return Trit.False;
            }

        }

    }

}
