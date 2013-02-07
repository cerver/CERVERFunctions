using System;
using System.Collections.Generic;
using System.Text;
using Bentley.GenerativeComponents;

namespace SampleAddIn
{
    public sealed class Initializer: IAssemblyInitializer
    {
        // Whenever GC loads an assembly, it examines all of the classes therein, and looks for
        // those that (1) are public, (2) implement the IAssemblyInitializer interface, and (3)
        // have a public constructor that takes no arguments. For each such class, GC instantiates
        // it, automatically.

        public Initializer()
        {
            ScriptFunctions.Load();
        }
    }
}
