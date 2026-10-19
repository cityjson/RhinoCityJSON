using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoCityJSON.SupportClasses
{
    public static class ProjSupport
    {
        private static readonly Lazy<ProjImplementation> instance_ =
            new Lazy<ProjImplementation>(
                () => new ProjImplementation()
                );

        public static ProjImplementation Instance =>  instance_.Value;
    }
}
