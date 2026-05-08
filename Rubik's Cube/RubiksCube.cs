using System;
using System.Linq;

namespace Rubik_s_Cube.Models
{
    public class RubiksCube
    {
        public string[,] U { get; set; } = new string[3, 3];
        public string[,] D { get; set; } = new string[3, 3];
        public string[,] L { get; set; } = new string[3, 3];
        public string[,] R { get; set; } = new string[3, 3];
        public string[,] F { get; set; } = new string[3, 3];
        public string[,] B { get; set; } = new string[3, 3];

        public RubiksCube()
        {
            Reset();
        }

        public void Reset()
        {
            // Default orientation: solver expects the face solved first to be on D.
            // Swap so white is treated as the first-solved face (white first, yellow last).
            InitializeFace(U, "Y");
            InitializeFace(D, "W");
            InitializeFace(L, "O");
            InitializeFace(R, "R");
            InitializeFace(F, "G");
            InitializeFace(B, "B");
        }

        private void InitializeFace(string[,] face, string color)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    face[i, j] = color;
        }

        public void PerformMove(string move)
        {
            switch (move)
            {
                case "R": RotateR(1); break;
                case "R'": RotateR(3); break;
                case "R2": RotateR(2); break;
                case "L": RotateL(1); break;
                case "L'": RotateL(3); break;
                case "L2": RotateL(2); break;
                case "U": RotateU(1); break;
                case "U'": RotateU(3); break;
                case "U2": RotateU(2); break;
                case "D": RotateD(1); break;
                case "D'": RotateD(3); break;
                case "D2": RotateD(2); break;
                case "F": RotateF(1); break;
                case "F'": RotateF(3); break;
                case "F2": RotateF(2); break;
                case "B": RotateB(1); break;
                case "B'": RotateB(3); break;
                case "B2": RotateB(2); break;
                case "M": RotateM(1); break;
                case "M'": RotateM(3); break;
                case "M2": RotateM(2); break;
            }
        }

        private void RotateR(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(R, true);
                string[] temp = { U[0, 2], U[1, 2], U[2, 2] };
                for (int j = 0; j < 3; j++) U[j, 2] = F[j, 2];
                for (int j = 0; j < 3; j++) F[j, 2] = D[j, 2];
                for (int j = 0; j < 3; j++) D[j, 2] = B[2 - j, 0];
                for (int j = 0; j < 3; j++) B[2 - j, 0] = temp[j];
            }
        }
        private void RotateL(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(L, true);
                string[] temp = { U[0, 0], U[1, 0], U[2, 0] };
                for (int j = 0; j < 3; j++) U[j, 0] = B[2 - j, 2];
                for (int j = 0; j < 3; j++) B[2 - j, 2] = D[j, 0];
                for (int j = 0; j < 3; j++) D[j, 0] = F[j, 0];
                for (int j = 0; j < 3; j++) F[j, 0] = temp[j];
            }
        }
        private void RotateU(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(U, true);
                string[] temp = { F[0, 0], F[0, 1], F[0, 2] };
                for (int j = 0; j < 3; j++) F[0, j] = R[0, j];
                for (int j = 0; j < 3; j++) R[0, j] = B[0, j];
                for (int j = 0; j < 3; j++) B[0, j] = L[0, j];
                for (int j = 0; j < 3; j++) L[0, j] = temp[j];
            }
        }
        private void RotateD(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(D, true);
                string[] temp = { F[2, 0], F[2, 1], F[2, 2] };
                for (int j = 0; j < 3; j++) F[2, j] = L[2, j];
                for (int j = 0; j < 3; j++) L[2, j] = B[2, j];
                for (int j = 0; j < 3; j++) B[2, j] = R[2, j];
                for (int j = 0; j < 3; j++) R[2, j] = temp[j];
            }
        }
        private void RotateF(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(F, true);
                string[] temp = { U[2, 0], U[2, 1], U[2, 2] };
                for (int j = 0; j < 3; j++) U[2, j] = L[2 - j, 2];
                for (int j = 0; j < 3; j++) L[j, 2] = D[0, j];
                for (int j = 0; j < 3; j++) D[0, j] = R[2 - j, 0];
                for (int j = 0; j < 3; j++) R[j, 0] = temp[j];
            }
        }
        private void RotateB(int times)
        {
            for (int i = 0; i < times; i++)
            {
                RotateFace(B, true);
                string[] temp = { U[0, 0], U[0, 1], U[0, 2] };
                for (int j = 0; j < 3; j++) U[0, j] = R[j, 2];
                for (int j = 0; j < 3; j++) R[j, 2] = D[2, 2 - j];
                for (int j = 0; j < 3; j++) D[2, j] = L[j, 0];
                for (int j = 0; j < 3; j++) L[2 - j, 0] = temp[j];
            }
        }
        
        /// <summary>
        /// Rotates the middle slice in the same direction as an R turn.
        /// </summary>
        private void RotateM(int times)
        {
            // The M move is defined as moving opposite to R (in the same direction as L).
            // However, to match the user request for an R-like animation, we perform an R-like logical move.
            // This is equivalent to M'. The cycle is F -> U -> B -> D.
            for (int i = 0; i < times; i++)
            {
                string[] temp = { U[0, 1], U[1, 1], U[2, 1] };
                for (int j = 0; j < 3; j++) U[j, 1] = F[j, 1];
                for (int j = 0; j < 3; j++) F[j, 1] = D[j, 1];
                for (int j = 0; j < 3; j++) D[j, 1] = B[2 - j, 1];
                for (int j = 0; j < 3; j++) B[2 - j, 1] = temp[j];
            }
        }


        private void RotateFace(string[,] face, bool clockwise)
        {
            string[,] temp = new string[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (clockwise)
                        temp[i, j] = face[2 - j, i];
                    else
                        temp[i, j] = face[j, 2 - i];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    face[i, j] = temp[i, j];
        }
    }
}