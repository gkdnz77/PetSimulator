using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PetSimulator
{
    public partial class Form1 : Form
    {
        // Global Keyboard Hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static IntPtr hookId = IntPtr.Zero;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc hookCallback;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private System.Windows.Forms.Timer moveTimer;
        private System.Windows.Forms.Timer statsTimer;
        private System.Windows.Forms.Timer animationTimer;
        private System.Windows.Forms.Timer ballTimer;
        private System.Windows.Forms.Timer weatherTimer;
        private Random random;
        private int velocityX = 2;
        private int velocityY = 2;
        private int hunger = 100;
        private int happiness = 100;
        private int energy = 100;
        private int age = 0;
        private DateTime birthDate;
        private string petName = "Kedi";
        private PetState currentState = PetState.Walking;
        private int animationFrame = 0;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private int facingDirection = 1;
        private int sleepCounter = 0;
        private bool isFollowingMouse = false;
        private Point targetMousePosition;
        private PetStage petStage = PetStage.Baby;
        private int idleCounter = 0;

        // Top Oyunu
        private bool ballActive = false;
        private Point ballPosition;
        private int ballVelocityX = 3;
        private int ballVelocityY = -5;
        private int ballRadius = 10;
        private Color ballColor = Color.Red;
        private int ballBounceCount = 0;

        // Baþarýmlar
        private Dictionary<string, bool> achievements = new Dictionary<string, bool>();
        private int totalFeeds = 0;
        private int totalPlays = 0;
        private int ballsCaught = 0;

        // Pet Tricks
        private PetTrick currentTrick = PetTrick.None;
        private int trickFrame = 0;

        // Aksesuarlar
        private List<Accessory> ownedAccessories = new List<Accessory>();
        private Accessory currentAccessory = null;

        // Hava Durumu
        private WeatherType currentWeather = WeatherType.Sunny;
        private List<Particle> weatherParticles = new List<Particle>();

        // Mouse dragging
        private bool isDragging = false;
        private Point dragStart;

        enum PetState
        {
            Walking,
            Idle,
            Happy,
            Sad,
            Eating,
            Sleeping,
            Playing,
            Stretching,
            Scratching,
            Grooming,
            FollowingMouse,
            ChasingBall,
            DoingTrick
        }

        enum PetStage
        {
            Baby,
            Young,
            Adult
        }

        enum PetTrick
        {
            None,
            Sit,
            Paw,
            Flip
        }

        enum WeatherType
        {
            Sunny,
            Rainy,
            Snowy,
            Cloudy
        }

        class Accessory
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public AccessoryType Type { get; set; }
        }

        enum AccessoryType
        {
            Hat,
            Bowtie,
            Glasses
        }

        class Particle
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float VelocityY { get; set; }
            public float VelocityX { get; set; }
            public Color Color { get; set; }
        }

        public Form1()
        {
            LoadPetData();
            InitializeForm();
            InitializePet();
            SetupTrayIcon();
            SetupGlobalKeyboardHook();
            InitializeAchievements();
            InitializeAccessories();
            InitializeWeather();

            if (string.IsNullOrEmpty(petName) || petName == "Kedi")
            {
                AskPetName();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Form yüklendiðinde çalýþacak kodlar (þu an boþ)
        }

        private void InitializeAchievements()
        {
            if (!achievements.ContainsKey("first_feed"))
                achievements["first_feed"] = totalFeeds > 0;
            if (!achievements.ContainsKey("first_play"))
                achievements["first_play"] = totalPlays > 0;
            if (!achievements.ContainsKey("ball_master"))
                achievements["ball_master"] = ballsCaught >= 10;
            if (!achievements.ContainsKey("week_old"))
                achievements["week_old"] = age >= 140;
            if (!achievements.ContainsKey("happy_pet"))
                achievements["happy_pet"] = happiness >= 80;
            if (!achievements.ContainsKey("trick_master"))
                achievements["trick_master"] = false;
        }

        private void InitializeAccessories()
        {
            ownedAccessories.Add(new Accessory { Name = "Þapka", Icon = "??", Type = AccessoryType.Hat });
            ownedAccessories.Add(new Accessory { Name = "Papyon", Icon = "??", Type = AccessoryType.Bowtie });
            ownedAccessories.Add(new Accessory { Name = "Gözlük", Icon = "??", Type = AccessoryType.Glasses });
        }

        private void InitializeWeather()
        {
            currentWeather = WeatherType.Sunny;
            weatherTimer = new System.Windows.Forms.Timer();
            weatherTimer.Interval = 30000;
            weatherTimer.Tick += WeatherTimer_Tick;
            weatherTimer.Start();
        }

        private void WeatherTimer_Tick(object sender, EventArgs e)
        {
            if (random.Next(100) < 20)
            {
                Array values = Enum.GetValues(typeof(WeatherType));
                currentWeather = (WeatherType)values.GetValue(random.Next(values.Length));

                string weatherMsg = currentWeather == WeatherType.Rainy ? "Yaðmur yaðýyor! ?" :
                                   currentWeather == WeatherType.Snowy ? "Kar yaðýyor! ??" :
                                   currentWeather == WeatherType.Cloudy ? "Hava bulutlu ??" :
                                   "Hava güneþli! ??";

                trayIcon.ShowBalloonTip(2000, "Hava Deðiþti!", weatherMsg, ToolTipIcon.Info);
                weatherParticles.Clear();
            }
        }

        private void SetupGlobalKeyboardHook()
        {
            hookCallback = HookCallback;
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => HandleKeyPress(key)));
                }
                else
                {
                    HandleKeyPress(key);
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private void HandleKeyPress(Keys key)
        {
            switch (key)
            {
                case Keys.I:
                    ShowFeedMenu();
                    break;
                case Keys.B:
                    SpawnBall();
                    break;
                case Keys.S:
                    PerformTrick(PetTrick.Sit);
                    break;
                case Keys.P:
                    PerformTrick(PetTrick.Paw);
                    break;
                case Keys.T:
                    PerformTrick(PetTrick.Flip);
                    break;
                case Keys.A:
                    ShowAccessoryMenu();
                    break;
                case Keys.H:
                    ShowAchievements();
                    break;
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowStatsMessage();
        }

        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            animationFrame++;
            trickFrame++;

            UpdateWeatherParticles();

            if (currentState == PetState.Sleeping)
            {
                sleepCounter++;
                if (sleepCounter > 200)
                {
                    currentState = PetState.Idle;
                    sleepCounter = 0;
                }
                this.Invalidate();
                return;
            }

            if (currentState == PetState.Stretching || currentState == PetState.Scratching ||
                currentState == PetState.Grooming || currentState == PetState.DoingTrick)
            {
                this.Invalidate();
                return;
            }

            if (currentState == PetState.ChasingBall && ballActive)
            {
                int dx = ballPosition.X - (this.Left + this.Width / 2);
                int dy = ballPosition.Y - (this.Top + this.Height / 2);
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > 20)
                {
                    velocityX = (int)(dx / distance * 5);
                    velocityY = (int)(dy / distance * 5);

                    if (velocityX != 0)
                        facingDirection = velocityX > 0 ? 1 : -1;
                }
            }
            else if (isFollowingMouse)
            {
                Point mousePos = Cursor.Position;
                int dx = mousePos.X - (this.Left + this.Width / 2);
                int dy = mousePos.Y - (this.Top + this.Height / 2);
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 30)
                {
                    isFollowingMouse = false;
                    currentState = PetState.Happy;
                    happiness = Math.Min(100, happiness + 5);
                    PlaySound("purr");
                    System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => { if (currentState == PetState.Happy) currentState = PetState.Idle; }));
                    });
                }
                else if (distance > 500)
                {
                    isFollowingMouse = false;
                    currentState = PetState.Sad;
                    System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => { if (currentState == PetState.Sad) currentState = PetState.Idle; }));
                    });
                }
                else
                {
                    velocityX = (int)(dx / distance * 4);
                    velocityY = (int)(dy / distance * 4);

                    if (velocityX != 0)
                        facingDirection = velocityX > 0 ? 1 : -1;
                }
            }
            else if (energy < 20 && currentState != PetState.Sleeping)
            {
                currentState = PetState.Sleeping;
                velocityX = 0;
                velocityY = 0;
            }
            else if (currentState == PetState.Walking || currentState == PetState.Idle ||
                     currentState == PetState.FollowingMouse)
            {
                if (random.Next(100) < 3)
                {
                    velocityX = random.Next(-4, 5);
                    velocityY = random.Next(-4, 5);

                    if (velocityX != 0)
                        facingDirection = velocityX > 0 ? 1 : -1;
                }

                if (random.Next(100) < 1)
                {
                    velocityX = 0;
                    velocityY = 0;
                }
            }

            int newX = this.Left + velocityX;
            int newY = this.Top + velocityY;

            Rectangle screen = Screen.FromPoint(this.Location).WorkingArea;
            if (newX < screen.Left || newX > screen.Right - this.Width)
            {
                velocityX = -velocityX;
                facingDirection = -facingDirection;
            }
            if (newY < screen.Top || newY > screen.Bottom - this.Height)
                velocityY = -velocityY;

            this.Location = new Point(newX, newY);

            if (!isFollowingMouse && currentState != PetState.Stretching &&
                currentState != PetState.Scratching && currentState != PetState.Grooming &&
                currentState != PetState.ChasingBall)
            {
                if (velocityX == 0 && velocityY == 0)
                {
                    idleCounter++;
                    if (idleCounter > 20)
                        currentState = PetState.Idle;
                }
                else
                {
                    idleCounter = 0;
                    if (currentState != PetState.Happy && currentState != PetState.Eating &&
                        currentState != PetState.Playing && currentState != PetState.Sleeping)
                        currentState = PetState.Walking;
                }

                if (happiness < 30 && currentState != PetState.Eating && currentState != PetState.Playing)
                    currentState = PetState.Sad;
            }

            this.Invalidate();
        }

        private void UpdateWeatherParticles()
        {
            if (currentWeather == WeatherType.Rainy)
            {
                if (random.Next(3) == 0)
                {
                    weatherParticles.Add(new Particle
                    {
                        X = random.Next(Screen.PrimaryScreen.WorkingArea.Width),
                        Y = 0,
                        VelocityY = random.Next(10, 15),
                        VelocityX = random.Next(-2, 2),
                        Color = Color.FromArgb(150, 100, 150, 255)
                    });
                }
            }
            else if (currentWeather == WeatherType.Snowy)
            {
                if (random.Next(5) == 0)
                {
                    weatherParticles.Add(new Particle
                    {
                        X = random.Next(Screen.PrimaryScreen.WorkingArea.Width),
                        Y = 0,
                        VelocityY = random.Next(2, 5),
                        VelocityX = random.Next(-3, 3),
                        Color = Color.White
                    });
                }
            }

            for (int i = weatherParticles.Count - 1; i >= 0; i--)
            {
                weatherParticles[i].Y += weatherParticles[i].VelocityY;
                weatherParticles[i].X += weatherParticles[i].VelocityX;

                if (weatherParticles[i].Y > Screen.PrimaryScreen.WorkingArea.Height)
                {
                    weatherParticles.RemoveAt(i);
                }
            }

            if (weatherParticles.Count > 100)
            {
                weatherParticles.RemoveRange(0, weatherParticles.Count - 100);
            }
        }

        private void StatsTimer_Tick(object sender, EventArgs e)
        {
            if (currentState == PetState.Sleeping)
            {
                energy = Math.Min(100, energy + 3);
                hunger = Math.Max(0, hunger - 1);
            }
            else
            {
                hunger = Math.Max(0, hunger - 2);
                happiness = Math.Max(0, happiness - 1);
                energy = Math.Max(0, energy - 1);
            }

            age++;
            UpdatePetStage();

            if (hunger < 30)
                happiness = Math.Max(0, happiness - 2);

            if (energy < 20)
                happiness = Math.Max(0, happiness - 1);

            if (age >= 140 && !achievements["week_old"])
            {
                achievements["week_old"] = true;
                ShowAchievementUnlocked("1 Haftalýk", "Pet'in 1 hafta yaþadý! ??");
            }

            if (happiness >= 80 && !achievements["happy_pet"])
            {
                achievements["happy_pet"] = true;
                ShowAchievementUnlocked("Çok Mutlu Pet", "Pet'in çok mutlu! ??");
            }

            UpdateTrayMenu();
            UpdateTrayIconStatus();
            SavePetData();

            if (hunger < 20 && hunger % 6 == 0)
            {
                trayIcon.ShowBalloonTip(2000, $"{petName} acýktý! ??", "Pet'iniz çok aç, yem verin!", ToolTipIcon.Warning);
                PlaySound("meow");
            }
            if (happiness < 20 && happiness % 6 == 0)
            {
                trayIcon.ShowBalloonTip(2000, $"{petName} sýkýldý! ??", "Pet'iniz mutsuz, onunla oynayýn!", ToolTipIcon.Warning);
            }
            if (energy < 20 && energy % 6 == 0)
            {
                trayIcon.ShowBalloonTip(2000, $"{petName} yorgun! ??", "Pet'iniz çok yorgun, dinlenmeli!", ToolTipIcon.Info);
            }
        }

        private void UpdatePetStage()
        {
            PetStage oldStage = petStage;

            if (age < 200)
                petStage = PetStage.Baby;
            else if (age < 600)
                petStage = PetStage.Young;
            else
                petStage = PetStage.Adult;

            if (oldStage != petStage)
            {
                string stageText = petStage == PetStage.Young ? "genç" : "yetiþkin";
                trayIcon.ShowBalloonTip(3000, $"{petName} büyüdü! ??",
                    $"{petName} artýk {stageText} bir kedi!", ToolTipIcon.Info);
                PlaySound("meow");
            }
        }

        private string GetAgeString()
        {
            int days = age / 20;
            string stage = petStage == PetStage.Baby ? "Bebek" :
                          petStage == PetStage.Young ? "Genç" : "Yetiþkin";
            return $"{days} gün ({stage})";
        }

        private void UpdateTrayIconStatus()
        {
            string status = "Saðlýklý";
            if (hunger < 30 || happiness < 30 || energy < 30)
                status = "Bakým Gerekli";
            if (hunger < 10 || happiness < 10)
                status = "Acil!";

            trayIcon.Text = $"{petName} - {status}\nAçlýk:{hunger}% Mutluluk:{happiness}% Enerji:{energy}%";
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawWeather(g);
            DrawPet(g);

            if (currentAccessory != null)
            {
                DrawAccessory(g);
            }

            if (ballActive)
            {
                DrawBall(g);
            }
        }

        private void DrawWeather(Graphics g)
        {
            foreach (var particle in weatherParticles)
            {
                if (particle.X >= this.Left && particle.X <= this.Left + this.Width &&
                    particle.Y >= this.Top && particle.Y <= this.Top + this.Height)
                {
                    int localX = (int)(particle.X - this.Left);
                    int localY = (int)(particle.Y - this.Top);

                    using (SolidBrush brush = new SolidBrush(particle.Color))
                    {
                        if (currentWeather == WeatherType.Rainy)
                        {
                            g.FillRectangle(brush, localX, localY, 2, 8);
                        }
                        else if (currentWeather == WeatherType.Snowy)
                        {
                            g.FillEllipse(brush, localX, localY, 4, 4);
                        }
                    }
                }
            }

            string weatherIcon = currentWeather == WeatherType.Sunny ? "??" :
                                currentWeather == WeatherType.Rainy ? "???" :
                                currentWeather == WeatherType.Snowy ? "??" : "??";

            using (Font font = new Font("Arial", 10))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                g.DrawString(weatherIcon, font, brush, 2, 2);
            }
        }

        private void DrawBall(Graphics g)
        {
            int localX = ballPosition.X - this.Left;
            int localY = ballPosition.Y - this.Top;

            if (localX >= -ballRadius * 2 && localX <= this.Width + ballRadius * 2 &&
                localY >= -ballRadius * 2 && localY <= this.Height + ballRadius * 2)
            {
                using (SolidBrush brush = new SolidBrush(ballColor))
                using (Pen outline = new Pen(Color.Black, 2))
                {
                    g.FillEllipse(brush, localX, localY, ballRadius * 2, ballRadius * 2);
                    g.DrawEllipse(outline, localX, localY, ballRadius * 2, ballRadius * 2);

                    using (SolidBrush whiteBrush = new SolidBrush(Color.FromArgb(150, Color.White)))
                    {
                        g.FillEllipse(whiteBrush, localX + 3, localY + 3, 6, 6);
                    }
                }
            }
        }

        private void DrawAccessory(Graphics g)
        {
            int s = petStage == PetStage.Baby ? 5 : petStage == PetStage.Young ? 6 : 7;

            using (Font font = new Font("Arial", s * 2))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                if (currentAccessory.Type == AccessoryType.Hat)
                {
                    g.DrawString(currentAccessory.Icon, font, brush, s * 3, s * 0);
                }
                else if (currentAccessory.Type == AccessoryType.Bowtie)
                {
                    g.DrawString(currentAccessory.Icon, font, brush, s * 4, s * 5);
                }
                else if (currentAccessory.Type == AccessoryType.Glasses)
                {
                    g.DrawString(currentAccessory.Icon, font, brush, s * 3, s * 3);
                }
            }
        }

        private void DrawPet(Graphics g)
        {
            int s = petStage == PetStage.Baby ? 5 : petStage == PetStage.Young ? 6 : 7;

            Color bodyColor = Color.FromArgb(255, 165, 0);

            if (happiness < 30)
                bodyColor = Color.Gray;
            else if (happiness > 70)
                bodyColor = Color.FromArgb(255, 200, 50);

            Color darkColor = Color.FromArgb(Math.Max(0, bodyColor.R - 50),
                                           Math.Max(0, bodyColor.G - 50),
                                           Math.Max(0, bodyColor.B - 50));

            int xOffset = facingDirection == 1 ? 0 : 14 * s;

            if (currentState == PetState.Sleeping)
            {
                DrawSleepingPet(g, s, bodyColor, darkColor, xOffset);
            }
            else if (currentState == PetState.Stretching)
            {
                DrawStretchingPet(g, s, bodyColor, darkColor, xOffset);
            }
            else if (currentState == PetState.Scratching)
            {
                DrawScratchingPet(g, s, bodyColor, darkColor, xOffset);
            }
            else if (currentState == PetState.Grooming)
            {
                DrawGroomingPet(g, s, bodyColor, darkColor, xOffset);
            }
            else if (currentState == PetState.DoingTrick)
            {
                DrawTrickPet(g, s, bodyColor, darkColor, xOffset);
            }
            else
            {
                DrawAwakePet(g, s, bodyColor, darkColor, xOffset);
            }
        }

        private void DrawTrickPet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int baseX = 2;
            int dir = facingDirection;

            if (currentTrick == PetTrick.Sit)
            {
                for (int i = 2; i <= 7; i++)
                    FillPixel(g, baseX + i * dir, 2, s, bodyColor, xOffset);
                for (int i = 1; i <= 8; i++)
                    FillPixel(g, baseX + i * dir, 3, s, bodyColor, xOffset);

                FillPixel(g, baseX + 2 * dir, 1, s, bodyColor, xOffset);
                FillPixel(g, baseX + 7 * dir, 1, s, bodyColor, xOffset);

                FillPixel(g, baseX + 3 * dir, 3, s, Color.Black, xOffset);
                FillPixel(g, baseX + 6 * dir, 3, s, Color.Black, xOffset);

                for (int i = 3; i <= 6; i++)
                    FillPixel(g, baseX + i * dir, 4, s, bodyColor, xOffset);
                for (int i = 2; i <= 7; i++)
                    FillPixel(g, baseX + i * dir, 5, s, bodyColor, xOffset);
                for (int i = 2; i <= 7; i++)
                    FillPixel(g, baseX + i * dir, 6, s, bodyColor, xOffset);
                for (int i = 3; i <= 6; i++)
                    FillPixel(g, baseX + i * dir, 7, s, bodyColor, xOffset);
            }
            else if (currentTrick == PetTrick.Paw)
            {
                DrawAwakePet(g, s, bodyColor, darkColor, xOffset);

                FillPixel(g, baseX + 2 * dir, 6, s, bodyColor, xOffset);
                FillPixel(g, baseX + 2 * dir, 5, s, bodyColor, xOffset);
                FillPixel(g, baseX + 1 * dir, 4, s, bodyColor, xOffset);
            }
            else if (currentTrick == PetTrick.Flip)
            {
                int rotation = (trickFrame / 5) % 4;
                if (rotation == 0 || rotation == 2)
                    DrawAwakePet(g, s, bodyColor, darkColor, xOffset);
                else
                {
                    for (int i = 3; i <= 6; i++)
                        FillPixel(g, baseX + i * dir, 4, s, bodyColor, xOffset);
                    for (int i = 2; i <= 7; i++)
                        FillPixel(g, baseX + i * dir, 5, s, bodyColor, xOffset);
                    for (int i = 3; i <= 6; i++)
                        FillPixel(g, baseX + i * dir, 6, s, bodyColor, xOffset);
                }
            }
        }

        private void DrawAwakePet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int dir = facingDirection;
            int baseX = 2;

            FillPixel(g, baseX + 2 * dir, 1, s, bodyColor, xOffset);
            FillPixel(g, baseX + 3 * dir, 0, s, bodyColor, xOffset);
            FillPixel(g, baseX + 3 * dir, 1, s, bodyColor, xOffset);
            FillPixel(g, baseX + 7 * dir, 1, s, bodyColor, xOffset);
            FillPixel(g, baseX + 6 * dir, 0, s, bodyColor, xOffset);
            FillPixel(g, baseX + 6 * dir, 1, s, bodyColor, xOffset);

            FillPixel(g, baseX + 3 * dir, 1, s, Color.FromArgb(255, 182, 193), xOffset);
            FillPixel(g, baseX + 6 * dir, 1, s, Color.FromArgb(255, 182, 193), xOffset);

            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 2, s, bodyColor, xOffset);
            for (int i = 1; i <= 8; i++)
                FillPixel(g, baseX + i * dir, 3, s, bodyColor, xOffset);
            for (int i = 1; i <= 8; i++)
                FillPixel(g, baseX + i * dir, 4, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 5, s, bodyColor, xOffset);

            FillPixel(g, baseX + 1 * dir, 4, s, Color.White, xOffset);
            FillPixel(g, baseX + 8 * dir, 4, s, Color.White, xOffset);

            bool blinking = animationFrame % 80 < 4;
            if (!blinking)
            {
                if (currentState == PetState.Sad)
                {
                    FillPixel(g, baseX + 3 * dir, 3, s, Color.Black, xOffset);
                    FillPixel(g, baseX + 6 * dir, 3, s, Color.Black, xOffset);
                    FillPixel(g, baseX + 3 * dir, 4, s, Color.Black, xOffset);
                    FillPixel(g, baseX + 6 * dir, 4, s, Color.Black, xOffset);
                }
                else
                {
                    FillPixel(g, baseX + 3 * dir, 3, s, Color.White, xOffset);
                    FillPixel(g, baseX + 6 * dir, 3, s, Color.White, xOffset);
                    FillPixel(g, baseX + 3 * dir, 4, s, Color.FromArgb(100, 200, 255), xOffset);
                    FillPixel(g, baseX + 6 * dir, 4, s, Color.FromArgb(100, 200, 255), xOffset);
                    FillPixel(g, baseX + 3 * dir, 3, s, Color.Black, xOffset);
                    FillPixel(g, baseX + 6 * dir, 3, s, Color.Black, xOffset);
                }
            }
            else
            {
                FillPixel(g, baseX + 3 * dir, 4, s, Color.Black, xOffset);
                FillPixel(g, baseX + 6 * dir, 4, s, Color.Black, xOffset);
            }

            FillPixel(g, baseX + 4 * dir, 4, s, Color.FromArgb(255, 105, 180), xOffset);
            FillPixel(g, baseX + 5 * dir, 4, s, Color.FromArgb(255, 105, 180), xOffset);

            FillPixel(g, baseX + 0 * dir, 4, s, Color.Black, xOffset);
            FillPixel(g, baseX + 9 * dir, 4, s, Color.Black, xOffset);

            if (currentState == PetState.Happy || currentState == PetState.Eating ||
                currentState == PetState.Playing || currentState == PetState.FollowingMouse ||
                currentState == PetState.ChasingBall)
            {
                FillPixel(g, baseX + 3 * dir, 5, s, Color.FromArgb(200, 0, 0), xOffset);
                FillPixel(g, baseX + 4 * dir, 5, s, Color.FromArgb(200, 0, 0), xOffset);
                FillPixel(g, baseX + 5 * dir, 5, s, Color.FromArgb(200, 0, 0), xOffset);
                FillPixel(g, baseX + 6 * dir, 5, s, Color.FromArgb(200, 0, 0), xOffset);
            }
            else if (currentState == PetState.Sad)
            {
                FillPixel(g, baseX + 4 * dir, 5, s, Color.FromArgb(100, 100, 100), xOffset);
                FillPixel(g, baseX + 5 * dir, 5, s, Color.FromArgb(100, 100, 100), xOffset);
            }

            for (int i = 3; i <= 6; i++)
                FillPixel(g, baseX + i * dir, 6, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 7, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 8, s, bodyColor, xOffset);
            for (int i = 3; i <= 6; i++)
                FillPixel(g, baseX + i * dir, 9, s, bodyColor, xOffset);

            FillPixel(g, baseX + 4 * dir, 7, s, Color.White, xOffset);
            FillPixel(g, baseX + 5 * dir, 7, s, Color.White, xOffset);
            FillPixel(g, baseX + 3 * dir, 8, s, Color.White, xOffset);
            FillPixel(g, baseX + 4 * dir, 8, s, Color.White, xOffset);
            FillPixel(g, baseX + 5 * dir, 8, s, Color.White, xOffset);
            FillPixel(g, baseX + 6 * dir, 8, s, Color.White, xOffset);

            bool legFrame = (animationFrame / 8) % 2 == 0;
            if (currentState == PetState.Walking || currentState == PetState.Playing ||
                currentState == PetState.FollowingMouse || currentState == PetState.ChasingBall)
            {
                if (legFrame)
                {
                    FillPixel(g, baseX + 3 * dir, 10, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 3 * dir, 11, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 5 * dir, 10, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 6 * dir, 10, s, bodyColor, xOffset);
                }
                else
                {
                    FillPixel(g, baseX + 6 * dir, 10, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 6 * dir, 11, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 4 * dir, 10, s, bodyColor, xOffset);
                    FillPixel(g, baseX + 3 * dir, 10, s, bodyColor, xOffset);
                }
            }
            else
            {
                FillPixel(g, baseX + 3 * dir, 10, s, bodyColor, xOffset);
                FillPixel(g, baseX + 4 * dir, 10, s, bodyColor, xOffset);
                FillPixel(g, baseX + 5 * dir, 10, s, bodyColor, xOffset);
                FillPixel(g, baseX + 6 * dir, 10, s, bodyColor, xOffset);

                FillPixel(g, baseX + 3 * dir, 11, s, Color.FromArgb(255, 182, 193), xOffset);
                FillPixel(g, baseX + 6 * dir, 11, s, Color.FromArgb(255, 182, 193), xOffset);
            }

            int tailBase = 7;
            if (happiness > 50 && currentState != PetState.Sad)
            {
                int tailWag = (animationFrame / 6) % 5 - 2;
                FillPixel(g, baseX + (7 + tailWag) * dir, tailBase, s, bodyColor, xOffset);
                FillPixel(g, baseX + (8 + tailWag) * dir, tailBase - 1, s, bodyColor, xOffset);
                FillPixel(g, baseX + (8 + tailWag) * dir, tailBase - 2, s, bodyColor, xOffset);
                FillPixel(g, baseX + (9 + tailWag) * dir, tailBase - 3, s, bodyColor, xOffset);
            }
            else
            {
                FillPixel(g, baseX + 7 * dir, tailBase + 1, s, bodyColor, xOffset);
                FillPixel(g, baseX + 8 * dir, tailBase + 1, s, bodyColor, xOffset);
                FillPixel(g, baseX + 9 * dir, tailBase + 2, s, bodyColor, xOffset);
            }

            if (happiness > 80 && animationFrame % 30 < 15)
            {
                using (Font font = new Font("Arial", 12, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 20, 147)))
                {
                    g.DrawString("?", font, brush, 50, 0);
                }
            }
        }

        private void DrawStretchingPet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int baseX = 2;
            int dir = facingDirection;
            int stretchOffset = (animationFrame / 5) % 3;

            FillPixel(g, baseX + 2 * dir, 1, s, bodyColor, xOffset);
            FillPixel(g, baseX + 7 * dir, 1, s, bodyColor, xOffset);

            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 2, s, bodyColor, xOffset);
            for (int i = 1; i <= 8; i++)
                FillPixel(g, baseX + i * dir, 3, s, bodyColor, xOffset);

            FillPixel(g, baseX + 3 * dir, 3, s, Color.Black, xOffset);
            FillPixel(g, baseX + 6 * dir, 3, s, Color.Black, xOffset);

            FillPixel(g, baseX + 4 * dir, 4, s, Color.Red, xOffset);
            FillPixel(g, baseX + 5 * dir, 4, s, Color.Red, xOffset);

            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 4 + stretchOffset, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 5 + stretchOffset, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 6 + stretchOffset, s, bodyColor, xOffset);

            FillPixel(g, baseX + 2 * dir, 7 + stretchOffset, s, bodyColor, xOffset);
            FillPixel(g, baseX + 3 * dir, 8 + stretchOffset, s, bodyColor, xOffset);
            FillPixel(g, baseX + 6 * dir, 7 + stretchOffset, s, bodyColor, xOffset);
            FillPixel(g, baseX + 7 * dir, 8 + stretchOffset, s, bodyColor, xOffset);
        }

        private void DrawScratchingPet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int baseX = 2;
            int dir = facingDirection;
            bool scratchFrame = (animationFrame / 3) % 2 == 0;

            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 2, s, bodyColor, xOffset);
            for (int i = 1; i <= 8; i++)
                FillPixel(g, baseX + i * dir, 3, s, bodyColor, xOffset);

            FillPixel(g, baseX + 2 * dir, 1, s, bodyColor, xOffset);
            FillPixel(g, baseX + 7 * dir, 1, s, bodyColor, xOffset);

            FillPixel(g, baseX + 3 * dir, 3, s, Color.Black, xOffset);
            FillPixel(g, baseX + 6 * dir, 3, s, Color.Black, xOffset);

            for (int i = 3; i <= 6; i++)
                FillPixel(g, baseX + i * dir, 4, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 5, s, bodyColor, xOffset);

            if (scratchFrame)
            {
                FillPixel(g, baseX + 7 * dir, 3, s, bodyColor, xOffset);
                FillPixel(g, baseX + 8 * dir, 2, s, bodyColor, xOffset);
            }
            else
            {
                FillPixel(g, baseX + 7 * dir, 4, s, bodyColor, xOffset);
                FillPixel(g, baseX + 8 * dir, 3, s, bodyColor, xOffset);
            }

            FillPixel(g, baseX + 3 * dir, 6, s, bodyColor, xOffset);
            FillPixel(g, baseX + 5 * dir, 6, s, bodyColor, xOffset);
        }

        private void DrawGroomingPet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int baseX = 2;
            int dir = facingDirection;

            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 3, s, bodyColor, xOffset);
            for (int i = 1; i <= 8; i++)
                FillPixel(g, baseX + i * dir, 4, s, bodyColor, xOffset);

            FillPixel(g, baseX + 2 * dir, 2, s, bodyColor, xOffset);
            FillPixel(g, baseX + 7 * dir, 2, s, bodyColor, xOffset);

            FillPixel(g, baseX + 3 * dir, 4, s, Color.Black, xOffset);
            FillPixel(g, baseX + 6 * dir, 4, s, Color.Black, xOffset);

            for (int i = 3; i <= 6; i++)
                FillPixel(g, baseX + i * dir, 5, s, bodyColor, xOffset);
            for (int i = 2; i <= 7; i++)
                FillPixel(g, baseX + i * dir, 6, s, bodyColor, xOffset);
            for (int i = 3; i <= 6; i++)
                FillPixel(g, baseX + i * dir, 7, s, bodyColor, xOffset);

            if ((animationFrame / 4) % 2 == 0)
            {
                FillPixel(g, baseX + 4 * dir, 5, s, Color.Pink, xOffset);
            }

            FillPixel(g, baseX + 3 * dir, 8, s, bodyColor, xOffset);
            FillPixel(g, baseX + 6 * dir, 8, s, bodyColor, xOffset);
        }

        private void DrawSleepingPet(Graphics g, int s, Color bodyColor, Color darkColor, int xOffset)
        {
            int baseX = 3;

            for (int i = 2; i <= 8; i++)
                FillPixel(g, baseX + i, 8, s, bodyColor, xOffset);
            for (int i = 1; i <= 9; i++)
                FillPixel(g, baseX + i, 9, s, bodyColor, xOffset);
            for (int i = 2; i <= 8; i++)
                FillPixel(g, baseX + i, 10, s, bodyColor, xOffset);

            for (int i = 2; i <= 5; i++)
                FillPixel(g, baseX + i, 6, s, bodyColor, xOffset);
            for (int i = 1; i <= 6; i++)
                FillPixel(g, baseX + i, 7, s, bodyColor, xOffset);

            FillPixel(g, baseX + 1, 5, s, darkColor, xOffset);
            FillPixel(g, baseX + 5, 5, s, darkColor, xOffset);

            FillPixel(g, baseX + 2, 7, s, Color.Black, xOffset);
            FillPixel(g, baseX + 5, 7, s, Color.Black, xOffset);

            if (animationFrame % 40 < 20)
            {
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.Blue))
                {
                    g.DrawString("Zzz", font, brush, 70, 10);
                }
            }
        }

        private void FillPixel(Graphics g, int x, int y, int size, Color color, int xOffset)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                int actualX = facingDirection == 1 ? x * size : this.Width - (x * size) - size;
                g.FillRectangle(brush, actualX, y * size, size, size);
            }
        }

        private void PlaySound(string soundType)
        {
            try
            {
                if (soundType == "meow")
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Console.Beep(800, 100);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(600, 150);
                    });
                }
                else if (soundType == "purr")
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Console.Beep(400, 80);
                            System.Threading.Thread.Sleep(30);
                        }
                    });
                }
            }
            catch { }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                trayMenu.Show(Cursor.Position);
            }
        }

        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                happiness = Math.Min(100, happiness + 10);
                currentState = PetState.Playing;
                PlaySound("purr");
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => { if (currentState == PetState.Playing) currentState = PetState.Idle; }));
                });
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStart = e.Location;
                happiness = Math.Min(100, happiness + 3);
                if (currentState != PetState.Playing && currentState != PetState.ChasingBall)
                {
                    currentState = PetState.Happy;
                    this.Invalidate();
                }
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.Location;
                newLocation.X += e.X - dragStart.X;
                newLocation.Y += e.Y - dragStart.Y;
                this.Location = newLocation;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void Form1_MouseEnter(object sender, EventArgs e)
        {
            if (happiness > 40 && random.Next(100) < 60 && currentState != PetState.ChasingBall)
            {
                isFollowingMouse = true;
                currentState = PetState.FollowingMouse;
            }
        }

        private void FeedPet(object sender, EventArgs e)
        {
            hunger = Math.Min(100, hunger + 35);
            happiness = Math.Min(100, happiness + 10);
            energy = Math.Min(100, energy + 5);
            currentState = PetState.Eating;
            totalFeeds++;
            PlaySound("purr");

            if (!achievements["first_feed"])
            {
                achievements["first_feed"] = true;
                ShowAchievementUnlocked("Ýlk Besleme", "Pet'ini ilk kez besledin! ??");
            }

            System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => { currentState = PetState.Idle; }));
                else
                    currentState = PetState.Idle;
            });

            trayIcon.ShowBalloonTip(2000, "Afiyet olsun! ??",
                $"{petName} doydu! Açlýk: {hunger}%",
                ToolTipIcon.Info);
            UpdateTrayMenu();
            this.Invalidate();
        }

        private void PlayWithPet(object sender, EventArgs e)
        {
            happiness = Math.Min(100, happiness + 30);
            energy = Math.Max(0, energy - 10);
            currentState = PetState.Playing;
            totalPlays++;
            PlaySound("meow");

            if (!achievements["first_play"])
            {
                achievements["first_play"] = true;
                ShowAchievementUnlocked("Ýlk Oyun", "Pet'inle ilk kez oynadýn! ??");
            }

            System.Threading.Tasks.Task.Delay(3000).ContinueWith(t =>
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => { currentState = PetState.Idle; }));
                else
                    currentState = PetState.Idle;
            });

            trayIcon.ShowBalloonTip(2000, "Yaþasýn! ??",
                $"{petName} çok mutlu! Mutluluk: {happiness}%",
                ToolTipIcon.Info);
            UpdateTrayMenu();
            this.Invalidate();
        }

        private void SleepPet(object sender, EventArgs e)
        {
            currentState = PetState.Sleeping;
            velocityX = 0;
            velocityY = 0;
            sleepCounter = 0;
            trayIcon.ShowBalloonTip(2000, "Ýyi uykular! ??",
                $"{petName} dinleniyor...",
                ToolTipIcon.Info);
            this.Invalidate();
        }

        private void ShowStatsMessage()
        {
            string condition = "Mükemmel";
            if (hunger < 50 || happiness < 50 || energy < 50)
                condition = "Ýyi";
            if (hunger < 30 || happiness < 30 || energy < 30)
                condition = "Orta";
            if (hunger < 20 || happiness < 20 || energy < 20)
                condition = "Kötü";

            TimeSpan timeLived = DateTime.Now - birthDate;
            int completed = achievements.Values.Count(x => x);
            string accessoryText = currentAccessory != null ? currentAccessory.Name : "Yok";
            string weatherText = currentWeather == WeatherType.Sunny ? "Güneþli ??" :
                                currentWeather == WeatherType.Rainy ? "Yaðmurlu ???" :
                                currentWeather == WeatherType.Snowy ? "Karlý ??" : "Bulutlu ??";

            MessageBox.Show(
                $"?? {petName} - Pet Ýstatistikleri\n\n" +
                $"Açlýk: {hunger}%\n" +
                $"Mutluluk: {happiness}%\n" +
                $"Enerji: {energy}%\n" +
                $"Yaþ: {GetAgeString()}\n" +
                $"Yaþam Süresi: {timeLived.Days} gün {timeLived.Hours} saat\n" +
                $"Aksesuar: {accessoryText}\n" +
                $"Hava Durumu: {weatherText}\n\n" +
                $"?? Ýstatistikler:\n" +
                $"Toplam Besleme: {totalFeeds}\n" +
                $"Toplam Oyun: {totalPlays}\n" +
                $"Yakalanan Top: {ballsCaught}\n" +
                $"Baþarýmlar: {completed}/6\n\n" +
                $"Durum: {condition}\n\n" +
                $"?? Tuþ Kontrolleri:\n" +
                $"I = Bakým Menüsü\n" +
                $"B = Top Fýrlat\n" +
                $"S = Otur | P = Pati Ver | T = Takla At\n" +
                $"A = Aksesuar | H = Baþarýmlar",
                "Pet Simulator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateTrayMenu()
        {
            trayMenu.Items[0].Text = $"?? {petName}";
            trayMenu.Items[6].Text = $"Açlýk: {hunger}%";
            trayMenu.Items[7].Text = $"Mutluluk: {happiness}%";
            trayMenu.Items[8].Text = $"Enerji: {energy}%";
            trayMenu.Items[9].Text = $"Yaþ: {GetAgeString()}";
        }

        private void SavePetData()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PetSimulator");
                Directory.CreateDirectory(appData);
                string filePath = Path.Combine(appData, "pet_data.txt");

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(petName);
                    writer.WriteLine(hunger);
                    writer.WriteLine(happiness);
                    writer.WriteLine(energy);
                    writer.WriteLine(age);
                    writer.WriteLine(birthDate.ToString("o"));
                    writer.WriteLine(totalFeeds);
                    writer.WriteLine(totalPlays);
                    writer.WriteLine(ballsCaught);
                    writer.WriteLine(string.Join(",", achievements.Select(x => x.Key + ":" + x.Value)));
                    writer.WriteLine(currentAccessory?.Name ?? "");
                }
            }
            catch { }
        }

        private void LoadPetData()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PetSimulator");
                string filePath = Path.Combine(appData, "pet_data.txt");

                if (File.Exists(filePath))
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        petName = reader.ReadLine();
                        hunger = int.Parse(reader.ReadLine());
                        happiness = int.Parse(reader.ReadLine());
                        energy = int.Parse(reader.ReadLine());
                        age = int.Parse(reader.ReadLine());
                        birthDate = DateTime.Parse(reader.ReadLine());
                        totalFeeds = int.Parse(reader.ReadLine());
                        totalPlays = int.Parse(reader.ReadLine());
                        ballsCaught = int.Parse(reader.ReadLine());

                        string achString = reader.ReadLine();
                        if (!string.IsNullOrEmpty(achString))
                        {
                            foreach (var pair in achString.Split(','))
                            {
                                var parts = pair.Split(':');
                                if (parts.Length == 2)
                                {
                                    achievements[parts[0]] = bool.Parse(parts[1]);
                                }
                            }
                        }

                        string accName = reader.ReadLine();
                        if (!string.IsNullOrEmpty(accName) && ownedAccessories != null)
                        {
                            currentAccessory = ownedAccessories.FirstOrDefault(x => x.Name == accName);
                        }
                    }
                }
                else
                {
                    birthDate = DateTime.Now;
                }
            }
            catch
            {
                birthDate = DateTime.Now;
                petName = "Kedi";
            }
        }

        private void AskPetName()
        {
            using (Form nameForm = new Form())
            {
                nameForm.Text = "Pet'ine Ýsim Ver! ??";
                nameForm.Size = new Size(350, 180);
                nameForm.StartPosition = FormStartPosition.CenterScreen;
                nameForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                nameForm.MaximizeBox = false;
                nameForm.MinimizeBox = false;

                Label label = new Label();
                label.Text = "Sevimli pet'ine bir isim ver:";
                label.Location = new Point(20, 20);
                label.Size = new Size(300, 20);
                label.Font = new Font("Arial", 10, FontStyle.Bold);

                TextBox textBox = new TextBox();
                textBox.Location = new Point(20, 50);
                textBox.Size = new Size(290, 25);
                textBox.Font = new Font("Arial", 11);
                textBox.Text = "Minnoþ";

                Button okButton = new Button();
                okButton.Text = "Tamam! ??";
                okButton.Location = new Point(110, 90);
                okButton.Size = new Size(120, 35);
                okButton.Font = new Font("Arial", 10, FontStyle.Bold);
                okButton.Click += (s, ev) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        petName = textBox.Text.Trim();
                        SavePetData();
                        UpdateTrayMenu();
                        nameForm.DialogResult = DialogResult.OK;
                        nameForm.Close();
                        PlaySound("meow");
                        trayIcon.ShowBalloonTip(3000, $"Merhaba {petName}! ??",
                            "Pet'in artýk hazýr! Tuþ kontrolleri:\nI=Menü B=Top S=Otur P=Pati T=Takla A=Aksesuar H=Baþarýmlar",
                            ToolTipIcon.Info);
                    }
                };

                nameForm.Controls.Add(label);
                nameForm.Controls.Add(textBox);
                nameForm.Controls.Add(okButton);
                nameForm.AcceptButton = okButton;

                nameForm.ShowDialog();
            }
        }

        private void ShowFeedMenu()
        {
            using (Form menuForm = new Form())
            {
                menuForm.Text = $"{petName} - Bakým Menüsü";
                menuForm.Size = new Size(350, 280);
                menuForm.StartPosition = FormStartPosition.CenterScreen;
                menuForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                menuForm.MaximizeBox = false;
                menuForm.MinimizeBox = false;
                menuForm.BackColor = Color.FromArgb(255, 250, 240);

                Label titleLabel = new Label();
                titleLabel.Text = $"?? {petName} ile ilgilen";
                titleLabel.Font = new Font("Arial", 12, FontStyle.Bold);
                titleLabel.Location = new Point(20, 15);
                titleLabel.Size = new Size(300, 25);
                titleLabel.TextAlign = ContentAlignment.MiddleCenter;

                Label statsLabel = new Label();
                statsLabel.Text = $"Açlýk: {hunger}%  |  Mutluluk: {happiness}%  |  Enerji: {energy}%";
                statsLabel.Font = new Font("Arial", 9);
                statsLabel.Location = new Point(20, 45);
                statsLabel.Size = new Size(300, 20);
                statsLabel.TextAlign = ContentAlignment.MiddleCenter;

                Button feedButton = new Button();
                feedButton.Text = "?? Yem Ver (+35 Açlýk)";
                feedButton.Font = new Font("Arial", 10, FontStyle.Bold);
                feedButton.Location = new Point(40, 80);
                feedButton.Size = new Size(260, 40);
                feedButton.BackColor = Color.FromArgb(144, 238, 144);
                feedButton.FlatStyle = FlatStyle.Flat;
                feedButton.Click += (s, ev) => { FeedPet(s, ev); menuForm.Close(); };

                Button playButton = new Button();
                playButton.Text = "?? Oyna (+30 Mutluluk)";
                playButton.Font = new Font("Arial", 10, FontStyle.Bold);
                playButton.Location = new Point(40, 130);
                playButton.Size = new Size(260, 40);
                playButton.BackColor = Color.FromArgb(255, 228, 181);
                playButton.FlatStyle = FlatStyle.Flat;
                playButton.Click += (s, ev) => { PlayWithPet(s, ev); menuForm.Close(); };

                Button sleepButton = new Button();
                sleepButton.Text = "?? Uyu (+Enerji)";
                sleepButton.Font = new Font("Arial", 10, FontStyle.Bold);
                sleepButton.Location = new Point(40, 180);
                sleepButton.Size = new Size(260, 40);
                sleepButton.BackColor = Color.FromArgb(173, 216, 230);
                sleepButton.FlatStyle = FlatStyle.Flat;
                sleepButton.Click += (s, ev) => { SleepPet(s, ev); menuForm.Close(); };

                menuForm.Controls.Add(titleLabel);
                menuForm.Controls.Add(statsLabel);
                menuForm.Controls.Add(feedButton);
                menuForm.Controls.Add(playButton);
                menuForm.Controls.Add(sleepButton);

                menuForm.ShowDialog();
            }
        }

        private void ShowAccessoryMenu()
        {
            using (Form accForm = new Form())
            {
                accForm.Text = $"{petName} - Aksesuarlar";
                accForm.Size = new Size(350, 350);
                accForm.StartPosition = FormStartPosition.CenterScreen;
                accForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                accForm.MaximizeBox = false;
                accForm.MinimizeBox = false;
                accForm.BackColor = Color.FromArgb(255, 250, 240);

                Label titleLabel = new Label();
                titleLabel.Text = "?? Aksesuar Seç";
                titleLabel.Font = new Font("Arial", 12, FontStyle.Bold);
                titleLabel.Location = new Point(20, 15);
                titleLabel.Size = new Size(300, 25);
                titleLabel.TextAlign = ContentAlignment.MiddleCenter;
                accForm.Controls.Add(titleLabel);

                int yPos = 60;
                Button noneButton = new Button();
                noneButton.Text = "? Hiçbiri";
                noneButton.Font = new Font("Arial", 10);
                noneButton.Location = new Point(40, yPos);
                noneButton.Size = new Size(260, 40);
                noneButton.BackColor = Color.LightGray;
                noneButton.Click += (s, ev) => { currentAccessory = null; accForm.Close(); };
                accForm.Controls.Add(noneButton);
                yPos += 50;

                foreach (var acc in ownedAccessories)
                {
                    Button btn = new Button();
                    btn.Text = $"{acc.Icon} {acc.Name}";
                    btn.Font = new Font("Arial", 10, FontStyle.Bold);
                    btn.Location = new Point(40, yPos);
                    btn.Size = new Size(260, 40);
                    btn.BackColor = Color.FromArgb(255, 228, 196);
                    btn.Tag = acc;
                    btn.Click += (s, ev) =>
                    {
                        currentAccessory = (Accessory)((Button)s).Tag;
                        trayIcon.ShowBalloonTip(1500, "Aksesuar Takýldý! ??",
                            $"{petName} artýk {currentAccessory.Name} takýyor!", ToolTipIcon.Info);
                        accForm.Close();
                    };
                    accForm.Controls.Add(btn);
                    yPos += 50;
                }

                accForm.ShowDialog();
            }
        }

        private void ShowAchievements()
        {
            string achText = "?? BAÞARIMLAR\n\n";

            achText += achievements["first_feed"] ? "?" : "?";
            achText += " Ýlk Besleme\n";

            achText += achievements["first_play"] ? "?" : "?";
            achText += " Ýlk Oyun\n";

            achText += achievements["ball_master"] ? "?" : "?";
            achText += $" Top Ustasý (10/10) - Yakalanan: {ballsCaught}\n";

            achText += achievements["week_old"] ? "?" : "?";
            achText += " 1 Haftalýk\n";

            achText += achievements["happy_pet"] ? "?" : "?";
            achText += " Çok Mutlu Pet\n";

            achText += achievements["trick_master"] ? "?" : "?";
            achText += " Numara Ustasý\n";

            int completed = achievements.Values.Count(x => x);
            achText += $"\n?? Tamamlanan: {completed}/6";

            MessageBox.Show(achText, $"{petName} - Baþarýmlar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CheckAchievement(string achName)
        {
            if (achievements.ContainsKey(achName) && !achievements[achName])
            {
                if (achName == "ball_master" && ballsCaught >= 10)
                {
                    achievements[achName] = true;
                    ShowAchievementUnlocked("Top Ustasý", "10 top yakaladýn! ??");
                }
            }
        }

        private void ShowAchievementUnlocked(string name, string desc)
        {
            trayIcon.ShowBalloonTip(3000, $"?? Baþarým Kazanýldý: {name}", desc, ToolTipIcon.Info);
            PlaySound("meow");
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (currentState == PetState.Idle || currentState == PetState.Walking)
            {
                int randomAction = random.Next(100);
                if (randomAction < 15)
                {
                    currentState = PetState.Stretching;
                    PlaySound("meow");
                    System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => { if (currentState == PetState.Stretching) currentState = PetState.Idle; }));
                    });
                }
                else if (randomAction < 30)
                {
                    currentState = PetState.Scratching;
                    System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => { if (currentState == PetState.Scratching) currentState = PetState.Idle; }));
                    });
                }
                else if (randomAction < 40)
                {
                    currentState = PetState.Grooming;
                    System.Threading.Tasks.Task.Delay(2500).ContinueWith(t =>
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => { if (currentState == PetState.Grooming) currentState = PetState.Idle; }));
                    });
                }
            }
        }

        private void InitializeForm()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(120, 120);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.Text = "Pet Simulator";
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = System.Drawing.Color.Lime;
            this.TransparencyKey = System.Drawing.Color.Lime;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width / 2,
                                     Screen.PrimaryScreen.WorkingArea.Height / 2);

            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form1_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseUp);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDoubleClick);
            this.MouseEnter += new EventHandler(this.Form1_MouseEnter);

            this.ResumeLayout(false);
        }

        private void InitializePet()
        {
            random = new Random();

            moveTimer = new System.Windows.Forms.Timer();
            moveTimer.Interval = 50;
            moveTimer.Tick += MoveTimer_Tick;
            moveTimer.Start();

            statsTimer = new System.Windows.Forms.Timer();
            statsTimer.Interval = 3000;
            statsTimer.Tick += StatsTimer_Tick;
            statsTimer.Start();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 10000;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            ballTimer = new System.Windows.Forms.Timer();
            ballTimer.Interval = 30;
            ballTimer.Tick += BallTimer_Tick;
        }

        private void BallTimer_Tick(object sender, EventArgs e)
        {
            if (!ballActive) return;

            ballPosition.X += ballVelocityX;
            ballPosition.Y += ballVelocityY;
            ballVelocityY += 1;

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;

            if (ballPosition.X < screen.Left || ballPosition.X > screen.Right - ballRadius * 2)
            {
                ballVelocityX = -ballVelocityX;
                ballBounceCount++;
            }

            if (ballPosition.Y > screen.Bottom - ballRadius * 2)
            {
                ballPosition.Y = screen.Bottom - ballRadius * 2;
                ballVelocityY = (int)(-ballVelocityY * 0.7f);
                ballBounceCount++;

                if (Math.Abs(ballVelocityY) < 2)
                {
                    ballActive = false;
                    ballTimer.Stop();
                    if (currentState == PetState.ChasingBall)
                        currentState = PetState.Idle;
                }
            }

            if (ballPosition.Y < screen.Top)
            {
                ballPosition.Y = screen.Top;
                ballVelocityY = -ballVelocityY;
            }

            int petCenterX = this.Left + this.Width / 2;
            int petCenterY = this.Top + this.Height / 2;
            double distance = Math.Sqrt(Math.Pow(petCenterX - ballPosition.X, 2) + Math.Pow(petCenterY - ballPosition.Y, 2));

            if (distance < 40)
            {
                ballActive = false;
                ballTimer.Stop();
                currentState = PetState.Happy;
                happiness = Math.Min(100, happiness + 15);
                ballsCaught++;
                CheckAchievement("ball_master");
                PlaySound("purr");
                trayIcon.ShowBalloonTip(1500, "Yakaladý! ??", $"{petName} topu yakaladý! Top: {ballsCaught}", ToolTipIcon.Info);

                System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                {
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => { if (currentState == PetState.Happy) currentState = PetState.Idle; }));
                });
            }

            this.Invalidate();
        }

        private void SpawnBall()
        {
            if (ballActive) return;

            ballActive = true;
            ballPosition = new Point(this.Left + 50, this.Top - 100);
            ballVelocityX = random.Next(-5, 6);
            ballVelocityY = 0;
            ballBounceCount = 0;
            ballColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));

            currentState = PetState.ChasingBall;
            ballTimer.Start();
            PlaySound("meow");
            trayIcon.ShowBalloonTip(1000, "Top Zamaný! ??", $"{petName} topu kovalýyor!", ToolTipIcon.Info);
        }

        private void PerformTrick(PetTrick trick)
        {
            if (currentState == PetState.DoingTrick) return;

            currentTrick = trick;
            currentState = PetState.DoingTrick;
            trickFrame = 0;
            happiness = Math.Min(100, happiness + 10);
            energy = Math.Max(0, energy - 5);

            string trickName = trick == PetTrick.Sit ? "oturdu" : trick == PetTrick.Paw ? "pati verdi" : "takla attý";
            trayIcon.ShowBalloonTip(1500, $"Aferin {petName}! ??", $"{petName} {trickName}!", ToolTipIcon.Info);
            PlaySound("meow");

            if (!achievements["trick_master"])
            {
                achievements["trick_master"] = true;
                ShowAchievementUnlocked("Numara Ustasý", "Ýlk numaraný yaptýn! ??");
            }

            System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => {
                        if (currentState == PetState.DoingTrick)
                        {
                            currentState = PetState.Idle;
                            currentTrick = PetTrick.None;
                        }
                    }));
            });
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add($"?? {petName}", null, null).Enabled = false;
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("?? Yem Ver", null, FeedPet);
            trayMenu.Items.Add("?? Oyna", null, PlayWithPet);
            trayMenu.Items.Add("?? Uyu", null, SleepPet);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add($"Açlýk: {hunger}%", null, null);
            trayMenu.Items.Add($"Mutluluk: {happiness}%", null, null);
            trayMenu.Items.Add($"Enerji: {energy}%", null, null);
            trayMenu.Items.Add($"Yaþ: {GetAgeString()}", null, null);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("?? Ýsim Deðiþtir", null, (s, ev) => { AskPetName(); });
            trayMenu.Items.Add("?? Ýstatistikler", null, (s, ev) => ShowStatsMessage());
            trayMenu.Items.Add("?? Baþarýmlar (H)", null, (s, ev) => ShowAchievements());
            trayMenu.Items.Add("? Çýkýþ", null, ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.Text = $"{petName} - Pet Simulator";
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
        }

        private void ExitApp(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                $"{petName}'den ayrýlmak istediðinize emin misiniz? ??",
                "Pet Simulator",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SavePetData();
                UnhookWindowsHookEx(hookId);
                trayIcon.Visible = false;
                Application.Exit();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, $"{petName} hala burada! ??",
                    "Pet'iniz sistem tepsisinde çalýþmaya devam ediyor.",
                    ToolTipIcon.Info);
            }
            else
            {
                SavePetData();
                UnhookWindowsHookEx(hookId);
            }
        }
    }
}