using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Seed;

internal static class WordPoolItemSeed
{
    private static readonly DateTime SeedDate =
        new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyCollection<WordPoolItem> GetItems()
    {
        (int Id, string Word, string Hint)[] definitions =
        [
            (1, "KİTAP", "Sayfaları arasında farklı dünyalar saklar."),
            (2, "MASAL", "Gerçek ile hayalin birbirine karıştığı anlatı."),
            (3, "ÇİÇEK", "Bahçelere renk ve güzel koku katar."),
            (4, "ORMAN", "Ağaçların birlikte yaşadığı geniş alan."),
            (5, "IRMAK", "Karaların arasında ilerleyen doğal su yolu."),
            (6, "SAHİL", "Deniz ile karanın buluştuğu yer."),
            (7, "DALGA", "Denizin kıyıya doğru ilerleyen hareketi."),
            (8, "YOSUN", "Suyun içinde veya nemli yerlerde yetişir."),
            (9, "TEKNE", "Su üzerinde yol almak için kullanılır."),
            (10, "KÖPÜK", "Dalgaların kıyıda bıraktığı beyazlık."),

            (11, "DERİN", "Yüzeyden aşağıya doğru uzak olan."),
            (12, "SERİN", "Ne sıcak ne de çok soğuk olan hava."),
            (13, "SAKİN", "Gürültüsüz ve telaşsız olma durumu."),
            (14, "SEVGİ", "Birine veya bir şeye duyulan güçlü yakınlık."),
            (15, "GÜLÜŞ", "Mutluluğun yüze yansıyan hâli."),
            (16, "MUTLU", "İçinde güzel ve olumlu duygular taşıyan."),
            (17, "KEYİF", "İnsana hoşluk ve rahatlık veren duygu."),
            (18, "ZAMAN", "Geçmişten geleceğe uzanan kesintisiz süreç."),
            (19, "ANLAM", "Bir sözün veya davranışın ifade ettiği şey."),
            (20, "HAYAL", "Zihinde canlandırılan düşünce veya görüntü."),

            (21, "BEYAZ", "Kar ve bulutlarla sıkça ilişkilendirilen renk."),
            (22, "YEŞİL", "Doğanın ve yaprakların belirgin rengi."),
            (23, "GÖLGE", "Işığın engellenmesiyle oluşan karanlık alan."),
            (24, "AKŞAM", "Günün güneş battıktan sonraki bölümü."),
            (25, "SABAH", "Günün uyanışla başlayan ilk bölümü."),
            (26, "MÜZİK", "Seslerin ritim ve uyumla birleşmesi."),
            (27, "RİTİM", "Ses veya hareketlerin düzenli tekrarı."),
            (28, "ŞARKI", "Söz ve melodinin birlikte oluşturduğu eser."),
            (29, "HAMLE", "Bir oyunda yapılan tek hareket."),
            (30, "DENGE", "Karşıt güçlerin uyum içinde kalması."),

            (31, "BAHAR", "Doğanın yeniden canlandığı mevsim."),
            (32, "ARMUT", "Alt kısmı daha geniş olan tatlı bir meyve."),
            (33, "KİRAZ", "Kırmızı renkli, küçük ve çekirdekli meyve."),
            (34, "KAVUN", "Yaz aylarında yetişen tatlı ve kokulu meyve."),
            (35, "LİMON", "Sarı renkli ve ekşi tada sahip meyve."),
            (36, "ÇİLEK", "Kırmızı renkli ve üzerinde küçük tohumları olan meyve."),
            (37, "TATLI", "Şekerli yiyeceklerin ortak özelliği."),
            (38, "ŞEKER", "Yiyecek ve içeceklere tat vermek için kullanılır."),
            (39, "EKMEK", "Un, su ve maya ile hazırlanan temel yiyecek."),
            (40, "SICAK", "Yüksek ısıya sahip olma durumu."),

            (41, "SOĞUK", "Düşük ısıya sahip olma durumu."),
            (42, "KÜÇÜK", "Boyutu veya miktarı az olan."),
            (43, "BÜYÜK", "Boyutu veya miktarı fazla olan."),
            (44, "KOLAY", "Yapılması fazla çaba gerektirmeyen."),
            (45, "CESUR", "Zorluklar karşısında korkmadan davranan."),
            (46, "GÜÇLÜ", "Kuvveti veya etkisi yüksek olan."),
            (47, "NAZİK", "Başkalarına karşı ince ve saygılı davranan."),
            (48, "BİLGİ", "Öğrenme ve deneyim sonucunda edinilen birikim."),
            (49, "BİLİM", "Olayları gözlem ve deneyle inceleyen çalışma alanı."),
            (50, "FİKİR", "Zihinde oluşan düşünce."),

            (51, "CEVAP", "Bir soruya karşılık verilen açıklama."),
            (52, "DOĞRU", "Gerçeğe veya kurala uygun olan."),
            (53, "TARİH", "Geçmişte yaşanan olayları inceleyen alan."),
            (54, "DÜNYA", "Üzerinde yaşadığımız gezegen."),
            (55, "ŞEHİR", "Nüfusu ve yerleşimi yoğun yaşam alanı."),
            (56, "SOKAK", "Ev ve binaların arasında uzanan yol."),
            (57, "KALEM", "Yazı yazmak veya çizim yapmak için kullanılır."),
            (58, "ÇANTA", "Eşyaları taşımaya yarayan günlük araç."),
            (59, "SINIF", "Öğrencilerin birlikte ders gördüğü yer."),
            (60, "YAZAR", "Kitap veya başka yazılı eserler oluşturan kişi.")
        ];

        return definitions
            .Select(definition =>
                new WordPoolItem
                {
                    Id = definition.Id,
                    Word = definition.Word,
                    Hint = definition.Hint,
                    RewardPoints = 30,
                    MaxAttempts = 6,
                    IsActive = true,
                    CreatedAtUtc = SeedDate
                })
            .ToArray();
    }
}
