﻿using BeatSaberDataProvider.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BeatSaberDataProvider.DataProviders
{
    public class SongDataContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
        //public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<BeatmapDifficulty> BeatmapCharacteristics { get; set; }
        //public DbSet<CharacteristicDifficulty> CharacteristicDifficulties { get; set; }
        public DbSet<Difficulty> Difficulties { get; set; }
        public DbSet<Uploader> Uploaders { get; set; }
        public string DataSourcePath { get; set; }
        private bool ReadOnlyMode { get; set; }
        public bool UseLoggerFactory { get; set; }
        public bool UseLazyLoadingProxies { get; set; }
        public bool EnableSensitiveDataLogging { get; set; }
        public static readonly LoggerFactory MyLoggerFactory
    = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

        public static SongDataContext AsReadOnly()
        {
            return new SongDataContext(true);
        }

        public SongDataContext()
            : base() { }
        public SongDataContext(bool readOnly)
            : base()
        {
            ReadOnlyMode = readOnly;
        }

        public IIncludableQueryable<Song, ICollection<ScoreSaberDifficulty>> LoadQuery(IQueryable<Song> query)
        {
            return query
                    .Include(s => s.SongDifficulties)
                        .ThenInclude(sd => sd.Difficulty)
                    .Include(s => s.BeatmapDifficulties)
                        //.ThenInclude(bc => bc.Characteristic)
                    //.Include(s => s.BeatmapDifficulties)
                        //.ThenInclude(bc => bc.CharacteristicDifficulties)
                    .Include(s => s.Uploader)
                    .Include(s => s.ScoreSaberDifficulties);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("OnConfiguring");
            if (string.IsNullOrEmpty(DataSourcePath))
                DataSourcePath = "songs.db";
            optionsBuilder
                .UseSqlite($"Data Source={DataSourcePath}", x => x.SuppressForeignKeyEnforcement());

            if (ReadOnlyMode)
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                Console.WriteLine("Read only mode");
            }
            if (EnableSensitiveDataLogging)
                optionsBuilder.EnableSensitiveDataLogging(true);
            if (UseLoggerFactory)
                optionsBuilder.UseLoggerFactory(MyLoggerFactory);
            if (UseLazyLoadingProxies)
                optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasKey(s => s.SongId);
            modelBuilder.Entity<Song>()
                .HasAlternateKey(s => s.Hash);
            modelBuilder.Entity<ScoreSaberDifficulty>()
                .HasKey(d => d.ScoreSaberDifficultyId);
            //modelBuilder.Entity<Characteristic>()
            //    .HasKey(c => c.CharacteristicName);
            //modelBuilder.Entity<CharacteristicDifficulty>()
            //    .HasKey(cd => new { cd.Difficulty, cd.BeatmapCharacteristicKey });
            //modelBuilder.Entity<CharacteristicDifficulty>()
            //    .HasAlternateKey(cd => new { cd.BeatmapCharacteristicKey, cd.Difficulty });
            //modelBuilder.Entity<Difficulty>()
            //    .HasKey(d => d.DifficultyLevel);
            modelBuilder.Entity<Difficulty>()
                .HasKey(d => d.DifficultyName);
            modelBuilder.Entity<Uploader>()
                .HasKey(u => u.UploaderId);
            modelBuilder.Entity<Uploader>()
                .HasAlternateKey(u => u.UploaderName);

            modelBuilder.Entity<BeatmapDifficulty>()
                .HasKey(b => new { b.SongId, b.CharacteristicName, b.Difficulty });
            modelBuilder.Entity<SongDifficulty>()
                .HasKey(d => new { d.DifficultyName, d.SongId });

            modelBuilder.Entity<ScoreSaberDifficulty>()
                .HasOne(d => d.Song)
                .WithMany(s => s.ScoreSaberDifficulties)
                .HasForeignKey(d => d.SongHash)
                .HasPrincipalKey(s => s.Hash);

            modelBuilder.Entity<Song>()
                .HasOne(s => s.Uploader)
                .WithMany(u => u.Songs)
                .HasForeignKey(s => s.UploaderRefId)
                .HasPrincipalKey(u => u.UploaderId);

            //modelBuilder.Entity<BeatmapCharacteristic>()
            //    .HasOne(b => b.Characteristic)
            //    .WithMany(b => b.BeatmapCharacteristics)
            //    .HasForeignKey(b => b.CharacteristicName);

            modelBuilder.Entity<BeatmapDifficulty>()
                .HasOne(b => b.Song)
                .WithMany(b => b.BeatmapDifficulties)
                .HasForeignKey(b => b.SongId);

            //modelBuilder.Entity<BeatmapDifficulty>()
            //    .HasMany(bc => bc.CharacteristicDifficulties)
            //    .WithOne(cd => cd.BeatmapCharacteristic)
            //    .HasForeignKey(cd => cd.BeatmapCharacteristicKey);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Difficulty)
                .WithMany(b => b.SongDifficulties)
                .HasForeignKey(b => b.DifficultyName);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Song)
                .WithMany(b => b.SongDifficulties)
                .HasForeignKey(b => b.SongId);


        }

        public static void AddOrUpdate(JToken jSong)
        {

        }


    }
}
