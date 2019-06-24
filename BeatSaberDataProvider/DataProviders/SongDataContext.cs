using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using BeatSaberDataProvider.DataModels;

namespace BeatSaberDataProvider.DataProviders
{
    public class SongDataContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<Difficulty> Difficulties { get; set; }
        public DbSet<Uploader> Uploaders { get; set; }
        public DbSet<SongMetadata> Metadatas { get; set; }
        public string DataSourcePath { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrEmpty(DataSourcePath))
                DataSourcePath = "songs.db";
            optionsBuilder.UseSqlite($"Data Source={DataSourcePath}");
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasKey(s => s.Hash);
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
            modelBuilder.Entity<JsonMetaData>()
                .HasKey(m => m.SongId);
            modelBuilder.Entity<JsonStats>()
                .HasKey(m => m.SongId);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasKey(b => new { b.CharactersticId, b.SongId });
            modelBuilder.Entity<SongDifficulty>()
                .HasKey(d => new { d.DifficultyId, d.SongId });

            modelBuilder.Entity<JsonMetaData>()
                .HasOne(m => m.Song)
                .WithOne(s => s.Metadata)
                .HasForeignKey<JsonMetaData>(m => m.SongId);
            modelBuilder.Entity<JsonStats>()
                .HasOne(st => st.Song)
                .WithOne(s => s.Stats)
                .HasForeignKey<JsonStats>(st => st.SongId);

            modelBuilder.Entity<BeatmapCharacteristic>()
                .HasOne(b => b.Characteristic)
                .WithMany(b => b.BeatmapCharacteristics)
                .HasForeignKey(b => b.CharactersticId);

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
                .WithMany(b => b.Metadata.Difficulties)
                .HasForeignKey(b => b.SongId);
        }


    }
}
