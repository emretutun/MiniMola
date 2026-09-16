using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class UserWorkScheduleDayConfiguration
    : IEntityTypeConfiguration<UserWorkScheduleDay>
{
    public void Configure(
        EntityTypeBuilder<UserWorkScheduleDay> builder)
    {
        builder.ToTable(
            "UserWorkScheduleDays",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_UserWorkScheduleDays_DayOfWeek",
                    "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");

                table.HasCheckConstraint(
                    "CK_UserWorkScheduleDays_TimeRange",
                    "[StartTime] <> [EndTime]");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.Property(x => x.IsWorkingDay)
            .HasDefaultValue(false);

        builder.Property(x => x.StartTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.HasIndex(
                x => new
                {
                    x.UserProfileId,
                    x.DayOfWeek
                })
            .IsUnique();

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}