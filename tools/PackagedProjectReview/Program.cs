using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;

if (args.Length != 3) throw new ArgumentException("Pass the unpacked app bundle, a disposable output root, and a clean base ROM (read-only input).");
var binaries = Path.Combine(Path.GetFullPath(args[0]), "Contents/MacOS");
AssemblyLoadContext.Default.Resolving += (context, name) => {
   var path = Path.Combine(binaries, name.Name + ".dll");
   return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
Review.Run(binaries, Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
