# LRP - Laboratuvar Kaynak Planlama Sistemi (Aşama-1)

Bu proje, **.NET 6 Minimal API**, **Entity Framework Core (SQLite)** ve **Vanilla JavaScript** kullanarak laboratuvar, bilgisayar ve öğrenci sorumluluk yönetimi sağlayan bir temel sistemdir.

## Özellikler

- Admin ve Student rolü ile kimlik doğrulama
- Admin panelinde laboratuvar ve bilgisayar yönetimi
- Bilgisayar ekleme ve atama işlemleri
- Öğrenci atandığında öğrenci hesabı otomatik oluşturma
- Öğrenci panelinde sadece kendisine atanan bilgisayar bilgilerini görüntüleme
- Tek sayfa uygulaması yaklaşımıyla içerik güncellemeleri

## Proje Yapısı

- `Program.cs` - uygulama başlangıcı ve API rotaları
- `Data/AppDbContext.cs` - EF Core veri bağlamı
- `Models/Entities.cs` - veri modelleri
- `wwwroot/index.html` - Admin paneli
- `wwwroot/student.html` - Öğrenci paneli
- `wwwroot/login.html` - Giriş sayfası
- `wwwroot/js/` - frontend JavaScript mantığı
- `wwwroot/css/style.css` - proje stil dosyası

## Kurulum ve Çalıştırma

1. `.NET 6 SDK` yüklü olmalıdır.
2. Proje klasörüne gidin:

```powershell
cd c:\Users\MONSTER\Desktop\Murat_Sonay__231903033
```

3. Bağımlılıkları yükleyin:

```powershell
dotnet restore
```

4. Uygulamayı çalıştırın:

```powershell
dotnet run --project LRP-Project-Vize-Murat-Sonay-231903033.csproj
```

5. Tarayıcıda `http://localhost:5000/login.html` adresine gidin.

## Giriş Bilgileri

- **Admin kullanıcı adı:** `admin`
- **Admin parola:** `Admin@123`

Öğrenciler için giriş bilgileri otomatik oluşturulur:

- Kullanıcı adı: öğrenci numarası
- Parola: `ÖğrenciNo@2026` (örnek: `231903006@2026`)

## Notlar

- Admin penceresi `wwwroot/index.html`, öğrenci penceresi `wwwroot/student.html` altında.
- Veritabanı dosyası proje kökünde `lrp.db` olarak tutulur.
- Eğer port doluysa, uygulamayı çalıştırmadan önce mevcut `dotnet` örneklerini kapatmanız gerekebilir.
