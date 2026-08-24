using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordPoolItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WordPoolItemId",
                table: "DailyWordPuzzles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WordPoolItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Word = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordPoolItems", x => x.Id);
                    table.CheckConstraint("CK_WordPoolItems_MaxAttempts_Range", "[MaxAttempts] >= 1 AND [MaxAttempts] <= 10");
                    table.CheckConstraint("CK_WordPoolItems_Reward_Positive", "[RewardPoints] > 0");
                    table.CheckConstraint("CK_WordPoolItems_Word_Length", "LEN([Word]) = 5");
                });

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 1,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 2,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 3,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 4,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 5,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 6,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DailyWordPuzzles",
                keyColumn: "Id",
                keyValue: 7,
                column: "WordPoolItemId",
                value: null);

            migrationBuilder.InsertData(
                table: "WordPoolItems",
                columns: new[] { "Id", "CreatedAtUtc", "Hint", "IsActive", "MaxAttempts", "RewardPoints", "UpdatedAtUtc", "Word" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Sayfaları arasında farklı dünyalar saklar.", true, 6, 30, null, "KİTAP" },
                    { 2, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Gerçek ile hayalin birbirine karıştığı anlatı.", true, 6, 30, null, "MASAL" },
                    { 3, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Bahçelere renk ve güzel koku katar.", true, 6, 30, null, "ÇİÇEK" },
                    { 4, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Ağaçların birlikte yaşadığı geniş alan.", true, 6, 30, null, "ORMAN" },
                    { 5, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Karaların arasında ilerleyen doğal su yolu.", true, 6, 30, null, "IRMAK" },
                    { 6, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Deniz ile karanın buluştuğu yer.", true, 6, 30, null, "SAHİL" },
                    { 7, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Denizin kıyıya doğru ilerleyen hareketi.", true, 6, 30, null, "DALGA" },
                    { 8, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Suyun içinde veya nemli yerlerde yetişir.", true, 6, 30, null, "YOSUN" },
                    { 9, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Su üzerinde yol almak için kullanılır.", true, 6, 30, null, "TEKNE" },
                    { 10, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Dalgaların kıyıda bıraktığı beyazlık.", true, 6, 30, null, "KÖPÜK" },
                    { 11, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yüzeyden aşağıya doğru uzak olan.", true, 6, 30, null, "DERİN" },
                    { 12, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Ne sıcak ne de çok soğuk olan hava.", true, 6, 30, null, "SERİN" },
                    { 13, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Gürültüsüz ve telaşsız olma durumu.", true, 6, 30, null, "SAKİN" },
                    { 14, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Birine veya bir şeye duyulan güçlü yakınlık.", true, 6, 30, null, "SEVGİ" },
                    { 15, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Mutluluğun yüze yansıyan hâli.", true, 6, 30, null, "GÜLÜŞ" },
                    { 16, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "İçinde güzel ve olumlu duygular taşıyan.", true, 6, 30, null, "MUTLU" },
                    { 17, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "İnsana hoşluk ve rahatlık veren duygu.", true, 6, 30, null, "KEYİF" },
                    { 18, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Geçmişten geleceğe uzanan kesintisiz süreç.", true, 6, 30, null, "ZAMAN" },
                    { 19, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Bir sözün veya davranışın ifade ettiği şey.", true, 6, 30, null, "ANLAM" },
                    { 20, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Zihinde canlandırılan düşünce veya görüntü.", true, 6, 30, null, "HAYAL" },
                    { 21, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Kar ve bulutlarla sıkça ilişkilendirilen renk.", true, 6, 30, null, "BEYAZ" },
                    { 22, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Doğanın ve yaprakların belirgin rengi.", true, 6, 30, null, "YEŞİL" },
                    { 23, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Işığın engellenmesiyle oluşan karanlık alan.", true, 6, 30, null, "GÖLGE" },
                    { 24, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Günün güneş battıktan sonraki bölümü.", true, 6, 30, null, "AKŞAM" },
                    { 25, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Günün uyanışla başlayan ilk bölümü.", true, 6, 30, null, "SABAH" },
                    { 26, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Seslerin ritim ve uyumla birleşmesi.", true, 6, 30, null, "MÜZİK" },
                    { 27, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Ses veya hareketlerin düzenli tekrarı.", true, 6, 30, null, "RİTİM" },
                    { 28, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Söz ve melodinin birlikte oluşturduğu eser.", true, 6, 30, null, "ŞARKI" },
                    { 29, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Bir oyunda yapılan tek hareket.", true, 6, 30, null, "HAMLE" },
                    { 30, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Karşıt güçlerin uyum içinde kalması.", true, 6, 30, null, "DENGE" },
                    { 31, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Doğanın yeniden canlandığı mevsim.", true, 6, 30, null, "BAHAR" },
                    { 32, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Alt kısmı daha geniş olan tatlı bir meyve.", true, 6, 30, null, "ARMUT" },
                    { 33, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Kırmızı renkli, küçük ve çekirdekli meyve.", true, 6, 30, null, "KİRAZ" },
                    { 34, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yaz aylarında yetişen tatlı ve kokulu meyve.", true, 6, 30, null, "KAVUN" },
                    { 35, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Sarı renkli ve ekşi tada sahip meyve.", true, 6, 30, null, "LİMON" },
                    { 36, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Kırmızı renkli ve üzerinde küçük tohumları olan meyve.", true, 6, 30, null, "ÇİLEK" },
                    { 37, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Şekerli yiyeceklerin ortak özelliği.", true, 6, 30, null, "TATLI" },
                    { 38, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yiyecek ve içeceklere tat vermek için kullanılır.", true, 6, 30, null, "ŞEKER" },
                    { 39, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Un, su ve maya ile hazırlanan temel yiyecek.", true, 6, 30, null, "EKMEK" },
                    { 40, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yüksek ısıya sahip olma durumu.", true, 6, 30, null, "SICAK" },
                    { 41, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Düşük ısıya sahip olma durumu.", true, 6, 30, null, "SOĞUK" },
                    { 42, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Boyutu veya miktarı az olan.", true, 6, 30, null, "KÜÇÜK" },
                    { 43, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Boyutu veya miktarı fazla olan.", true, 6, 30, null, "BÜYÜK" },
                    { 44, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yapılması fazla çaba gerektirmeyen.", true, 6, 30, null, "KOLAY" },
                    { 45, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Zorluklar karşısında korkmadan davranan.", true, 6, 30, null, "CESUR" },
                    { 46, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Kuvveti veya etkisi yüksek olan.", true, 6, 30, null, "GÜÇLÜ" },
                    { 47, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Başkalarına karşı ince ve saygılı davranan.", true, 6, 30, null, "NAZİK" },
                    { 48, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Öğrenme ve deneyim sonucunda edinilen birikim.", true, 6, 30, null, "BİLGİ" },
                    { 49, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Olayları gözlem ve deneyle inceleyen çalışma alanı.", true, 6, 30, null, "BİLİM" },
                    { 50, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Zihinde oluşan düşünce.", true, 6, 30, null, "FİKİR" },
                    { 51, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Bir soruya karşılık verilen açıklama.", true, 6, 30, null, "CEVAP" },
                    { 52, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Gerçeğe veya kurala uygun olan.", true, 6, 30, null, "DOĞRU" },
                    { 53, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Geçmişte yaşanan olayları inceleyen alan.", true, 6, 30, null, "TARİH" },
                    { 54, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Üzerinde yaşadığımız gezegen.", true, 6, 30, null, "DÜNYA" },
                    { 55, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Nüfusu ve yerleşimi yoğun yaşam alanı.", true, 6, 30, null, "ŞEHİR" },
                    { 56, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Ev ve binaların arasında uzanan yol.", true, 6, 30, null, "SOKAK" },
                    { 57, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Yazı yazmak veya çizim yapmak için kullanılır.", true, 6, 30, null, "KALEM" },
                    { 58, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Eşyaları taşımaya yarayan günlük araç.", true, 6, 30, null, "ÇANTA" },
                    { 59, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Öğrencilerin birlikte ders gördüğü yer.", true, 6, 30, null, "SINIF" },
                    { 60, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Kitap veya başka yazılı eserler oluşturan kişi.", true, 6, 30, null, "YAZAR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWordPuzzles_WordPoolItemId",
                table: "DailyWordPuzzles",
                column: "WordPoolItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WordPoolItems_Word",
                table: "WordPoolItems",
                column: "Word",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyWordPuzzles_WordPoolItems_WordPoolItemId",
                table: "DailyWordPuzzles",
                column: "WordPoolItemId",
                principalTable: "WordPoolItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyWordPuzzles_WordPoolItems_WordPoolItemId",
                table: "DailyWordPuzzles");

            migrationBuilder.DropTable(
                name: "WordPoolItems");

            migrationBuilder.DropIndex(
                name: "IX_DailyWordPuzzles_WordPoolItemId",
                table: "DailyWordPuzzles");

            migrationBuilder.DropColumn(
                name: "WordPoolItemId",
                table: "DailyWordPuzzles");
        }
    }
}
