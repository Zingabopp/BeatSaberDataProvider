using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
using System;
using System.Collections.Generic;

namespace BeatSaberDataProvider.Util
{
    public static class DatabaseExtensions
    {
        public static void AddOrUpdate<TEntity>(this SongDataContext context, TEntity updatedEntity, List<object[]> refChain = null)
            where TEntity : DatabaseDataType
        {
            if (context == null)
                context = new SongDataContext();
            var entity = context.Find(updatedEntity.GetType(), updatedEntity.PrimaryKey);

            if (entity == null)
            {
                context.Add(updatedEntity);
                return;
            }
            var existingEntity = context.Entry(entity);
            // Update properties.
            foreach (var oldProp in existingEntity.Properties)
            {
                if (Attribute.IsDefined(oldProp.Metadata.PropertyInfo, typeof(Updatable)))
                {
                    Console.WriteLine($"Checking property: {oldProp.Metadata.Name}");
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

        //public static Song UpdateFrom(this Song song, JToken token)
        //{
        //    throw new NotImplementedException();
        //    return song;
        //}
    }


}
