using System;
using System.Collections.Generic;

namespace Rubik_s_Cube.Models
{
    public class BeginnersMethodSolver
    {
        private RubiksCube _cube;
        private List<string> _solution;

        private string cU => _cube.U[1, 1];
        private string cD => _cube.D[1, 1];
        private string cF => _cube.F[1, 1];
        private string cR => _cube.R[1, 1];
        private string cB => _cube.B[1, 1];
        private string cL => _cube.L[1, 1];

        public BeginnersMethodSolver(RubiksCube source)
        {
            _cube = CloneCube(source);
            _solution = new List<string>();
        }

        public List<string> Solve()
        {
            _solution.Clear();
            if (IsSolved()) return _solution;

            // Step 1: White Cross
            SolveWhiteCross();

            // Step 2: First Layer Corners
            SolveWhiteCorners();

            // Step 3: F2L Middle Layer
            SolveSecondLayer();

            // Step 4: Yellow Cross
            SolveYellowCross();

            // Step 5: Yellow Corners
            SolveYellowFace();

            // Step 6: Position Corners
            PositionYellowCorners();

            // Step 7: Position Edges
            PositionYellowEdges();

            OptimizeSolution();
            return _solution;
        }

        private void Apply(string m)
        {
            if (string.IsNullOrWhiteSpace(m)) return;
            foreach (var move in m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                _cube.PerformMove(move);
                _solution.Add(move);
            }
        }

        private RubiksCube CloneCube(RubiksCube source)
        {
            var target = new RubiksCube();
            Array.Copy(source.U, target.U, 9);
            Array.Copy(source.D, target.D, 9);
            Array.Copy(source.L, target.L, 9);
            Array.Copy(source.R, target.R, 9);
            Array.Copy(source.F, target.F, 9);
            Array.Copy(source.B, target.B, 9);
            return target;
        }

        private bool IsSolved()
        {
            return CheckFace(_cube.U) && CheckFace(_cube.D) &&
                   CheckFace(_cube.F) && CheckFace(_cube.B) &&
                   CheckFace(_cube.L) && CheckFace(_cube.R);
        }

        private bool CheckFace(string[,] face)
        {
            string c = face[1, 1];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (face[i, j] != c) return false;
            return true;
        }

        // ════════════════════════════════════════════════════════
        // Phase 1: White Cross 
        // ════════════════════════════════════════════════════════
        private void SolveWhiteCross()
        {
            Action<string, string, string, string, string, string> solveEdge = (clrD, clrSide, tgtD, tgtU, insStd, insFlip) =>
            {
                for (int i = 0; i < 30; i++)
                {
                    var e = GetEdge(clrD, clrSide);
                    if (e.Item1 == tgtD && e.Item2 == clrD) break;

                    if (e.Item1.StartsWith("D")) {
                        if (e.Item1 == "DF") Apply("F2");
                        else if (e.Item1 == "DR") Apply("R2");
                        else if (e.Item1 == "DB") Apply("B2");
                        else if (e.Item1 == "DL") Apply("L2");
                    }
                    else if (e.Item1 == "FR") Apply("R U R'");
                    else if (e.Item1 == "RB") Apply("B U B'");
                    else if (e.Item1 == "BL") Apply("L U L'");
                    else if (e.Item1 == "LF") Apply("F U F'");
                    else {
                        if (e.Item1 != tgtU) Apply("U");
                        else {
                            if (e.Item2 == clrD) Apply(insStd); 
                            else Apply(insFlip);                
                        }
                    }
                }
            };

            solveEdge(cD, cF, "DF", "UF", "F2", "U' R' F R");
            solveEdge(cD, cR, "DR", "UR", "R2", "U' B' R B");
            solveEdge(cD, cB, "DB", "UB", "B2", "U' L' B L");
            solveEdge(cD, cL, "DL", "UL", "L2", "U' F' L F");
        }

