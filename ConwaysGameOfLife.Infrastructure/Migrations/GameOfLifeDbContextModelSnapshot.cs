﻿// <auto-generated />
using System;
using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ConwaysGameOfLife.Infrastructure.Migrations
{
    [DbContext(typeof(GameOfLifeDbContext))]
    partial class GameOfLifeDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("ConwaysGameOfLife.Infrastructure.Persistence.BoardEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("AliveCellsJson")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("AliveCells");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("Generation")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("MaxDimension")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Boards");
                });

            modelBuilder.Entity("ConwaysGameOfLife.Infrastructure.Persistence.BoardHistoryEntity", b =>
                {
                    b.Property<Guid>("BoardId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Generation")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AliveCellsJson")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("AliveCells");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("StateHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("BoardId", "Generation");

                    b.HasIndex("BoardId");

                    b.HasIndex("StateHash");

                    b.HasIndex("BoardId", "StateHash");

                    b.ToTable("BoardHistories");
                });
#pragma warning restore 612, 618
        }
    }
}
