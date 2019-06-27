using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using BeatSaberDataProvider.DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace BeatSaberDataProvider.DataProviders
{
    public class SongDataContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
        public DbSet<Difficulty> Difficulties { get; set; }
        public DbSet<Uploader> Uploaders { get; set; }
        public string DataSourcePath { get; set; }
        private bool ReadOnlyMode { get; set; }
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("OnConfiguring");
            if (string.IsNullOrEmpty(DataSourcePath))
                DataSourcePath = "songs.db";
            optionsBuilder
                .EnableSensitiveDataLogging(true).EnableDetailedErrors(true)
                .UseSqlite($"Data Source={DataSourcePath}", x => x.SuppressForeignKeyEnforcement());

            if (ReadOnlyMode)
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                Console.WriteLine("Read only mode");
            }
            //.UseLoggerFactory(MyLoggerFactory)
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasKey(s => s.SongId);
            modelBuilder.Entity<Song>()
                .HasAlternateKey(s => s.Hash);
            modelBuilder.Entity<ScoreSaberDifficulty>()
                .HasKey(d => d.ScoreSaberDifficultyId);
            modelBuilder.Entity<Characteristic>()
                .HasKey(c => c.CharacteristicId);
            modelBuilder.Entity<Difficulty>()
                .HasKey(d => d.DifficultyId);
            modelBuilder.Entity<Difficulty>()
                .HasAlternateKey(d => d.DifficultyName);
            modelBuilder.Entity<Uploader>()
                .HasKey(u => u.UploaderId);
            modelBuilder.Entity<Uploader>()
                .HasAlternateKey(u => u.UploaderName);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasKey(b => new { b.CharacteristicId, b.SongId });
            modelBuilder.Entity<SongDifficulty>()
                .HasKey(d => new { d.DifficultyId, d.SongId });

            modelBuilder.Entity<ScoreSaberDifficulty>()
                .HasOne(d => d.Song)
                .WithMany(s => s.ScoreSaberDifficulties)
                .HasForeignKey(d => d.SongHash)
                .HasPrincipalKey(s => s.Hash);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasOne(b => b.Characteristic)
                .WithMany(b => b.BeatmapCharacteristics)
                .HasForeignKey(b => b.CharacteristicId);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasOne(b => b.Song)
                .WithMany(b => b.BeatmapCharacteristics)
                .HasForeignKey(b => b.SongId);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Difficulty)
                .WithMany(b => b.SongDifficulties)
                .HasForeignKey(b => b.DifficultyId);

            modelBuilder.Entity<SongDifficulty>()
                .HasOne(b => b.Song)
                .WithMany(b => b.Difficulties)
                .HasForeignKey(b => b.SongId);


        }


    }
}
