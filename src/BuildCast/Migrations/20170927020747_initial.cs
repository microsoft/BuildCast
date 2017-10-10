using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuildCast.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Duration = table.Column<TimeSpan>(nullable: false),
                    FeedId = table.Column<string>(nullable: true),
                    IsDownloaded = table.Column<bool>(nullable: false),
                    ItemThumbnail = table.Column<string>(nullable: true),
                    Key = table.Column<string>(nullable: true),
                    LocalFileName = table.Column<string>(nullable: true),
                    PublishDate = table.Column<DateTimeOffset>(nullable: false),
                    Subtitle = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaybackState",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EpisodeKey = table.Column<string>(nullable: true),
                    ListenProgress = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EpisodeId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EpisodeKey = table.Column<string>(nullable: true),
                    HasInk = table.Column<bool>(nullable: false),
                    NoteText = table.Column<string>(nullable: true),
                    Thumbnail = table.Column<byte[]>(nullable: true),
                    Time = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemeData",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ImageBytes = table.Column<byte[]>(nullable: true),
                    Ink = table.Column<byte[]>(nullable: true),
                    InkMeme = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemeData", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeCache");

            migrationBuilder.DropTable(
                name: "PlaybackState");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "Memes");

            migrationBuilder.DropTable(
                name: "MemeData");
        }
    }
}
