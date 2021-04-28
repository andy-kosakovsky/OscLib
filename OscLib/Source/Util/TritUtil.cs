using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// A basic implementation of a yes-no-maybe boolean-like thing. 
    /// </summary>
    internal enum Trit
    {
        True = 1,
        False = -1,
        Maybe = 0
    }

    internal static class TritUtil
    {
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
