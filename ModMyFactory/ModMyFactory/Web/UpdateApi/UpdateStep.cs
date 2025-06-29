using System;

namespace ModMyFactory.Web.UpdateApi
{
    sealed class UpdateStep
    {
        public Version From { get; }

        public Version To { get; }

        public bool IsStable { get; }

        public bool IsExpansion { get; }

        public UpdateStep(Version from, Version to, bool isStable, bool isExpansion)
        {
            From = from;
            To = to;
            IsStable = isStable;
            IsExpansion = isExpansion;
        }
    }
}
