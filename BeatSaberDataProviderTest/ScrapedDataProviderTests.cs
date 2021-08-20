using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSaberDataProvider;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.DataProviders;
using BeatSaberDataProvider.DataModels;
using Microsoft.EntityFrameworkCore;
using BeatSaberDataProvider.Util;
using System.Linq.Expressions;
using System;

namespace BeatSaberDataProviderTest
{
    [TestClass]
    public class ScrapedDataProviderTests
    {
        //[TestMethod]
        public void BuildDatabaseFromJsonTest()
        {
            var bsScrape = @"TestData\BeatSaverScrape.json";
            var ssScrape = @"TestData\ScoreSaberScrape.json";
            var dbPath = "songsTest.db";
            ScrapedDataProvider.BuildDatabaseFromJson(dbPath, bsScrape, ssScrape);
        }
    }
}
