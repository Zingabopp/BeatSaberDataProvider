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
    public class ExpressionTests
    {
        [TestMethod]
        public void TestExpressions()
        {
            var minDate = new DateTime(2019, 7, 1);
            var maxDate = new DateTime(2019, 7, 3);
            var parameter = Expression.Parameter(typeof(Song), "s");
            var member = Expression.Property(parameter, "Uploaded");
            var minConstant = Expression.Constant(minDate);
            var maxConstant = Expression.Constant(maxDate);
            var body = Expression.AndAlso(
                Expression.GreaterThan(member, minConstant),
                Expression.LessThan(member, maxConstant));
            var finalExpression = Expression.Lambda<Func<Song, bool>>(body, parameter);

            var orderByPropertyExp = Expression.Lambda<Func<Song, DateTime>>(
                member, 
                parameter);

            var context = new SongDataContext();
            context.Database.EnsureCreated();
            //context.Songs.Where(finalExpression).OrderByDescending(s => s.Uploaded).Load();
            context.Songs.Where(finalExpression).OrderByDescending(orderByPropertyExp).Load();
            var test = context.Songs.Local.Select(s => new { s.SongName, s.Uploaded }).ToList();
            Assert.IsTrue(test.All(s => s.Uploaded > minDate && s.Uploaded < maxDate));
            //var test = context.Songs.Where(lam).OrderBy(s => s.Uploaded).Select(s => new { s.SongName, s.Uploaded }).ToList();
        }
    }
}
