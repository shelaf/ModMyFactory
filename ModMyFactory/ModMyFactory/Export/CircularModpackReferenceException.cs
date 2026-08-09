using System;

namespace ModMyFactory.Export
{
    /// <summary>
    /// This exception is thrown if a circular reference between modpacks is detected during export.
    /// </summary>
    class CircularModpackReferenceException : Exception
    {
        public CircularModpackReferenceException()
            : base("A circular reference between modpacks was detected.")
        { }
    }
}
