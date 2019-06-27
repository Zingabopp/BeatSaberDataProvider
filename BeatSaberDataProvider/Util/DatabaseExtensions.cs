using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Newtonsoft.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;

namespace BeatSaberDataProvider.Util
{
    public static class DatabaseExtensions
    {
        public static Song UpdateDB(this Song update, ref SongDataContext context)
        {
            if (context == null)
                context = new SongDataContext();
            var songEntry = context.Entry(context.Songs.Find(update.SongId));
            foreach (var item in songEntry.Properties)
            {
                object newVal = update[$"{item.Metadata.Name}"];
                if(newVal != null && !newVal.Equals(item.CurrentValue))
                {
                    item.CurrentValue = newVal;
                    Console.WriteLine($"Updating {update.SongName}'s {item.Metadata.Name} property from {item.OriginalValue.ToString()} to {newVal.ToString()}");
                }
            }
            return songEntry.Entity;
        }

        //public static Song UpdateFrom(this Song song, JToken token)
        //{
        //    throw new NotImplementedException();
        //    return song;
        //}
    }
}
