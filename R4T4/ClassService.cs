using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using R4T4.Extensions;
using R4T4.Models;

namespace R4T4
{
    /// <summary>
    /// Class and Interface provider
    /// </summary>
    public class ClassService
    {
        private readonly Dictionary<string, INamedTypeSymbol> _classes = new Dictionary<string, INamedTypeSymbol>();
        private readonly Dictionary<string, List<INamedTypeSymbol>> _bases = new Dictionary<string, List<INamedTypeSymbol>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassService"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        public ClassService(CompiledProject project)
        {
          var models = project.Compilation.SyntaxTrees.Select(s => project.Compilation.GetSemanticModel(s));
          foreach (var model in models)
          {
            var classes = model.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var @class in classes)
            {
              if (!(model.GetDeclaredSymbol(@class) is INamedTypeSymbol symbol)) continue;
              var fullTypeString = GetFullTypeString(symbol);
              _classes[fullTypeString] = symbol;
              foreach (var typeSymbol in GetBaseClasses(model, @class))
              {
                var baseType = typeSymbol.GetFullTypeString();
                if (!_bases.ContainsKey(baseType))
                  _bases[baseType] = new List<INamedTypeSymbol>();
                _bases[baseType].Add(symbol);
              }
            }
          }
        }

        /// <summary>
        /// Gets the class by it's base type.
        /// </summary>
        /// <param name="fqBaseName">Fully qualified name of the base type.</param>
        /// <returns></returns>
        public IEnumerable<ClassModel> GetByBaseType(string fqBaseName)
        {
            return FindByBaseType(fqBaseName).Select(symbol => new ClassModel(symbol));
        }
        /// <summary>
        /// Gets the class by it's type.
        /// </summary>
        /// <param name="fqName">Name of the fq.</param>
        /// <returns></returns>
        public ClassModel GetByType(string fqName)
        {
            var symbol = FindByType(fqName);
            return symbol!=null ? new ClassModel(symbol) : null;
        }

        internal IEnumerable<INamedTypeSymbol> FindByBaseType(string fqBaseName)
        {
            return _bases.TryGetValue(fqBaseName, out List<INamedTypeSymbol> list)
                ? list
                : Enumerable.Empty<INamedTypeSymbol>();
        }

        internal INamedTypeSymbol FindByType(string name)
        {
            return _classes.TryGetValue(name, out INamedTypeSymbol cls) ? cls : null;
        }

        private IEnumerable<INamedTypeSymbol> GetBaseClasses(SemanticModel model, BaseTypeDeclarationSyntax type)
        {
            var classSymbol = model.GetDeclaredSymbol(type) as INamedTypeSymbol;
            var returnValue = new List<INamedTypeSymbol>();
            while (classSymbol?.BaseType != null)
            {
                returnValue.Add(classSymbol.BaseType);
                if (!classSymbol.Interfaces.IsEmpty)
                    returnValue.AddRange(classSymbol.Interfaces);
                classSymbol = classSymbol.BaseType;
            }
            return returnValue;
        }
      /// <summary>
      /// Gets the fully qualified type string.
      /// </summary>
      /// <param name="symbol">The symbol.</param>
      /// <param name="incNs">if set to <c>true</c> Include full namespace for type.</param>
      /// <returns></returns>
      private string GetFullTypeString(ISymbol symbol, bool incNs = true)
      {
        var name = symbol.Name;
        if (!(symbol is INamedTypeSymbol)) return name;
        var arguments = ((INamedTypeSymbol)symbol).TypeArguments;
        if (arguments.Length <= 0) return incNs ? $"{GetFullNamespace(symbol)}.{name}" : name;

        var types = string.Join(", ", arguments.Select(ta => GetFullTypeString(ta, incNs)));
        return $"{name}<{types}>";
      }
      /// <summary>
      /// Gets the fully qualified namespace of type.
      /// </summary>
      /// <param name="symbol">The symbol.</param>
      /// <returns></returns>
      private string GetFullNamespace( ISymbol symbol)
      {
        var ns = symbol.ContainingNamespace;
        if (string.IsNullOrEmpty(ns?.Name))
          return null;

        var full = ns.GetFullNamespace();
        return full == null ? ns.Name : $"{full}.{ns.Name}";
      }
  }
}
