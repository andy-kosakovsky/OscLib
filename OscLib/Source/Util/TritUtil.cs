
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

        /// <summary>
        /// If both trits are "maybe", returns "maybe".
        /// Will return "false" if at least one trit is "false" and the other is not "true".
        /// Will return "true" if at least one trit is "true";
        /// </summary>
        /// <returns></returns>
        internal static Trit Orish(Trit one, Trit two)
        {
            if ((one == Trit.True) || (two == Trit.True))
            {
                return Trit.True;
            }

            if ((one == Trit.False) || (two == Trit.False))
            {
                return Trit.False;
            }

            return Trit.Maybe;
        }

    }

}
