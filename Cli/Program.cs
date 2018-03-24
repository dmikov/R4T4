using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using R4T4;

namespace Cli
{
  class Program
  {
    static void Main(string[] args)
    {
      var solutionFile = Path.GetFullPath("..\\..\\..\\R4T4.sln");
      var solution = new SolutionService(solutionFile);
      var project = solution.GetProject("ExampleClasses");
      var typeService = new ClassService(project);
      var classModel = typeService.GetByType("ExampleClasses.Models.Person");
      var classAttributes = classModel.Attributes;
    }
  }
}
