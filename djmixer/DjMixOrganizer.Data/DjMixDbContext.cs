using DjMixOrganizer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DjMixOrganizer.Data;

public class DjMixDbContext(DbContextOptions<DjMixDbContext> options) : DbContext(options)
{
    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<Mix> Mixes => Set<Mix>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Track>(track =>
        {
            // DisplayName is computed from Artist/Title in C# (Track.cs) —
            // there's no column for it, so EF should never try to map it.
            track.Ignore(t => t.DisplayName);

            // EF null-handles MusicalKey? automatically when the converter is
            // non-nullable MusicalKey <-> string. A MusicalKey?/string? converter
            // double-wraps nulls and can fail on SaveChanges.
            var musicalKeyConverter = new ValueConverter<MusicalKey, string>(
                key => key.Value,
                value => MusicalKey.Parse(value));

            track.Property(t => t.MusicalKey)
                .HasConversion(musicalKeyConverter)
                .HasMaxLength(8)
                .HasColumnType("varchar(8)");
        });

        modelBuilder.Entity<Mix>(mix =>
        {
            // Tracks has no public setter — EF fills it in through the
            // (List<MixTrackEntry>) constructor we added in Mix.cs instead
            // of trying (and failing) to find a settable property.
            mix.Navigation(m => m.Tracks).UsePropertyAccessMode(PropertyAccessMode.Field);

            mix.HasMany(m => m.Tracks)
                .WithOne()
                .HasForeignKey("MixId")
                .IsRequired();
        });

        // MixTrackEntry isn't a value fully owned by one Mix — it points at a
        // Track that's independently tracked (and reused across mixes) — so
        // it's a regular entity with a composite key, not an owned type.
        modelBuilder.Entity<MixTrackEntry>(entry =>
        {
            entry.HasKey("MixId", "TrackId");

            entry.HasOne(e => e.Track)
                .WithMany()
                .HasForeignKey("TrackId");

            entry.ToTable("MixTrackEntries");
        });
    }
}