        // ════════════════════════════════════════════════════════
        // Phase 2: First Layer Corners
        // ════════════════════════════════════════════════════════
        private void SolveWhiteCorners()
        {
            Action<string, string, string, string, string, string> solveCorner = (clrD, clrRight, clrLeft, tgtD, tgtU, kick) =>
            {
                for (int i = 0; i < 30; i++)
                {
                    var c = GetCorner(clrD, clrRight, clrLeft);
                    if (c.Item1 == tgtD && c.Item2 == clrD) break;

                    if (c.Item1.StartsWith("D") && c.Item1 != tgtD) {
                        if (c.Item1 == "DFR") Apply("R U R'");
                        else if (c.Item1 == "DBR") Apply("B U B'");
                        else if (c.Item1 == "DBL") Apply("L U L'");
                        else if (c.Item1 == "DFL") Apply("F U F'");
                    }
                    else if (c.Item1 == tgtD) {
                        Apply(kick); 
                    }
                    else {
                        if (c.Item1 != tgtU) Apply("U");
                        else Apply(kick + " U'"); 
                    }
                }
            };

            solveCorner(cD, cF, cR, "DFR", "UFR", "R U R'");
            solveCorner(cD, cR, cB, "DBR", "UBR", "B U B'");
            solveCorner(cD, cB, cL, "DBL", "UBL", "L U L'");
            solveCorner(cD, cL, cF, "DFL", "UFL", "F U F'");
        }

        // ════════════════════════════════════════════════════════
        // Phase 3: F2L Middle Layer
        // ════════════════════════════════════════════════════════
        private void SolveSecondLayer()
        {
            Action<string, string, string, string, string, string, string> solveF2L = (cFrt, cRht, tgtPos, uFceF, uFceR, insR, insL) =>
            {
                for (int i = 0; i < 30; i++)
                {
                    var e = GetEdge(cFrt, cRht);
                    if (e.Item1 == tgtPos && e.Item2 == cFrt) break;

                    if (!e.Item1.StartsWith("U")) {
                        if (e.Item1 == "FR") Apply("U R U' R' U' F' U F");
                        else if (e.Item1 == "RB") Apply("U B U' B' U' R' U R");
                        else if (e.Item1 == "BL") Apply("U L U' L' U' B' U B");
                        else if (e.Item1 == "LF") Apply("U F U' F' U' L' U L");
                    }
                    else {
                        if (e.Item3 == cFrt) { 
                            if (e.Item1 != uFceF) Apply("U");
                            else Apply(insR); 
                        } else {               
                            if (e.Item1 != uFceR) Apply("U");
                            else Apply(insL); 
                        }
                    }
                }
            };

            solveF2L(cF, cR, "FR", "UF", "UR", "U R U' R' U' F' U F", "U' F' U F U R U' R'");
            solveF2L(cR, cB, "RB", "UR", "UB", "U B U' B' U' R' U R", "U' R' U R U B U' B'");
            solveF2L(cB, cL, "BL", "UB", "UL", "U L U' L' U' B' U B", "U' B' U B U L U' L'");
            solveF2L(cL, cF, "LF", "UL", "UF", "U F U' F' U' L' U L", "U' L' U L U F U' F'");
        }

        // ════════════════════════════════════════════════════════
        // Phase 4: Yellow Cross (OLL 1)
        // ════════════════════════════════════════════════════════
        private void SolveYellowCross()
        {
            for (int i = 0; i < 15; i++)
            {
                bool uf = _cube.U[2, 1] == cU;
                bool ur = _cube.U[1, 2] == cU;
                bool ub = _cube.U[0, 1] == cU;
                bool ul = _cube.U[1, 0] == cU;

                if (uf && ur && ub && ul) break;

                if (ul && ub) Apply("F R U R' U' F'"); 
                else if (ul && ur) Apply("F R U R' U' F'"); 
                else if (!uf && !ur && !ub && !ul) Apply("F R U R' U' F'"); 
                else Apply("U");
            }
        }

