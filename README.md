# 🐱 Pet Simulator - Masaüstü Sanal Pet Oyunu

Masaüstünüzde yaşayan, etkileşimli bir sanal kedi simülatörü! Nostaljik Tamagotchi tarzında, modern C# Windows Forms ile yazılmış.

![Pet Simulator](https://img.shields.io/badge/C%23-Windows%20Forms-purple)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2+-blue)
![License](https://img.shields.io/badge/License-MIT-green)


## ✨ Özellikler

### 🎮 Temel Özellikler
- **Şeffaf masaüstü pet** - Her zaman üstte, ekranınızda dolaşır
- **İhtiyaç sistemi** - Açlık, mutluluk ve enerji takibi
- **Gelişim evreleri** - Bebek → Genç → Yetişkin
- **Otomatik davranışlar** - Esneme, kaşınma, temizlenme
- **Fare takibi** - Pet'iniz farenizi takip edebilir

### 🎯 Mini Oyunlar & Aktiviteler
- **Top oyunu** - Top fırlatın, pet'iniz kovalasın
- **Numaralar** - Otur, pati ver, takla at komutları
- **Aksesuarlar** - Şapka, papyon, gözlük
- **Hava durumu** - Yağmur, kar, güneş efektleri

### 🏆 Başarım Sistemi
- İlk Besleme
- İlk Oyun
- Top Ustası (10 top yakala)
- 1 Haftalık (7 gün hayatta kal)
- Çok Mutlu Pet (80%+ mutluluk)
- Numara Ustası (İlk numara)

### 💾 Gelişmiş Özellikler
- **Veri kaydetme** - Pet'iniz kapatsanız bile devam eder
- **Pixel-art grafikleri** - Nostaljik, el yapımı tasarım
- **Global tuş kontrolleri** - Sistem genelinde çalışan kısayollar
- **Sistem tepsisi entegrasyonu** - Arka planda çalışır
- **Ses efektleri** - Açılıp kapatılabilir (Console.Beep ile)

## 🎮 Kontroller

### ⌨️ Klavye Kısayolları (Sistem Geneli)
| Tuş | Fonksiyon |
|-----|-----------|
| `I` | Bakım Menüsü (Yem ver, oyna, uyu) |
| `B` | Top Fırlat |
| `S` | Otur (Numara) |
| `P` | Pati Ver (Numara) |
| `T` | Takla At (Numara) |
| `A` | Aksesuar Menüsü |
| `H` | Başarımlar |

### 🖱️ Fare Kontrolleri
- **Sol Tık + Sürükle** - Pet'i taşı
- **Çift Tık** - Pet ile oyna
- **Sağ Tık** - Menüyü aç
- **Fare Üzerine Gelme** - Pet sizi takip edebilir

### 📋 Sistem Tepsisi
- **Çift Tık** - İstatistikleri göster
- **Sağ Tık** - Tam menü

## 🚀 Kurulum

### Gereksinimler
- Windows 7/8/10/11
- .NET Framework 4.7.2 veya üzeri

### Yükleme Adımları

#### Yöntem 1: Hazır EXE (Önerilen)
1. [Releases](https://github.com/gkdnz77/PetSimulator/releases) sayfasından son sürümü indirin
2. `PetSimulator.exe` dosyasını çalıştırın
3. İlk açılışta pet'inize isim verin
4. Keyfinize bakın! 🎉

#### Yöntem 2: Kaynak Koddan Derleme
```bash
# Repoyu klonlayın
git clone https://github.com/kullaniciadi/PetSimulator.git

# Visual Studio ile açın
PetSimulator.sln

# Build > Build Solution (Ctrl+Shift+B)
# Çalıştırın (F5)
```

## 📖 Kullanım Kılavuzu

### İlk Başlangıç
1. Programı açın
2. Pet'inize bir isim verin (örn: "Minnoş")
3. Pet ekranınızda görünecek ve dolaşmaya başlayacak

### Pet'inize Bakın
- **Açlık** düştüğünde `I` tuşuna basıp yem verin
- **Mutluluk** düştüğünde `I` tuşuna basıp oynayın
- **Enerji** düştüğünde pet otomatik uyuyacak veya `I` ile uyutabilirsiniz

### İstatistikleri Takip Edin
- Sistem tepsisindeki ikona **çift tıklayın**
- Veya **sağ tık** → İstatistikler

### Sesler
- Sistem tepsisi → Sağ tık → **"🔇 Sesler: Kapalı"**
- Varsayılan olarak sesler kapalıdır

## 🛠️ Teknik Detaylar

### Teknolojiler
- **C# Windows Forms** - UI Framework
- **GDI+** - Pixel-art rendering
- **Windows API** - Global keyboard hook
- **System.IO** - Veri kalıcılığı

### Mimari
```
PetSimulator/
├── Form1.cs              # Ana form ve game loop
├── Enums/
│   ├── PetState         # Pet durumları
│   ├── PetStage         # Gelişim evreleri
│   ├── PetTrick         # Numaralar
│   └── WeatherType      # Hava durumu
├── Classes/
│   ├── Accessory        # Aksesuar sistemi
│   └── Particle         # Hava partikülleri
└── Data/
    └── pet_data.txt     # Kaydedilen veriler
```

### Kayıt Konumu
```
%APPDATA%/PetSimulator/pet_data.txt
```

## 🎨 Özelleştirme

### Pet Renklerini Değiştirme
`DrawAwakePet()` metodunda:
```csharp
Color bodyColor = Color.FromArgb(255, 165, 0); // Turuncu
// Başka renkler:
// Color.Orange
// Color.Gray
// Color.White
```

### Animasyon Hızı
```csharp
moveTimer.Interval = 50;  // Hareket hızı (ms)
statsTimer.Interval = 3000; // İhtiyaç azalma hızı (ms)
```

### Yeni Aksesuar Eklemek
```csharp
ownedAccessories.Add(new Accessory { 
    Name = "Taç", 
    Icon = "♔", 
    Type = AccessoryType.Hat 
});
```

## 🐛 Bilinen Sorunlar

- [ ] Bazı sistemlerde `Console.Beep()` çalışmayabilir
- [ ] Çok hızlı fare hareketlerinde pet takip edemeyebilir
- [ ] Çoklu monitör kurulumlarında sınır kontrolü eksik

## 📜 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 👨‍💻 Geliştirici

---

<div align="center">

**⭐ Projeyi beğendiyseniz yıldız vermeyi unutmayın! ⭐**

Made with ❤️ and C#
