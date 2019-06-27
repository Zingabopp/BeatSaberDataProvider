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
        public static Song UpdateSong(this Song update, ref SongDataContext context)
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

        public static void AddOrUpdate<TEntity>(this SongDataContext context, TEntity updatedEntity, List<object[]> refChain = null)
            where TEntity : DatabaseDataType
        {
            if (context == null)
                context = new SongDataContext();
            var entity = context.Find(updatedEntity.GetType(), updatedEntity.PrimaryKey);
            
            if(entity == null)
            {
                context.Add(updatedEntity);
                return;
            }
            var existingEntity = context.Entry(entity);
            // Update properties.
            foreach (var oldProp in existingEntity.Properties)
            {
                Console.WriteLine($"Checking property: {oldProp.Metadata.Name}");
                object newVal = updatedEntity[$"{oldProp.Metadata.Name}"];                
                if (newVal != null && !newVal.Equals(oldProp.CurrentValue))
                {
                    oldProp.CurrentValue = newVal;
                    Console.WriteLine($"Updating ({string.Join(", ", updatedEntity.PrimaryKey)})'s {oldProp.Metadata.Name} property from {oldProp.OriginalValue?.ToString() ?? "null"} to {newVal?.ToString() ?? "null"}");
                }
            }
            /* Probably don't need to worry about this, if these change it'll be because the song was updated.
            // Update references
            foreach (var item in existingEntity.References)
            {
                if(item.CurrentValue is IDatabaseDataType refEntity)
                {
                    if (refChain == null)
                        refChain = new List<object[]>() { entity.PrimaryKey };
                    if (refChain.Any(i => i.SequenceEqual(refEntity.PrimaryKey)))
                        continue; // This object was already navigated to
                    
                }
            }
            // Update Collections
            */

            //return songEntry.Entity;
        }

        //public static Song UpdateFrom(this Song song, JToken token)
        //{
        //    throw new NotImplementedException();
        //    return song;
        //}
    }


}
