using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Wox.Test
{
    [TestFixture]
    public class FuzzyMatcherTest
    {
        [Test]
        public void MatchTest()
        {
            var sources = new List<string>
            {
                "file open in browser-test",
                "Install Package",
                "add new bsd",
                "Inste",
                "aac"
            };


            var results = new List<Result>();
            foreach (var str in sources)
            {
                results.Add(new Result
                {
                    Title = str,
                    Score = FuzzyMatcher.Create("inst").Evaluate(str).Score
                });
            }

            results = results.Where(x => x.Score > 0).OrderByDescending(x => x.Score).ToList();

            Assert.IsTrue(results.Count == 3);
            Assert.IsTrue(results[0].Title == "Inste");
            Assert.IsTrue(results[1].Title == "Install Package");
            Assert.IsTrue(results[2].Title == "file open in browser-test");
        }

        [TestCase("c", 50)]
        [TestCase("ch", 50)]
        [TestCase("chr", 50)]
        [TestCase("chrom", 50)]
        public void FuzzyMatchSensitivityTest(string searchTerm, int searchSensitivity)
        {
            var sources = new List<string>
            {
                "Chrome",
                "Choose which programs you want Windows to use for activities like web browsing, editing photos, sending e-mail, and playing music.",
                "Candy Crush Saga from King",
                "Uninstall or change programs on your computer",
                "Add, change, and manage fonts on your computer"
            };

            var results = new List<Result>();
            foreach (var str in sources)
            {
                results.Add(new Result
                {
                    Title = str,
                    Score = FuzzyMatcher.Create(searchTerm).Evaluate(str).Score
                });
            }

            results = results.Where(x => x.Score > searchSensitivity).OrderByDescending(x => x.Score).ToList();

            Debug.WriteLine("");
            Debug.WriteLine("###############################################");

            Debug.WriteLine("SEARCHTERM: " + searchTerm + ", GreaterThanSearchSensitivityScore: " + searchSensitivity);
            foreach (var item in results)
                Debug.WriteLine("SCORE: " + item.Score.ToString() + ", SearchString: " + item.Title);

            Debug.WriteLine("###############################################");
            Debug.WriteLine("");



            //Assert.IsTrue(results.Count == 5);



            //Assert.IsTrue(results[0].Title == sources[1]);

            //Assert.IsTrue(results[0].Title == sources[0]);
            //Assert.IsFalse(results[1].Title == sources[1]);
            //Assert.IsTrue(results.Count == 3);
        }
    }
}
