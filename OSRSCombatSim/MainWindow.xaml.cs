using OSRSCombatSim.Engine;
using OSRSCombatSim.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OSRSCombatSim {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private CombatEngine _engine;

        public MainWindow() {
            InitializeComponent();
            SetupCombatants();
            SetupEngine();

            AttackStyleSelector.SelectionChanged += AttackStyleSelector_SelectionChanged;
            this.KeyDown += MainWindow_KeyDown;  // Add this line
        }

        private void AnimateAttack(AttackType type, bool isPlayer) {
            // Pick which UI element moves
            var target = isPlayer ? PlayerSquare : MonsterSquare;

            // Prepare transform for animation
            var transform = new TranslateTransform();
            target.RenderTransform = transform;

            // Decide movement based on attack type
            double offsetX = 0;
            double offsetY = 0;

            switch (type) {
                case AttackType.Stab:
                    offsetX = isPlayer ? 20 : -20;
                    break;

                case AttackType.Slash:
                    offsetX = isPlayer ? 15 : -15;
                    offsetY = -10; // little upward swing
                    break;

                case AttackType.Crush:
                    offsetX = isPlayer ? 10 : -10;
                    offsetY = 10; // downward smash
                    break;

                case AttackType.Ranged:
                    AnimateProjectile(isPlayer);
                    return; // skip melee motion
            }

            var forwardX = new DoubleAnimation(0, offsetX, TimeSpan.FromMilliseconds(100));
            var backX = new DoubleAnimation(offsetX, 0, TimeSpan.FromMilliseconds(100)) {
                BeginTime = TimeSpan.FromMilliseconds(100)
            };

            var forwardY = new DoubleAnimation(0, offsetY, TimeSpan.FromMilliseconds(100));
            var backY = new DoubleAnimation(offsetY, 0, TimeSpan.FromMilliseconds(100)) {
                BeginTime = TimeSpan.FromMilliseconds(100)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(forwardX, target);
            Storyboard.SetTargetProperty(forwardX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTarget(backX, target);
            Storyboard.SetTargetProperty(backX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            Storyboard.SetTarget(forwardY, target);
            Storyboard.SetTargetProperty(forwardY, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            Storyboard.SetTarget(backY, target);
            Storyboard.SetTargetProperty(backY, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            storyboard.Children.Add(forwardX);
            storyboard.Children.Add(backX);
            storyboard.Children.Add(forwardY);
            storyboard.Children.Add(backY);

            storyboard.Begin();
        }

        private void AnimateProjectile(bool fromPlayer) {
            var arrow = new Rectangle {
                Width = 10,
                Height = 2,
                Fill = Brushes.Gold
            };

            GameCanvas.Children.Add(arrow);

            // Get positions
            double startX = fromPlayer ? Canvas.GetLeft(PlayerSquare) + PlayerSquare.Width : Canvas.GetLeft(MonsterSquare);
            double startY = Canvas.GetTop(PlayerSquare) + PlayerSquare.Height / 2;

            double endX = fromPlayer ? Canvas.GetLeft(MonsterSquare) : Canvas.GetLeft(PlayerSquare);
            double endY = Canvas.GetTop(MonsterSquare) + MonsterSquare.Height / 2;

            var animX = new DoubleAnimation(startX, endX, TimeSpan.FromMilliseconds(200));
            var animY = new DoubleAnimation(startY, endY, TimeSpan.FromMilliseconds(200));

            Storyboard.SetTarget(animX, arrow);
            Storyboard.SetTargetProperty(animX, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTarget(animY, arrow);
            Storyboard.SetTargetProperty(animY, new PropertyPath("(Canvas.Top)"));

            var sb = new Storyboard();
            sb.Children.Add(animX);
            sb.Children.Add(animY);

            sb.Completed += (s, e) => GameCanvas.Children.Remove(arrow);

            sb.Begin();
        }

        private void AttackStyleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_engine.Player == null) return; // replace with your actual player variable
            if (AttackStyleSelector.SelectedItem is ComboBoxItem selectedItem) {
                if (Enum.TryParse<CombatStyle>(selectedItem.Tag.ToString(), out var combatStyle)) {
                    _engine.Player.CombatStyle = combatStyle;
                    LogCombatRoll($"Player attack style set to {combatStyle}");
                }
            }
        }

        private void AutoBattleToggle_Checked(object sender, RoutedEventArgs e) {
            _engine.AutoBattleEnabled = true;
        }

        private void AutoBattleToggle_Unchecked(object sender, RoutedEventArgs e) {
            _engine.AutoBattleEnabled = false;
        }

        private void Engine_AttackOccurred(object? sender, CombatEventArgs e) {
            Dispatcher.Invoke(() => {
                AnimateAttack(e.AttackType, e.Attacker == _engine.Player);
                UpdateUI();
                ShowHitSplat(e.Defender, e.Damage, e.HitSuccess);

                // Example: Log a sample combat roll message (adjust with your actual roll variables)
                LogCombatRoll($"{e.Attacker.Name} attacked {e.Defender.Name} with {e.AttackType} - Damage: {e.Damage}, Hit Success: {e.HitSuccess}");
            });
        }

        private void Engine_CombatEnded(object? sender, EventArgs e) {
            Dispatcher.Invoke(() => {
                UpdateUI();
                string winner = _engine.Player.IsDead ? _engine.Monster.Name : _engine.Player.Name;
                MessageBox.Show($"{winner} wins!", "Combat Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                _engine.Stop();
            });
        }

        private void LogCombatRoll(string message) {
            CombatLogTextBox.AppendText(message + Environment.NewLine);
            CombatLogTextBox.ScrollToEnd();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                Application.Current.Shutdown();
            }
        }

        private void MonsterSquare_MouseDown(object sender, MouseButtonEventArgs e) {
            if (!_engine.AutoBattleEnabled)
                _engine.ManualPlayerAttack();
        }

        private void SetupCombatants() {
            var player = new Combatant("Player", 100) {
                AttackLevel = 75,
                StrengthLevel = 70,
                DefenceLevel = 70,
                RangedLevel = 60,
                MeleeDefenceBonus = 60,
                RangedDefenceBonus = 40,
                EquippedWeapon = new Weapon {
                    Name = "Rune Sword",
                    AttackType = AttackType.Slash,
                    SpeedTicks = 4,
                    AttackBonus = 50,
                    StrengthBonus = 40
                },
                CombatStyle = CombatStyle.Aggressive
            };

            var monster = new Combatant("Demonic Gorilla", 150) {
                AttackLevel = 85,
                StrengthLevel = 80,
                DefenceLevel = 75,
                RangedLevel = 30,
                MeleeDefenceBonus = 70,
                RangedDefenceBonus = 20,
                EquippedWeapon = new Weapon {
                    Name = "Claws",
                    AttackType = AttackType.Slash,
                    SpeedTicks = 5,
                    AttackBonus = 45,
                    StrengthBonus = 35
                },
                CombatStyle = CombatStyle.Aggressive
            };

            _engine = new CombatEngine(player, monster);
        }

        private void SetupEngine() {
            _engine.AttackOccurred += Engine_AttackOccurred;
            _engine.CombatEnded += Engine_CombatEnded;
            UpdateUI();
            AutoBattleToggle.IsChecked = true; // now _engine is ready
        }

        private void ShowHitSplat(Combatant defender, int damage, bool hitSuccess) {
            if (!hitSuccess || damage == 0) return;

            var text = new TextBlock {
                Text = damage.ToString(),
                Foreground = Brushes.Yellow,
                FontWeight = FontWeights.Bold,
                FontSize = 24,
                Opacity = 1,
                IsHitTestVisible = false
            };

            // Position over defender's square
            Point startPoint = defender == _engine.Player
                ? PlayerHealthBar.TranslatePoint(new Point(PlayerHealthBar.ActualWidth / 2, -20), HitSplatCanvas)
                : MonsterHealthBar.TranslatePoint(new Point(MonsterHealthBar.ActualWidth / 2, -20), HitSplatCanvas);

            Canvas.SetLeft(text, startPoint.X);
            Canvas.SetTop(text, startPoint.Y);
            HitSplatCanvas.Children.Add(text);

            // Animate upwards and fade out
            var animY = new DoubleAnimation {
                From = startPoint.Y,
                To = startPoint.Y - 50,
                Duration = TimeSpan.FromSeconds(1)
            };

            var animOpacity = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1)
            };

            animOpacity.Completed += (s, e) => HitSplatCanvas.Children.Remove(text);

            text.BeginAnimation(Canvas.TopProperty, animY);
            text.BeginAnimation(OpacityProperty, animOpacity);
        }

        private void StartAutoBattleButton_Click(object sender, RoutedEventArgs e) {
            if (_engine == null) return;

            _engine.AutoBattleEnabled = true;
            _engine.Start();
            // Optionally disable button after starting
            StartAutoBattleButton.IsEnabled = false;
        }

        private void UpdateUI() {
            var player = _engine.Player;
            var monster = _engine.Monster;

            PlayerName.Text = player.Name;
            PlayerHealthBar.Maximum = player.MaxHitpoints;
            PlayerHealthBar.Value = player.Hitpoints;
            PlayerHPText.Text = $"{player.Hitpoints} / {player.MaxHitpoints}";

            MonsterName.Text = monster.Name;
            MonsterHealthBar.Maximum = monster.MaxHitpoints;
            MonsterHealthBar.Value = monster.Hitpoints;
            MonsterHPText.Text = $"{monster.Hitpoints} / {monster.MaxHitpoints}";
        }
    }
}