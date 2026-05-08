using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Rubik_s_Cube.Models;

namespace Rubik_s_Cube
{
    public partial class MainWindow : Window
    {
        private readonly RubiksCube _cube = new RubiksCube();
        private readonly Model3DGroup[,,] _cubies = new Model3DGroup[3, 3, 3];
        private readonly Transform3DGroup _scene = new Transform3DGroup();
        private readonly QuaternionRotation3D _sceneRotation = new QuaternionRotation3D(Quaternion.Identity);
        private AmbientLight _ambientLight = new AmbientLight(Colors.Black);
        private DirectionalLight _keyLight = new DirectionalLight(Colors.Black, new Vector3D(-1, -1, -2));
        private DirectionalLight _fillLight = new DirectionalLight(Colors.Black, new Vector3D(1, 1, 2));
        private DirectionalLight _rimLight = new DirectionalLight(Colors.Black, new Vector3D(-1, 1, 1));

        private Point _lastMouse;
        private bool _dragging;
        private bool _isAnimating;
        private bool _isSceneAnimating;
        private readonly Queue<string> _moveQueue = new Queue<string>();
        private bool _queueUsesMappedMoves = true;

        // Dictionary to map Input Keys -> Internal Cube Faces based on camera view
        private Dictionary<char, char> _orientationMap = new Dictionary<char, char>();

        private enum CubePerspective { Green, Blue, Red, Orange, White, Yellow }
        private CubePerspective _currentPerspective = CubePerspective.Green;

        // UI tracking
        private int _moveCount = 0;
        private DateTime _timerStart;
        private bool _timerRunning = false;
        private DispatcherTimer _uiTimer;

        // Customization settings
        private double _cubeGap = 0.02;
        private double _animationSpeedScale = 1.0;
        private byte _shininessLevel = 24;
        private double _shininessPower = 8;
        private bool _glossEnabled = true;
        private bool _rimLightEnabled = true;
        private byte _ambientLevel = 50;
        private byte _keyLevel = 255;
        private byte _fillLevel = 150;
        private byte _rimLevel = 90;
        private bool _settingsReady;

        public MainWindow()
        {
            InitializeComponent();
            BuildCube();
            UpdateCubeVisuals();

            // Timer for UI updates
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiTimer.Tick += (s, e) => UpdateTimerDisplay();

            // Initial setup
            SnapToCurrentPerspective();
            MoveInput.Focus();
            SyncSettingsControls();
            _settingsReady = true;
        }

        #region Cube Construction and Visuals
        private void BuildCube()
        {
            var root = new Model3DGroup();

            // Lighting
            _ambientLight = new AmbientLight(Color.FromRgb(_ambientLevel, _ambientLevel, _ambientLevel));
            _keyLight = new DirectionalLight(Color.FromRgb(_keyLevel, _keyLevel, _keyLevel), new Vector3D(-1, -1, -2));
            _fillLight = new DirectionalLight(Color.FromRgb(_fillLevel, _fillLevel, _fillLevel), new Vector3D(1, 1, 2));
            _rimLight = new DirectionalLight(Color.FromRgb(_rimLevel, _rimLevel, _rimLevel), new Vector3D(-1, 1, 1));

            root.Children.Add(_ambientLight);
            root.Children.Add(_keyLight);
            root.Children.Add(_fillLight);
            root.Children.Add(_rimLight);

            const double size = 1.0;
            double pitch = size + _cubeGap;

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 3; z++)
                    {
                        // Skip internal core
                        if (x == 1 && y == 1 && z == 1) continue;

                        var cubie = CreateCubie(size);
                        cubie.Transform = new TranslateTransform3D(
                            (x - 1) * pitch,
                            (y - 1) * pitch,
                            (z - 1) * pitch);

                        root.Children.Add(cubie);
                        _cubies[x, y, z] = cubie;
                    }

            var rootVisual = new ModelVisual3D
            {
                Content = root,
                Transform = _scene
            };

            MyViewport.Children.Add(rootVisual);
            _scene.Children.Add(new RotateTransform3D(_sceneRotation));
        }

