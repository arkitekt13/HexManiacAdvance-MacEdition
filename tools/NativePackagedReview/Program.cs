using System.Reflection;
using System.Runtime.Loader;

var binaries = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
AssemblyLoadContext.Default.Resolving += (context, name) => {
   var file = Path.Combine(binaries, name.Name + ".dll");
   return File.Exists(file) ? context.LoadFromAssemblyPath(file) : null;
};
Bootstrap.Run(binaries);