        // ════════════════════════════════════════════════════════
        // Phase 5: Yellow Face (OLL 2)
        // ════════════════════════════════════════════════════════
        private void SolveYellowFace()
        {
            for (int i = 0; i < 15; i++)
            {
                int count = (_cube.U[0, 0] == cU ? 1 : 0) + (_cube.U[0, 2] == cU ? 1 : 0) +
                            (_cube.U[2, 0] == cU ? 1 : 0) + (_cube.U[2, 2] == cU ? 1 : 0);
                if (count == 4) break;

                if (count == 1 && _cube.U[2, 0] == cU) Apply("R U R' U R U2 R'");
                else if (count == 2 && _cube.F[0, 0] == cU) Apply("R U R' U R U2 R'");
                else if (count == 0 && _cube.L[0, 2] == cU) Apply("R U R' U R U2 R'");
                else Apply("U");
            }
        }

        // ════════════════════════════════════════════════════════
        // Phase 6: Top Layer Corners (PLL 1)
        // ════════════════════════════════════════════════════════
        private void PositionYellowCorners()
        {
            for (int i = 0; i < 15; i++)
            {
                bool mF = _cube.F[0, 0] == _cube.F[0, 2];
                bool mR = _cube.R[0, 0] == _cube.R[0, 2];
                bool mB = _cube.B[0, 0] == _cube.B[0, 2];
                bool mL = _cube.L[0, 0] == _cube.L[0, 2];
                
                if (mF && mR && mB && mL) break;

                if (mB) Apply("R' F R' B2 R F' R' B2 R2");
                else if (mF || mR || mL) Apply("U");
                else Apply("R' F R' B2 R F' R' B2 R2");
            }

            for (int i = 0; i < 4; i++) {
                if (_cube.F[0, 0] == cF) break;
                Apply("U");
            }
        }

