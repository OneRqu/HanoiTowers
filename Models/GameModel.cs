using System.Collections.Generic;

namespace HanoiTowers.Models
{
    public struct Move
    {
        public int From, To;
        public Move(int from, int to) { From = from; To = to; }
    }

    public class GameModel
    {
        public const int TargetPeg = 2;               // C — целевой стержень
        public int DiskCount { get; private set; }
        public int MoveCount { get; private set; }
        public Stack<Move> History { get; private set; }
        public static long MinMoves(int n) { return (1L << n) - 1; }

        // pegs[0]=A (старт), pegs[1]=B (вспомогательный), pegs[2]=C (цель).
        // Храним РАЗМЕРЫ дисков, снизу вверх.
        public List<int>[] Pegs { get; private set; }

        public GameModel(int diskCount) { Restart(diskCount); }

        public void Restart(int diskCount)
        {
            DiskCount = diskCount;
            MoveCount = 0;
            History = new Stack<Move>();
            Pegs = new List<int>[3];
            for (int i = 0; i < 3; i++) Pegs[i] = new List<int>();
            for (int d = diskCount; d >= 1; d--) Pegs[0].Add(d);
        }

        public bool IsSolved { get { return Pegs[TargetPeg].Count == DiskCount; } }

        public int PegOf(int disk)
        {
            for (int p = 0; p < 3; p++)
                if (Pegs[p].Contains(disk)) return p;
            return -1;
        }

        public bool CanMove(int from, int to)
        {
            if (from == to || from < 0 || from > 2 || to < 0 || to > 2) return false;
            if (Pegs[from].Count == 0) return false;
            int disk = Pegs[from][Pegs[from].Count - 1];
            if (Pegs[to].Count > 0 && Pegs[to][Pegs[to].Count - 1] < disk) return false;
            return true;
        }

        public bool TryMove(int from, int to)
        {
            if (!CanMove(from, to)) return false;
            int disk = Pegs[from][Pegs[from].Count - 1];
            Pegs[from].RemoveAt(Pegs[from].Count - 1);
            Pegs[to].Add(disk);
            History.Push(new Move(from, to));
            MoveCount++;
            return true;
        }

        public bool Undo()
        {
            if (History.Count == 0) return false;
            Move m = History.Pop();
            int disk = Pegs[m.To][Pegs[m.To].Count - 1];
            Pegs[m.To].RemoveAt(Pegs[m.To].Count - 1);
            Pegs[m.From].Add(disk);
            MoveCount--;
            return true;
        }

        // ---------- Оптимальное решение из ТЕКУЩЕЙ позиции ----------

        public List<Move> BuildSolution()
        {
            List<int>[] sim = new List<int>[3];       // симуляция на копии
            for (int i = 0; i < 3; i++) sim[i] = new List<int>(Pegs[i]);
            List<Move> moves = new List<Move>();
            Solve(sim, DiskCount, TargetPeg, moves);
            return moves;
        }

        // ---------- Доп. задание: то же самое, но без рекурсии — через явный Стек ----------

        // Задача для стека: либо «подзадача» (перенести башню), либо конкретный ход
        private struct SolveTask
        {
            public int Disk, From, Target;
            public bool IsMove;   // true — перенести кольцо Disk со стержня From на Target

            public static SolveTask Sub(int disk, int target)
            {
                return new SolveTask { Disk = disk, Target = target, IsMove = false };
            }
            public static SolveTask Move(int disk, int from, int target)
            {
                return new SolveTask { Disk = disk, From = from, Target = target, IsMove = true };
            }
        }

        public List<Move> BuildSolutionWithStack()
        {
            List<int>[] sim = new List<int>[3];
            for (int i = 0; i < 3; i++) sim[i] = new List<int>(Pegs[i]);
            List<Move> moves = new List<Move>();

            // Явный стек задач — заменяет стек вызовов рекурсии
            Stack<SolveTask> stack = new Stack<SolveTask>();
            stack.Push(SolveTask.Sub(DiskCount, TargetPeg));

            while (stack.Count > 0)
            {
                SolveTask t = stack.Pop();

                if (t.IsMove)                       // конкретный ход: переносим кольцо
                {
                    sim[t.From].Remove(t.Disk);
                    sim[t.Target].Add(t.Disk);
                    moves.Add(new Move(t.From, t.Target));
                    continue;
                }

                if (t.Disk == 0) continue;          // базовый случай (аналог «if (disk == 0) return»)

                int from = -1;                      // где сейчас этот диск
                for (int p = 0; p < 3; p++)
                    if (sim[p].Contains(t.Disk)) { from = p; break; }

                if (from == t.Target)               // уже на месте — разбираемся с меньшими
                {
                    stack.Push(SolveTask.Sub(t.Disk - 1, t.Target));
                    continue;
                }

                int aux = 3 - from - t.Target;
                // Стек — LIFO, поэтому кладём в ОБРАТНОМ порядке:
                stack.Push(SolveTask.Sub(t.Disk - 1, t.Target));    // выполнится третьим
                stack.Push(SolveTask.Move(t.Disk, from, t.Target)); // вторым
                stack.Push(SolveTask.Sub(t.Disk - 1, aux));         // первым
            }

            return moves;
        }

        private static void Solve(List<int>[] pegs, int disk, int target, List<Move> moves)
        {
            if (disk == 0) return;
            int from = -1;
            for (int p = 0; p < 3; p++)
                if (pegs[p].Contains(disk)) { from = p; break; }

            if (from == target)                       // уже на месте — разбираемся с меньшими
            {
                Solve(pegs, disk - 1, target, moves);
                return;
            }
            int aux = 3 - from - target;              // третий стержень (0+1+2=3)
            Solve(pegs, disk - 1, aux, moves);        // освободить диск
            moves.Add(new Move(from, target));
            pegs[from].Remove(disk);
            pegs[target].Add(disk);
            Solve(pegs, disk - 1, target, moves);     // водрузить башню сверху
        }
    }
}