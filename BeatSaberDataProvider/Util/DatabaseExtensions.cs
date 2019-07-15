using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSaberDataProvider.Util
{
    public static class DatabaseExtensions
    {
        public static void AddOrUpdate<TEntity>(this SongDataContext context, TEntity updatedEntity, List<object> refChain = null)
            where TEntity : DatabaseDataType
        {
            if (refChain == null)
                refChain = new List<object>();
            if (refChain.Contains(updatedEntity))
                return;
            refChain.Add(updatedEntity);
            if (context == null)
                context = new SongDataContext();
            var entity = context.Find(updatedEntity.GetType(), updatedEntity.PrimaryKey);

            if (entity == null)
            {
                entity = AddIfMissing(context, updatedEntity);

            }
            var existingEntity = context.Entry(entity);
            // Update properties.
            foreach (var oldProp in existingEntity.Properties)
            {
                if (Attribute.IsDefined(oldProp.Metadata.PropertyInfo, typeof(Updatable)))
                {
                    //Console.WriteLine($"Checking property: {oldProp.Metadata.Name}");
                    object newVal = updatedEntity[$"{oldProp.Metadata.Name}"];
                    if (newVal != null && !newVal.Equals(oldProp.CurrentValue))
                    {
                        oldProp.CurrentValue = newVal;
                        Console.WriteLine($"Updating ({string.Join(", ", updatedEntity.PrimaryKey)})'s {oldProp.Metadata.Name} property from {oldProp.OriginalValue?.ToString() ?? "null"} to {newVal?.ToString() ?? "null"}");
                    }
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

        public static TEntity AddIfMissing<TEntity>(this SongDataContext context, TEntity entityToAdd, List<object> refChain = null, Func<TEntity, bool> searchPredicate = null)
            where TEntity : DatabaseDataType
        {
            if (refChain == null)
                refChain = new List<object>();
            if (refChain.Contains(entityToAdd))
                return entityToAdd;
            if (context == null)
                context = new SongDataContext();
            TEntity entity = null;
            if (searchPredicate == null)
            {
                entity = context.Find<TEntity>(entityToAdd.PrimaryKey);
                //entity = context.Set<TEntity>().Where(e => e.PrimaryKey == entityToAdd.PrimaryKey).Take(1).FirstOrDefault();
            }
            else
            {
                entity = context.Set<TEntity>().Where(searchPredicate).Take(1).FirstOrDefault();
            }
            EntityEntry newEntity;
            if (entity == null)
            {
                newEntity = context.Add(entityToAdd);
                
            }
            else
                newEntity = context.Entry(entity);

            if (entity == null)
                newEntity.State = EntityState.Added;

            refChain.Add(newEntity.Entity);
            foreach (var nav in newEntity.Navigations)
            {
                if (nav.CurrentValue == null)
                    continue;
                if (nav.CurrentValue is IEnumerable<DatabaseDataType> collection)
                {
                    foreach (var item in collection)
                    {
                        context.AddIfMissing(((object)item), refChain);
                    }
                }
                else
                    context.AddIfMissing(nav.CurrentValue, refChain);
            }
            return (TEntity)newEntity.Entity;


        }

        public static DatabaseDataType AddIfMissing(this SongDataContext context, object entityToAdd, List<object> refChain = null, Func<DatabaseDataType, bool> searchPredicate = null)
        {
            if (!typeof(DatabaseDataType).IsAssignableFrom(entityToAdd.GetType()))
                throw new ArgumentException("EntityToAdd is not a DatabaseDataType");

            DatabaseDataType retVal = entityToAdd as DatabaseDataType;
            if (refChain == null)
                refChain = new List<object>();
            if (refChain.Contains(entityToAdd))
                return retVal;


            if (entityToAdd is Song song)
                retVal = context.AddIfMissing<Song>(song, refChain);
            else if (entityToAdd is ScoreSaberDifficulty ssDiff)
                retVal = context.AddIfMissing<ScoreSaberDifficulty>(ssDiff, refChain);
            //else if (entityToAdd is Characteristic characteristic)
            //    retVal = context.AddIfMissing<Characteristic>(characteristic, refChain);
            else if (entityToAdd is BeatmapCharacteristic bmChar)
                retVal = context.AddIfMissing<BeatmapCharacteristic>(bmChar, refChain);//, bc => (bc.CharacteristicName == bmChar.CharacteristicName && bc.SongId == bmChar.SongId));
            else if (entityToAdd is CharacteristicDifficulty charDiff)
                retVal = context.AddIfMissing<CharacteristicDifficulty>(charDiff, refChain);//, cd => (cd.Difficulty == charDiff.Difficulty && cd.BeatmapCharacteristic.SongId == charDiff.BeatmapCharacteristic.SongId));
            //else if (entityToAdd is SongDifficulty songDiff)
            //    retVal = context.AddIfMissing<SongDifficulty>(songDiff, refChain);
            //else if (entityToAdd is Difficulty difficulty)
            //    retVal = context.AddIfMissing<Difficulty>(difficulty, refChain);
            else if (entityToAdd is Uploader uploader)
                retVal = context.AddIfMissing<Uploader>(uploader, refChain);

            return retVal;
        }

        //public static Song UpdateFrom(this Song song, JToken token)
        //{
        //    throw new NotImplementedException();
        //    return song;
        //}
    }


}
