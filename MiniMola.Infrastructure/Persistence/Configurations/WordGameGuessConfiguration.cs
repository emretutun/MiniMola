using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class WordGameGuessConfiguration
    : IEntityTypeConfiguration<WordGameGuess>
{
    public void Configure(
        EntityTypeBuilder<WordGameGuess> builder)
    {
        builder.ToTable(
            "WordGameGuesses",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_WordGameGuesses_Guess_Length",
                    "LEN([Guess]) = 5");

                table.HasCheckConstraint(
                    "CK_WordGameGuesses_Pattern_Valid",
                    "LEN([ResultPattern]) = 5 "
                    + "AND [ResultPattern] NOT LIKE '%[^012]%'");

                table.HasCheckConstraint(
                    "CK_WordGameGuesses_AttemptNumber_Range",
                    "[AttemptNumber] >= 1 "
                    + "AND [AttemptNumber] <= 10");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Guess)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(x => x.ResultPattern)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasIndex(x => new
        {
            x.WordGameSessionId,
            x.AttemptNumber
        })
            .IsUnique();

        builder.HasOne(x => x.WordGameSession)
            .WithMany()
            .HasForeignKey(x => x.WordGameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}