        private Model3DGroup CreateCubie(double size)
        {
            var mg = new Model3DGroup();

            // Body
            var bodyDiffuse = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(25, 25, 25)));
            // Using typical Rubik's cube rounded look proportions
            const double bevelDistance = 0.08;
            var body = new GeometryModel3D(CreateBeveledBox(size, bevelDistance), bodyDiffuse);
            mg.Children.Add(body);

            // Stickers slightly smaller than size-bevel to look like stickers applied to the flat faces
            double s = size - bevelDistance * 2 - 0.02;
            double offset = size / 2;

            var stickerColors = new[]
            {
                new SolidColorBrush(Color.FromRgb(59, 130, 246)),  // 0: Blue (Back)
                new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // 1: Green (Front)
                new SolidColorBrush(Color.FromRgb(240, 240, 245)), // 2: White (Top)
                new SolidColorBrush(Color.FromRgb(250, 204, 21)),  // 3: Yellow (Bottom)
                new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // 4: Red (Right)
                new SolidColorBrush(Color.FromRgb(255, 140, 0))    // 5: Orange (Left)
            };

            for (int i = 0; i < 6; i++)
            {
                var mesh = CreateSquircleMesh(s, s * 0.15, 12);
                var stickerMaterialGroup = new MaterialGroup();
                stickerMaterialGroup.Children.Add(new DiffuseMaterial(stickerColors[i]));
                // Add a little specular for plastic shine - lowered power and color alpha to make it less shiny
                if (_glossEnabled && _shininessLevel > 0)
                {
                    stickerMaterialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(_shininessLevel, 255, 255, 255)), _shininessPower));
                }

                var face = new GeometryModel3D(mesh, stickerMaterialGroup)
                {
                    Transform = GetStickerXForm(i, offset)
                };
                mg.Children.Add(face);
            }

            return mg;
        }

        private MeshGeometry3D CreateSquircleMesh(double size, double radius, int segments)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var indices = new Int32Collection();

            double s_half = size / 2.0;
            positions.Add(new Point3D(0, 0, 0));

            for (int i = 0; i < 4; i++)
            {
                double cx = (i == 0 || i == 3) ? s_half - radius : -s_half + radius;
                double cy = (i < 2) ? s_half - radius : -s_half + radius;

                double startAngle = i * Math.PI / 2;
                for (int j = 0; j <= segments; j++)
                {
                    double angle = startAngle + (j * (Math.PI / 2) / segments);
                    double x = cx + radius * Math.Cos(angle);
                    double y = cy + radius * Math.Sin(angle);
                    positions.Add(new Point3D(x, y, 0));
                }
            }

            for (int i = 1; i < positions.Count; i++)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add((i % (positions.Count - 1)) + 1);
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = indices;
            return mesh;
        }

        private Transform3D GetStickerXForm(int f, double o)
        {
            var tg = new Transform3DGroup();
            double stickerOffset = o + 0.005;

            switch (f)
            {
                case 0: // Back
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
                    tg.Children.Add(new TranslateTransform3D(0, 0, -stickerOffset));
                    break;
                case 1: // Front
                    tg.Children.Add(new TranslateTransform3D(0, 0, stickerOffset));
                    break;
                case 2: // Top
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));
                    tg.Children.Add(new TranslateTransform3D(0, stickerOffset, 0));
                    break;
                case 3: // Bottom
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
                    tg.Children.Add(new TranslateTransform3D(0, -stickerOffset, 0));
                    break;
                case 4: // Right
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    tg.Children.Add(new TranslateTransform3D(stickerOffset, 0, 0));
                    break;
                case 5: // Left
                    tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
                    tg.Children.Add(new TranslateTransform3D(-stickerOffset, 0, 0));
                    break;
            }
            return tg;
        }

        private MeshGeometry3D CreateBeveledBox(double size, double bevel)
        {
            var mesh = new MeshGeometry3D();
            double s = size / 2.0;
            double d = s - bevel;
            mesh.Positions = new Point3DCollection {
                new Point3D( s, -d, -d ),
                new Point3D( s, d, -d ),
                new Point3D( s, d, d ),
                new Point3D( s, -d, d ),
                new Point3D( -s, -d, d ),
                new Point3D( -s, d, d ),
                new Point3D( -s, d, -d ),
                new Point3D( -s, -d, -d ),
                new Point3D( -d, s, -d ),
                new Point3D( d, s, -d ),
                new Point3D( d, s, d ),
                new Point3D( -d, s, d ),
                new Point3D( -d, -s, d ),
                new Point3D( d, -s, d ),
                new Point3D( d, -s, -d ),
                new Point3D( -d, -s, -d ),
                new Point3D( -d, -d, s ),
                new Point3D( d, -d, s ),
                new Point3D( d, d, s ),
                new Point3D( -d, d, s ),
                new Point3D( -d, d, -s ),
                new Point3D( d, d, -s ),
                new Point3D( d, -d, -s ),
                new Point3D( -d, -d, -s ),
            };
            mesh.TriangleIndices = new Int32Collection {
                0,1,2, 0,2,3,
                4,5,6, 4,6,7,
                11,10,9, 11,9,8,
                15,14,13, 15,13,12,
                16,17,18, 16,18,19,
                20,21,22, 20,22,23,
                0,22,21, 0,21,1,
                1,9,10, 1,10,2,
                2,18,17, 2,17,3,
                3,13,14, 3,14,0,
                4,16,19, 4,19,5,
                5,11,8, 5,8,6,
                6,20,23, 6,23,7,
                7,15,12, 7,12,4,
                11,19,18, 11,18,10,
                8,9,21, 8,21,20,
                12,13,17, 12,17,16,
                15,23,22, 15,22,14,
                2,10,18,
                1,21,9,
                3,17,13,
                0,14,22,
                5,19,11,
                6,8,20,
                4,12,16,
                7,23,15,
            };
            return mesh;
        }

        private MeshGeometry3D CreateSimpleCube(double size)
        {
            var mesh = new MeshGeometry3D();
            double h = size / 2;
            mesh.Positions = new Point3DCollection {
                new Point3D(-h,-h,h), new Point3D(h,-h,h), new Point3D(h,h,h), new Point3D(-h,h,h),
                new Point3D(-h,-h,-h), new Point3D(h,-h,-h), new Point3D(h,h,-h), new Point3D(-h,h,-h)
            };
            mesh.TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3, 1, 5, 6, 1, 6, 2, 5, 4, 7, 5, 7, 6, 4, 0, 3, 4, 3, 7, 3, 2, 6, 3, 6, 7, 1, 0, 4, 1, 4, 5 };
            return mesh;
        }

        private void UpdateCubeVisuals()
        {
            RebuildCubieTransforms();
            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        if (_cubies[xi, yi, zi] == null) continue;
                        var cubie = _cubies[xi, yi, zi];
                        int x = xi - 1, y = yi - 1, z = zi - 1;

                        for (int f = 0; f < 6; f++)
                        {
                            if (cubie.Children.Count <= f + 1) continue;
                            var face = (GeometryModel3D)cubie.Children[f + 1];
                            string? code = f switch
                            {
                                0 when z == -1 => _cube.B[1 - y, 1 - x],
                                1 when z == +1 => _cube.F[1 - y, x + 1],
                                2 when y == +1 => _cube.U[z + 1, x + 1],
                                3 when y == -1 => _cube.D[1 - z, x + 1],
                                4 when x == +1 => _cube.R[1 - y, 1 - z],
                                5 when x == -1 => _cube.L[1 - y, z + 1],
                                _ => null
                            };

                            Brush b = code switch
                            {
                                "W" => new SolidColorBrush(Color.FromRgb(240, 240, 245)),
                                "Y" => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                                "G" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                                "B" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                                "R" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                                "O" => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                                _ => Brushes.Transparent
                            };

                            if (face.Material is MaterialGroup matGroup && matGroup.Children[0] is DiffuseMaterial diffuse)
                            {
                                diffuse.Brush = b;
                            }
                        }
                    }
        }
        #endregion

        #region Input and Move Handling
        private void MoveInput_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            string move = MoveInput.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(move)) return;
            AnimateSingleMove(GetMappedMove(move));
            MoveInput.Clear();
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button b)) return;
            string move = b.Content.ToString();
            AnimateSingleMove(GetMappedMove(move));
        }

        private void AnimateSingleMove(string move)
        {
            if (_isAnimating || string.IsNullOrEmpty(move)) return;
            _isAnimating = true;
            StartTimerIfNeeded();
            AnimateMove(move, () =>
            {
                _isAnimating = false;
                _moveCount++;
                MoveCountText.Text = _moveCount.ToString();
                LastMoveText.Text = move;
                StatusText.Text = $"Applied move: {move}";
            });
        }

        private void AnimateMove(string move, Action onCompleted)
        {
            char face = move[0];
            double angle = move.Contains("'") ? 90 : (move.Contains("2") ? 180 : -90);

            var cubiesToAnimate = new List<Model3DGroup>();
            int sliceIndex;

            switch (face)
            {
                case 'R': case 'U': case 'F': sliceIndex = 2; break;
                case 'L': case 'D': case 'B': sliceIndex = 0; break;
                case 'M': sliceIndex = 1; break;
                default: sliceIndex = 1; break;
            }

            Vector3D axis = new Vector3D();
            switch (face)
            {
                case 'R': axis = new Vector3D(1, 0, 0); for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) cubiesToAnimate.Add(_cubies[sliceIndex, y, z]); break;
                case 'L': axis = new Vector3D(1, 0, 0); for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) cubiesToAnimate.Add(_cubies[sliceIndex, y, z]); break;
                case 'U': axis = new Vector3D(0, 1, 0); for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++) cubiesToAnimate.Add(_cubies[x, sliceIndex, z]); break;
                case 'D': axis = new Vector3D(0, 1, 0); for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++) cubiesToAnimate.Add(_cubies[x, sliceIndex, z]); break;
                case 'F': axis = new Vector3D(0, 0, 1); for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) cubiesToAnimate.Add(_cubies[x, y, sliceIndex]); break;
                case 'B': axis = new Vector3D(0, 0, 1); for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) cubiesToAnimate.Add(_cubies[x, y, sliceIndex]); break;
                case 'M': axis = new Vector3D(1, 0, 0); for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) cubiesToAnimate.Add(_cubies[sliceIndex, y, z]); break;
            }

            var rotation = new AxisAngleRotation3D(axis, 0);
            var rotateTransform = new RotateTransform3D(rotation);

            foreach (var cubie in cubiesToAnimate)
            {
                if (cubie == null) continue;
                var transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(cubie.Transform);
                transformGroup.Children.Add(rotateTransform);
                cubie.Transform = transformGroup;
            }

            double visualAngle = angle;
            if (face == 'L' || face == 'D' || face == 'B' || face == 'M') visualAngle = -angle;

            var animation = new DoubleAnimation(0, visualAngle, new Duration(GetScaledDuration(250)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                PerformMove(move);
                onCompleted?.Invoke();
            };

            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
        }

        private void PerformMove(string move)
        {
            _cube.PerformMove(move);
            UpdateCubiePositions(move);
            UpdateCubeVisuals();
        }

        private void UpdateCubiePositions(string move)
        {
            char face = move[0];
            int turns = move.Contains("'") ? 3 : (move.Contains("2") ? 2 : 1);
            for (int i = 0; i < turns; i++)
            {
                var temp = (Model3DGroup[,,])_cubies.Clone();
                switch (face)
                {
                    case 'R': for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) _cubies[2, y, z] = temp[2, z, 2 - y]; break;
                    case 'L': for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) _cubies[0, y, z] = temp[0, 2 - z, y]; break;
                    case 'U': for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++) _cubies[x, 2, z] = temp[z, 2, 2 - x]; break;
                    case 'D': for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++) _cubies[x, 0, z] = temp[2 - z, 0, x]; break;
                    case 'F': for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) _cubies[x, y, 2] = temp[y, 2 - x, 2]; break;
                    case 'B': for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) _cubies[x, y, 0] = temp[2 - y, x, 0]; break;
                    case 'M': for (int y = 0; y < 3; y++) for (int z = 0; z < 3; z++) _cubies[1, y, z] = temp[1, z, 2 - y]; break;
                }
            }
        }

        private void RebuildCubieTransforms()
        {
            const double size = 1.0;
            double pitch = size + _cubeGap;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 3; z++)
                    {
                        if (_cubies[x, y, z] != null)
                            _cubies[x, y, z].Transform = new TranslateTransform3D((x - 1) * pitch, (y - 1) * pitch, (z - 1) * pitch);
                    }
        }

        private void RebuildCubeScene()
        {
            if (_isAnimating || _isSceneAnimating || !_settingsReady)
            {
                return;
            }

            MyViewport.Children.Clear();
            _scene.Children.Clear();
            Array.Clear(_cubies, 0, _cubies.Length);
            BuildCube();
            UpdateCubeVisuals();
            UpdateUIForPerspective();
        }

        private void SyncSettingsControls()
        {
            if (GapSlider != null)
            {
                GapSlider.Value = _cubeGap;
            }

            if (GlossSlider != null)
            {
                GlossSlider.Value = _shininessLevel;
            }

            if (SpeedSlider != null)
            {
                SpeedSlider.Value = _animationSpeedScale;
            }

            if (GlossToggle != null)
            {
                GlossToggle.IsChecked = _glossEnabled;
            }

            if (AmbientSlider != null)
            {
                AmbientSlider.Value = _ambientLevel;
            }

            if (KeyLightSlider != null)
            {
                KeyLightSlider.Value = _keyLevel;
            }

            if (FillLightSlider != null)
            {
                FillLightSlider.Value = _fillLevel;
            }

            if (RimLightSlider != null)
            {
                RimLightSlider.Value = _rimLevel;
            }

            if (RimLightToggle != null)
            {
                RimLightToggle.IsChecked = _rimLightEnabled;
            }

            UpdateSettingsLabels();
            ApplyLightingSettings();
            ApplyGlossSettings();
        }

        private void UpdateSettingsLabels()
        {
            if (GapValueText != null)
            {
                GapValueText.Text = _cubeGap.ToString("0.00");
            }

            if (GlossValueText != null)
            {
                GlossValueText.Text = _shininessLevel.ToString();
            }

            if (SpeedValueText != null)
            {
                SpeedValueText.Text = _animationSpeedScale.ToString("0.0x");
            }

            if (AmbientValueText != null)
            {
                AmbientValueText.Text = _ambientLevel.ToString();
            }

            if (KeyLightValueText != null)
            {
                KeyLightValueText.Text = _keyLevel.ToString();
            }

            if (FillLightValueText != null)
            {
                FillLightValueText.Text = _fillLevel.ToString();
            }

            if (RimLightValueText != null)
            {
                RimLightValueText.Text = _rimLevel.ToString();
            }
        }

        private void ApplyLightingSettings()
        {
            if (_ambientLight != null)
            {
                _ambientLight.Color = Color.FromRgb(_ambientLevel, _ambientLevel, _ambientLevel);
            }

            if (_keyLight != null)
            {
                _keyLight.Color = Color.FromRgb(_keyLevel, _keyLevel, _keyLevel);
            }

            if (_fillLight != null)
            {
                _fillLight.Color = Color.FromRgb(_fillLevel, _fillLevel, _fillLevel);
            }

            if (_rimLight != null)
            {
                if (_rimLightEnabled)
                {
                    _rimLight.Color = Color.FromRgb(_rimLevel, _rimLevel, _rimLevel);
                }
                else
                {
                    _rimLight.Color = Colors.Black;
                }
            }
        }

        private void ApplyGlossSettings()
        {
            // Map slider level to a reasonable specular power for visible changes
            _shininessPower = Math.Max(1.0, _shininessLevel / 16.0);

            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        var cubie = _cubies[xi, yi, zi];
                        if (cubie == null) continue;

                        // children[0] is body, stickers start at index 1
                        for (int ci = 1; ci < cubie.Children.Count; ci++)
                        {
                            if (!(cubie.Children[ci] is GeometryModel3D face)) continue;
                            if (face.Material is MaterialGroup mg)
                            {
                                SpecularMaterial? spec = null;
                                for (int mi = 0; mi < mg.Children.Count; mi++)
                                {
                                    if (mg.Children[mi] is SpecularMaterial s) { spec = s; break; }
                                }

                                if (_glossEnabled && _shininessLevel > 0)
                                {
                                    if (spec == null)
                                    {
                                        var brush = new SolidColorBrush(Color.FromArgb(_shininessLevel, 255, 255, 255));
                                        spec = new SpecularMaterial(brush, _shininessPower);
                                        mg.Children.Add(spec);
                                    }
                                    else
                                    {
                                        if (spec.Brush is SolidColorBrush scb)
                                        {
                                            scb.Color = Color.FromArgb(_shininessLevel, 255, 255, 255);
                                        }
                                        spec.SpecularPower = _shininessPower;
                                    }
                                }
                                else
                                {
                                    if (spec != null) mg.Children.Remove(spec);
                                }
                            }
                        }
                    }
        }

        private TimeSpan GetScaledDuration(double baseMilliseconds)
        {
            return TimeSpan.FromMilliseconds(baseMilliseconds * _animationSpeedScale);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SyncSettingsControls();
            _settingsReady = true;
        }

        private void GapSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _cubeGap = Math.Max(0.0, e.NewValue);
            UpdateSettingsLabels();
            RebuildCubeScene();
        }

        private void GlossSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _shininessLevel = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(e.NewValue)));
            UpdateSettingsLabels();
            ApplyGlossSettings();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _animationSpeedScale = Math.Max(0.5, e.NewValue);
            UpdateSettingsLabels();
        }

        private void GlossToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _glossEnabled = true;
            if (_shininessLevel == 0)
            {
                _shininessLevel = 24;
                if (GlossSlider != null)
                {
                    GlossSlider.Value = _shininessLevel;
                }
            }

            UpdateSettingsLabels();
            ApplyGlossSettings();
        }

        private void GlossToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _glossEnabled = false;
            UpdateSettingsLabels();
            ApplyGlossSettings();
        }

        private void AmbientSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _ambientLevel = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(e.NewValue)));
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }

        private void KeyLightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _keyLevel = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(e.NewValue)));
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }

        private void FillLightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _fillLevel = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(e.NewValue)));
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }

        private void RimLightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _rimLevel = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(e.NewValue)));
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }

        private void RimLightToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _rimLightEnabled = true;
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }

        private void RimLightToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_settingsReady)
            {
                return;
            }

            _rimLightEnabled = false;
            UpdateSettingsLabels();
            ApplyLightingSettings();
        }
        #endregion

        #region Logic: Orientation Map and Scene Rotation

        private void RecalculateDynamicOrientation()
        {
            _orientationMap.Clear();
            Quaternion q = _sceneRotation.Quaternion;
            q.Invert();
            var matrix = Matrix3D.Identity;
            matrix.Rotate(q);

            var screenVectors = new Dictionary<char, Vector3D>
            {
                { 'U', new Vector3D(0, 1, 0) },
                { 'D', new Vector3D(0, -1, 0) },
                { 'R', new Vector3D(1, 0, 0) },
                { 'L', new Vector3D(-1, 0, 0) },
                { 'F', new Vector3D(0, 0, 1) },
                { 'B', new Vector3D(0, 0, -1) }
            };

            var internalAxes = new Dictionary<char, Vector3D>
            {
                { 'U', new Vector3D(0, 1, 0) },
                { 'D', new Vector3D(0, -1, 0) },
                { 'R', new Vector3D(1, 0, 0) },
                { 'L', new Vector3D(-1, 0, 0) },
                { 'F', new Vector3D(0, 0, 1) },
                { 'B', new Vector3D(0, 0, -1) }
            };

            foreach (var screenKvp in screenVectors)
            {
                Vector3D objectDir = matrix.Transform(screenKvp.Value);
                char closestFace = '?';
                double maxDot = -2;

                foreach (var internalKvp in internalAxes)
                {
                    double dot = Vector3D.DotProduct(objectDir, internalKvp.Value);
                    if (dot > maxDot)
                    {
                        maxDot = dot;
                        closestFace = internalKvp.Key;
                    }
                }
                _orientationMap[screenKvp.Key] = closestFace;
            }
        }

        private string GetMappedMove(string move)
        {
            if (string.IsNullOrEmpty(move)) return move;
            char inputFace = move[0];
            if (_orientationMap.ContainsKey(inputFace))
            {
                char internalFace = _orientationMap[inputFace];
                string suffix = move.Length > 1 ? move.Substring(1) : "";
                return internalFace + suffix;
            }
            return move;
        }

        private void CubeRotationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating || !(sender is Button b)) return;
            AnimateWholeCubeRotation(b.Content.ToString());
        }

        private void CubeRollButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating || !(sender is Button b)) return;
            string tag = b.Tag?.ToString() ?? b.Content.ToString();
            bool isClockwise = tag.Contains("CW") && !tag.Contains("CCW");
            AnimateCubeRoll(isClockwise);
        }

        private void AnimateWholeCubeRotation(string rotation)
        {
            if (_isAnimating) return;
            _isAnimating = true;
            _isSceneAnimating = true;

            var axis = new Vector3D();
            double angle = rotation.Contains("'") ? -90 : (rotation.Contains("2") ? 180 : 90);
            switch (rotation[0])
            {
                case 'x': axis = new Vector3D(1, 0, 0); break;
                case 'y': axis = new Vector3D(0, 1, 0); break;
                case 'z': axis = new Vector3D(0, 0, 1); break;
                default: 
                    _isAnimating = false; 
                    _isSceneAnimating = false; 
                    return;
            }

            var deltaRotation = new Quaternion(axis, angle);
            var targetQuaternion = deltaRotation * _sceneRotation.Quaternion;

            var animation = new QuaternionAnimation(targetQuaternion, new Duration(GetScaledDuration(300)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                _sceneRotation.Quaternion = targetQuaternion;
                // RELEASE LOCK
                _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, null);

                RecalculateDynamicOrientation();
                _isAnimating = false;
                _isSceneAnimating = false;
            };
            _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, animation);
        }

        private void AnimateCubeRoll(bool isClockwise)
        {
            if (_isAnimating) return;
            _isAnimating = true;
            _isSceneAnimating = true;

            double angle = isClockwise ? 90 : -90;
            var axis = new Vector3D(0, 0, 1);
            var deltaRotation = new Quaternion(axis, angle);
            var targetQuaternion = deltaRotation * _sceneRotation.Quaternion;

            var animation = new QuaternionAnimation(targetQuaternion, new Duration(GetScaledDuration(300)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                _sceneRotation.Quaternion = targetQuaternion;
                // RELEASE LOCK
                _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, null);

                RecalculateDynamicOrientation();
                _isAnimating = false;
                _isSceneAnimating = false;
            };
            _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, animation);
        }

        private void SwitchPerspective_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating || !(sender is Button b)) return;

            CubePerspective targetPerspective = b.Content.ToString() switch
            {
                "W" => CubePerspective.White,
                "Y" => CubePerspective.Yellow,
                "G" => CubePerspective.Green,
                "B" => CubePerspective.Blue,
                "R" => CubePerspective.Red,
                "O" => CubePerspective.Orange,
                _ => _currentPerspective
            };

            if (targetPerspective != _currentPerspective)
            {
                _currentPerspective = targetPerspective;
                RotateToCurrentPerspective();
            }
        }

        private void RotateToCurrentPerspective()
        {
            if (_isAnimating) return;
            _isAnimating = true;
            _isSceneAnimating = true;

            Quaternion targetQuaternion;
            switch (_currentPerspective)
            {
                case CubePerspective.Green: targetQuaternion = Quaternion.Identity; break;
                case CubePerspective.Blue: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), 180); break;
                case CubePerspective.Red: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), -90); break;
                case CubePerspective.Orange: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), 90); break;
                case CubePerspective.White: targetQuaternion = new Quaternion(new Vector3D(1, 0, 0), 90); break;
                case CubePerspective.Yellow: targetQuaternion = new Quaternion(new Vector3D(1, 0, 0), -90); break;
                default: targetQuaternion = Quaternion.Identity; break;
            }

            var duration = new Duration(GetScaledDuration(400));
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var quatAnim = new QuaternionAnimation(targetQuaternion, duration) { EasingFunction = ease };

            quatAnim.Completed += (s, e) =>
            {
                _sceneRotation.Quaternion = targetQuaternion;
                // RELEASE LOCK HERE TO ALLOW MOUSE TO WORK AGAIN
                _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, null);

                UpdateUIForPerspective();
                RecalculateDynamicOrientation();
                _isAnimating = false;
                _isSceneAnimating = false;
            };

            _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, quatAnim);
        }

        private void SnapToCurrentPerspective()
        {
            const double camDistance = 9;
            Camera.Position = new Point3D(0, 0, camDistance);
            Camera.LookDirection = new Vector3D(0, 0, -1);
            Camera.UpDirection = new Vector3D(0, 1, 0);

            Quaternion targetQuaternion;
            switch (_currentPerspective)
            {
                case CubePerspective.Green: targetQuaternion = Quaternion.Identity; break;
                case CubePerspective.Blue: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), 180); break;
                case CubePerspective.Red: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), -90); break;
                case CubePerspective.Orange: targetQuaternion = new Quaternion(new Vector3D(0, 1, 0), 90); break;
                case CubePerspective.White: targetQuaternion = new Quaternion(new Vector3D(1, 0, 0), 90); break;
                case CubePerspective.Yellow: targetQuaternion = new Quaternion(new Vector3D(1, 0, 0), -90); break;
                default: targetQuaternion = Quaternion.Identity; break;
            }
            _sceneRotation.Quaternion = targetQuaternion;

            UpdateUIForPerspective();
            RecalculateDynamicOrientation();
        }

        private void UpdateUIForPerspective()
        {
            var white = new SolidColorBrush(Color.FromRgb(240, 240, 245));
            var yellow = new SolidColorBrush(Color.FromRgb(250, 204, 21));
            var green = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            var blue = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            var red = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            var orange = new SolidColorBrush(Color.FromRgb(255, 140, 0));

            (Brush, Brush, Brush, Brush, Brush, Brush) faceBrushes;
            switch (_currentPerspective)
            {
                case CubePerspective.Green:  faceBrushes = (white, yellow, orange, red, green, blue); break;
                case CubePerspective.Blue:   faceBrushes = (white, yellow, red, orange, blue, green); break;
                case CubePerspective.Red:    faceBrushes = (white, yellow, green, blue, red, orange); break;
                case CubePerspective.Orange: faceBrushes = (white, yellow, blue, green, orange, red); break;
                case CubePerspective.White:  faceBrushes = (green, blue, orange, red, yellow, white); break;
                case CubePerspective.Yellow: faceBrushes = (blue, green, orange, red, white, yellow); break;
                default: return;
            }
            if (UpFace != null)
            {
                UpFace.Background = faceBrushes.Item1;
                DownFace.Background = faceBrushes.Item2;
                LeftFace.Background = faceBrushes.Item3;
                RightFace.Background = faceBrushes.Item4;
                FrontFace.Background = faceBrushes.Item5;
                BackFace.Background = faceBrushes.Item6;
            }
        }
        #endregion

        #region Scramble
        private void ScrambleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;
            var moves = new[] { "R", "L", "U", "D", "F", "B" };
            var modifiers = new[] { "", "'", "2" };
            var random = new Random();

            _queueUsesMappedMoves = true;
            _moveQueue.Clear();
            for (int i = 0; i < 20; i++)
            {
                var move = moves[random.Next(moves.Length)];
                var modifier = modifiers[random.Next(modifiers.Length)];
                _moveQueue.Enqueue(move + modifier);
            }

            // Reset counters for new scramble
            _moveCount = 0;
            MoveCountText.Text = "0";
            StopTimer();
            StatusText.Text = "Scrambling...";

            if (_moveQueue.Count > 0)
            {
                _isAnimating = true;
                ProcessMoveQueue();
            }
        }

        private void ProcessMoveQueue()
        {
            if (_moveQueue.Count > 0)
            {
                string move = _moveQueue.Dequeue();
                string effectiveMove = _queueUsesMappedMoves ? GetMappedMove(move) : move;
                AnimateMove(effectiveMove, ProcessMoveQueue);
            }
            else
            {
                _isAnimating = false;
                _queueUsesMappedMoves = true;
                StatusText.Text = "Scramble complete — ready to solve!";
                LastMoveText.Text = "\u2014";
            }
        }
        #endregion

        #region Reset & Timer
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;
            _cube.Reset();
            UpdateCubeVisuals();
            _moveCount = 0;
            MoveCountText.Text = "0";
            StopTimer();
            TimerText.Text = "0:00";
            LastMoveText.Text = "\u2014";
            StatusText.Text = "Cube reset";
        }

        private void SolveBeginnerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;

            // Initialize the solver with the current state of the cube
            var solver = new BeginnersMethodSolver(_cube);
            var solutionMoves = solver.Solve();

            if (solutionMoves.Count == 0)
            {
                StatusText.Text = "Cube is already solved (or solver not fully completed)!";
                return;
            }

            // Enqueue all the solved moves for animation
            _queueUsesMappedMoves = false;
            _moveQueue.Clear();
            foreach (var move in solutionMoves)
            {
                System.Diagnostics.Debug.WriteLine("Solver move: " + move);
                _moveQueue.Enqueue(move);
            }

            // Start animation
            if (_moveQueue.Count > 0)
            {
                _isAnimating = true;
                StatusText.Text = $"Solving using Beginner's Method... ({solutionMoves.Count} moves)";
                ProcessMoveQueue();
            }
        }

        private void StartTimerIfNeeded()
        {
            if (!_timerRunning)
            {
                _timerRunning = true;
                _timerStart = DateTime.Now;
                _uiTimer.Start();
            }
        }

        private void StopTimer()
        {
            _timerRunning = false;
            _uiTimer.Stop();
        }

        private void UpdateTimerDisplay()
        {
            if (!_timerRunning) return;
            var elapsed = DateTime.Now - _timerStart;
            TimerText.Text = elapsed.TotalMinutes >= 1
                ? $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}"
                : $"0:{(int)elapsed.TotalSeconds:D2}";
        }
        #endregion

        #region Mouse Rotation
        private void MyViewport_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isSceneAnimating) return;
            
            // If the scene is spinning from a perspective jump, stop that WPF animation 
            // so manual manipulation can take over seamlessly.
            _sceneRotation.BeginAnimation(QuaternionRotation3D.QuaternionProperty, null);

            _dragging = true;
            _lastMouse = e.GetPosition(this);
            Mouse.Capture(MyViewport);
        }

        private void MyViewport_MouseMove(object s, MouseEventArgs e)
        {
            if (!_dragging) return;
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMouse;
            _lastMouse = currentPosition;

            Vector3D up = new Vector3D(0, 1, 0);
            Vector3D right = new Vector3D(1, 0, 0);
            double scale = 0.5;

            var yaw = new Quaternion(up, delta.X * scale);
            var pitch = new Quaternion(right, delta.Y * scale);

            _sceneRotation.Quaternion = yaw * pitch * _sceneRotation.Quaternion;
        }

        private void MyViewport_MouseUp(object s, MouseButtonEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                Mouse.Capture(null);
                RecalculateDynamicOrientation();
            }
        }
        #endregion
    }
}