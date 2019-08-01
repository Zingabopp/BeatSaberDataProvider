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
    public class PlayerDataProvider_Tests
    {
        [TestMethod]
        public void ProviderTest()
        {
            var provider = new PlayerDataProvider();
            provider.Initialize();
        }
    }
}