        // ════════════════════════════════════════════════════════
        // Phase 7: Top Layer Edges (PLL 2)
        // ════════════════════════════════════════════════════════
        private void PositionYellowEdges()
        {
            string[] edgeCycle = { "R", "U'", "R", "U", "R", "U", "R", "U'", "R'", "U'", "R2" };

            bool IsTopLayerSolved(RubiksCube cube)
            {
                return cube.F[0, 0] == cF && cube.F[0, 1] == cF && cube.F[0, 2] == cF &&
                       cube.R[0, 0] == cR && cube.R[0, 1] == cR && cube.R[0, 2] == cR &&
                       cube.B[0, 0] == cB && cube.B[0, 1] == cB && cube.B[0, 2] == cB &&
                       cube.L[0, 0] == cL && cube.L[0, 1] == cL && cube.L[0, 2] == cL;
            }

            void ApplyMoves(RubiksCube cube, List<string> sequence, IEnumerable<string> moves)
            {
                foreach (var move in moves)
                {
                    cube.PerformMove(move);
                    sequence.Add(move);
                }
            }

            void ApplyU(RubiksCube cube, List<string> sequence, int turns)
            {
                for (int i = 0; i < turns; i++)
                {
                    cube.PerformMove("U");
                    sequence.Add("U");
                }
            }

            if (IsTopLayerSolved(_cube)) return;

            for (int depth = 1; depth <= 3; depth++)
            {
                int max1 = depth >= 1 ? 4 : 1;
                int max2 = depth >= 2 ? 4 : 1;
                int max3 = depth >= 3 ? 4 : 1;

                for (int firstRotation = 0; firstRotation < max1; firstRotation++)
                {
                    for (int secondRotation = 0; secondRotation < max2; secondRotation++)
                    {
                        for (int thirdRotation = 0; thirdRotation < max3; thirdRotation++)
                        {
                            var trial = CloneCube(_cube);
                            var sequence = new List<string>();

                            if (depth >= 1) {
                                ApplyU(trial, sequence, firstRotation);
                                ApplyMoves(trial, sequence, edgeCycle);
                            }
                            if (depth >= 2) {
                                ApplyU(trial, sequence, secondRotation);
                                ApplyMoves(trial, sequence, edgeCycle);
                            }
                            if (depth >= 3) {
                                ApplyU(trial, sequence, thirdRotation);
                                ApplyMoves(trial, sequence, edgeCycle);
                            }

                            // Check all U rotations at the very end
                            for (int finalU = 0; finalU < 4; finalU++)
                            {
                                var finalTrial = CloneCube(trial);
                                var finalSequence = new List<string>(sequence);
                                ApplyU(finalTrial, finalSequence, finalU);

                                if (IsTopLayerSolved(finalTrial))
                                {
                                    Apply(string.Join(" ", finalSequence));
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private (string, string, string) GetEdge(string clr1, string clr2)
        {
            var edges = new[] {
                ("UF", _cube.U[2, 1], _cube.F[0, 1]), ("UR", _cube.U[1, 2], _cube.R[0, 1]),
                ("UB", _cube.U[0, 1], _cube.B[0, 1]), ("UL", _cube.U[1, 0], _cube.L[0, 1]),
                ("DF", _cube.D[0, 1], _cube.F[2, 1]), ("DR", _cube.D[1, 2], _cube.R[2, 1]),
                ("DB", _cube.D[2, 1], _cube.B[2, 1]), ("DL", _cube.D[1, 0], _cube.L[2, 1]),
                ("FR", _cube.F[1, 2], _cube.R[1, 0]), ("RB", _cube.R[1, 2], _cube.B[1, 0]),
                ("BL", _cube.B[1, 2], _cube.L[1, 0]), ("LF", _cube.L[1, 2], _cube.F[1, 0])
            };
            foreach (var e in edges) {
                if (e.Item2 == clr1 && e.Item3 == clr2) return e;
                if (e.Item2 == clr2 && e.Item3 == clr1) return e; 
            }
            return ("", "", "");
        }

        private (string, string, string, string) GetCorner(string clr1, string clr2, string clr3)
        {
            var corners = new[] {
                ("UFL", _cube.U[2, 0], _cube.F[0, 0], _cube.L[0, 2]),
                ("UFR", _cube.U[2, 2], _cube.F[0, 2], _cube.R[0, 0]),
                ("UBR", _cube.U[0, 2], _cube.B[0, 0], _cube.R[0, 2]),
                ("UBL", _cube.U[0, 0], _cube.B[0, 2], _cube.L[0, 0]),
                ("DFL", _cube.D[0, 0], _cube.F[2, 0], _cube.L[2, 2]),
                ("DFR", _cube.D[0, 2], _cube.F[2, 2], _cube.R[2, 0]),
                ("DBR", _cube.D[2, 2], _cube.B[2, 0], _cube.R[2, 2]),
                ("DBL", _cube.D[2, 0], _cube.B[2, 2], _cube.L[2, 0])
            };
            string[] target = { clr1, clr2, clr3 };
            Array.Sort(target);
            foreach (var c in corners) {
                string[] curr = { c.Item2, c.Item3, c.Item4 };
                Array.Sort(curr);
                if (target[0] == curr[0] && target[1] == curr[1] && target[2] == curr[2]) return c;
            }
            return ("", "", "", "");
        }

        private void OptimizeSolution()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < _solution.Count - 1; i++)
                {
                    string m1 = _solution[i], m2 = _solution[i + 1];
                    if (m1[0] != m2[0]) continue;

                    int v1 = m1.Contains("2") ? 2 : m1.Contains("'") ? 3 : 1;
                    int v2 = m2.Contains("2") ? 2 : m2.Contains("'") ? 3 : 1;
                    int sum = (v1 + v2) % 4;

                    _solution.RemoveRange(i, 2);
                    if (sum == 1) _solution.Insert(i, m1[0].ToString());
                    else if (sum == 2) _solution.Insert(i, m1[0] + "2");
                    else if (sum == 3) _solution.Insert(i, m1[0] + "'");

                    changed = true;
                    break;
                }
            }
        }
    }
}