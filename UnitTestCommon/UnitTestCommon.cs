using NUnit.Framework;
using System.IO;
using Soulseek;
using File = System.IO.File;
using Con = System.Console;
using System.Linq;

namespace UnitTestCommon
{
    public class Tests
    {
        public string SeekerTestingDirectory = null;
        [SetUp]
        public void Setup()
        {
            SeekerTestingDirectory = System.Environment.GetEnvironmentVariable("SEEKER_TESTING_DIR");
        }

        public void TestParsingBrowseResponse()
        {
            StreamReader sw = new StreamReader(System.IO.Path.Join(SeekerTestingDirectory, System.Reflection.MethodBase.GetCurrentMethod().Name + "_spec.txt"));
           
            string uname = null;
            while ((uname = sw.ReadLine()) != null)
            {
                if(uname.StartsWith('#'))
                {
                    continue;
                }
                Con.WriteLine(uname);
                var fstream = File.Open($"{SeekerTestingDirectory}\\{uname}_dir_response", FileMode.Open);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                BrowseResponse b = formatter.Deserialize(fstream) as BrowseResponse;
                Common.TreeNode<Soulseek.Directory> tree = Common.Algorithms.CreateTreeCore(b, false, null, null, uname, false);
                
                //FileStream ms = File.Open($"{SeekerTestingDirectory}\\{uname}_dir_solution", FileMode.Create); //uncomment to write the solution
                System.IO.MemoryStream ms = new MemoryStream();
                var streamWriter = new StreamWriter(ms);
                PrintTree(tree, 0, streamWriter);
                streamWriter.Flush();
                ms.Position = 0;
                StreamReader ourReader = new StreamReader(ms);
                FileStream fs = File.Open($"{SeekerTestingDirectory}\\{uname}_dir_solution", FileMode.Open);
                StreamReader solution = new StreamReader(fs);
                string solutionLine = null;
                string ourLine = null;
                int i=0;
                while((solutionLine = solution.ReadLine()) != null && (ourLine = ourReader.ReadLine()) != null)
                {
                    i++;
                    Assert.AreEqual(solutionLine, ourLine, $"Failure at line {i} soln: {solutionLine}");
                }
                ourLine = ourReader.ReadLine(); //since AND short circuit
                if (solutionLine != ourLine)
                {
                    Assert.Fail("Number of lines are different");
                }
            }

            
        }

        private void PrintLine(string s, StreamWriter sw)
        {
            Con.WriteLine(s);
            sw.WriteLine(s);
        }

        private void PrintTree(Common.TreeNode<Soulseek.Directory> tree, int depth, StreamWriter sw)
        {
            PrintLine(tree.Data.Name, sw);
            PrintLine(tree.Data.Files.Count.ToString(), sw);
            if (tree.Data.Files.Count > 0)
            {
                PrintLine(tree.Data.Files.First().Filename, sw);
            }
            foreach (Common.TreeNode<Soulseek.Directory> child in tree.Children)
            {
                PrintTree(child, depth + 1, sw);
            }
        }
    }
}