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
        public List<string> GetSearchStrings() 
            => new List<string>
            {
                "Chrome",
                "Choose which programs you want Windows to use for activities like web browsing, editing photos, sending e-mail, and playing music.",
                "Help cure hope raise on mind entity Chrome ",
                "Candy Crush Saga from King",
                "Uninstall or change programs on your computer",
                "Add, change, and manage fonts on your computer",
                "Last is chrome",
                "1111"
            };

        public List<int> GetPrecisionScores()
            => new List<int>
            {
                0, //no precision
                20, //low
                50 //regular
            };

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

        [TestCase("Chrome")]
        public void WhenGivenNotAllCharactersFoundInSearchStringThenShouldReturnZeroScore(string searchString)
        {
            var compareString = "Can have rum only in my glass";

            var scoreResult = FuzzyMatcher.Create(compareString).Evaluate(searchString).Score;

            Assert.True(scoreResult == 0);
        }


        //[TestCase("c", 50)]
        //[TestCase("ch", 50)]
        //[TestCase("chr", 50)]
        [TestCase("chrom")]
        [TestCase("chrome")]
        //[TestCase("chrom", 0)]
        //[TestCase("cand", 50)]
        //[TestCase("cpywa", 0)]
        [TestCase("ccs")]
        public void WhenGivenStringsAndAppliedPrecisionFilteringThenShouldReturnGreaterThanPrecisionScoreResults(string searchTerm)
        {
            var results = new List<Result>();
            
            foreach (var str in GetSearchStrings())
            {
                results.Add(new Result
                {
                    Title = str,
                    Score = FuzzyMatcher.Create(searchTerm).Evaluate(str).Score
                });
            }

            foreach(var precisionScore in GetPrecisionScores())
            {
                var filteredResult = results.Where(result => result.Score >= precisionScore).Select(result => result).OrderByDescending(x => x.Score).ToList();

                Debug.WriteLine("");
                Debug.WriteLine("###############################################");
                Debug.WriteLine("SEARCHTERM: " + searchTerm + ", GreaterThanSearchPrecisionScore: " + precisionScore);
                foreach (var item in filteredResult)
                {
                    Debug.WriteLine("SCORE: " + item.Score.ToString() + ", FoundString: " + item.Title);
                }
                Debug.WriteLine("###############################################");
                Debug.WriteLine("");

                Assert.IsFalse(filteredResult.Any(x => x.Score < precisionScore));
            }
        }
    }
}
