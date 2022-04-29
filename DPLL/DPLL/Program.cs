//TODO multi-threading
using System.Text;
//HeuristicsTest(Path.Combine(Environment.CurrentDirectory, @"CNF\", "Example2.dimacs"));

while (true)
{
    Console.WriteLine("Input dimacs absolute file path: ");
    var path = Console.ReadLine();
    if (path != null)
        HeuristicsTest(path);
    Console.WriteLine("-----------------");
}

static void HeuristicsTest(string path)
{
    foreach (Heuristics h in Enum.GetValues(typeof(Heuristics)))
    {
        Formula f = new();
        SatDpllSolver solver = new();

        f.ParseFormulaFromFile(path, false);
        Console.WriteLine("Heuristic: " + h.ToString());
        Console.WriteLine("Formula: [" + f.InputLiterals + "; " + f.InputClauses + "]");
        Console.WriteLine("Is satisfiable: " + solver.IsSatisfiable(f, h));
        Console.WriteLine("Recursive calls: " + $"{solver.RecursiveCalls:N0}");
        solver.ResetRecursiveCalls();
        Console.WriteLine("----------------------------------------------------");
    }
}

public enum Heuristics
{
    Dlis,
    Dlcs,
    Mom,
    Bohm,
    My
}

internal class SatDpllSolver
{
    public int RecursiveCalls { get; set; }

    public SatDpllSolver()
    {
        RecursiveCalls = 0;
    }

    public bool IsSatisfiable(Formula f, Heuristics h)
    {
        RecursiveCalls++;


        if (f.IsEmpty()) return true;
        if (f.HasEmptyClause) return false;

        var x = f.GetFirstSingletonClauseLiteral();
        if (x != null)
        {
            f.SetLiteral((int)x);
            return IsSatisfiable(f, h);
        }

        var y = f.GetFirstPureLiteral();
        if (y != null)
        {
            f.SetLiteral((int)y);
            return IsSatisfiable(f, h);
        }

        var literal = h switch
        {
            Heuristics.Dlis => f.Dlis(),
            Heuristics.Dlcs => f.Dlcs(),
            Heuristics.Mom => f.Mom(),
            Heuristics.Bohm => f.Bohm(),
            Heuristics.My => f.My(),
            _ => throw new Exception("Heuristic not implemented")
        };

        var f1 = f.Clone();
        f1.SetLiteral(-literal);
        if (IsSatisfiable(f1, h))
        {
            return true;
        }

        f.SetLiteral(literal);
        return IsSatisfiable(f, h);
    }

    public void ResetRecursiveCalls()
    {
        RecursiveCalls = 0;
    }
}

internal class Formula
{
    public Dictionary<int, int> AllLiterals { get; set; }

    public Dictionary<int, Dictionary<int, int>> Fk { get; set; }

    public Dictionary<int, HashSet<Clause>> CountClauses { get; set; }

    public int InputLiterals;
    public int InputClauses;
    public bool HasEmptyClause;
    public int ClauseCount;

    public Formula()
    {
        AllLiterals = new Dictionary<int, int>();
        CountClauses = new Dictionary<int, HashSet<Clause>>();
        Fk = new Dictionary<int, Dictionary<int, int>>();
    }

    public Formula(IEnumerable<Clause> clauses)
    {
        AllLiterals = new Dictionary<int, int>();
        CountClauses = new Dictionary<int, HashSet<Clause>>();
        Fk = new Dictionary<int, Dictionary<int, int>>();

        SetFormulaFromClauses(clauses);
    }

    public void ParseFormulaFromFile(string path, bool print)
    {
        try
        {
            using StreamReader s = new(path);
            string? line;

            while ((line = s.ReadLine()) != null)
            {
                line = line.Trim();
                line = line.Replace('\t', ' ');
                if (line.Length <= 0)
                    break;

                switch (line[0])
                {
                    case 'p':
                        var split = line.Split(" ");
                        InputLiterals = int.Parse(split[2]);
                        InputClauses = int.Parse(split[3]);
                        if (print)
                        {
                            Console.WriteLine("Literals count: " + split[2]);
                            Console.WriteLine("Formulas count: " + split[3]);
                        }

                        break;
                    case 'c':
                        if (print)
                            Console.WriteLine("Comment: " + line);
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        Clause newClause = new();
                        foreach (var num in line.Split(" "))
                        {
                            var number = int.Parse(num);

                            if (number == 0) continue;

                            if (newClause.Literals.Contains(number)) continue;

                            newClause.Literals.Add(number);
                            if (AllLiterals.ContainsKey(number))
                                AllLiterals[number]++;
                            else
                                AllLiterals[number] = 1;
                        }
                        //CountClauses
                        if (CountClauses.ContainsKey(newClause.Literals.Count))
                            CountClauses[newClause.Literals.Count].Add(newClause);
                        else
                        {
                            CountClauses[newClause.Literals.Count] = new HashSet<Clause> { newClause };
                        }
                        //FK
                        if (!Fk.ContainsKey(newClause.Literals.Count))
                            Fk[newClause.Literals.Count] = new Dictionary<int, int>();

                        foreach (var l in newClause.Literals)
                        {
                            if (Fk[newClause.Literals.Count].ContainsKey(l))
                                Fk[newClause.Literals.Count][l]++;
                            else
                                Fk[newClause.Literals.Count][l] = 1;
                        }

                        ClauseCount++;
                        break;
                    default:
                        Console.WriteLine(line);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            AllLiterals.Clear();
            CountClauses.Clear();
            Console.WriteLine("Error while parsing dimacs file");
            Console.WriteLine(ex.ToString());
        }
    }

    public void SetFormulaFromClauses(IEnumerable<Clause> clauses)
    {
        foreach (var c in clauses)
        {
            if (CountClauses.ContainsKey(c.Literals.Count))
                CountClauses[c.Literals.Count].Add(c);
            else
            {
                CountClauses[c.Literals.Count] = new HashSet<Clause> { c };
            }
            ClauseCount++;
        }


        foreach (var c in AllClauses())
        {
            if (!Fk.ContainsKey(c.Literals.Count))
                Fk[c.Literals.Count] = new Dictionary<int, int>();

            foreach (var l in c.Literals)
            {
                if (AllLiterals.ContainsKey(l))
                    AllLiterals[l]++;
                else
                    AllLiterals[l] = 1;

                if (Fk[c.Literals.Count].ContainsKey(l))
                    Fk[c.Literals.Count][l]++;
                else
                    Fk[c.Literals.Count][l] = 1;
            }
        }
    }

    public HashSet<Clause> AllClauses()
    {
        return CountClauses.Values.SelectMany(x => x).ToHashSet();
    }

    public bool IsEmpty()
    {
        return ClauseCount == 0;
    }

    public void SetLiteral(int literal)
    {
        if (AllLiterals[literal] <= 0)
        {
            Console.WriteLine("Warning! Setting literal that doesn't exists");
            return;
        }

        foreach (var c in AllClauses().Where(x => x.Literals.Contains(literal) || x.Literals.Contains(-literal)).ToList())
        {
            if (c.Literals.Contains(literal))
            {
                CountClauses[c.Literals.Count].Remove(c);
                ClauseCount--;
                foreach (var lit in c.Literals)
                {
                    AllLiterals[lit]--;
                    Fk[c.Literals.Count][lit]--;
                }
            }
            else
            {
                CountClauses[c.Literals.Count].Remove(c);

                if (!Fk.ContainsKey(c.Literals.Count - 1))
                    Fk[c.Literals.Count - 1] = new Dictionary<int, int>();

                foreach (var l in c.Literals)
                {
                    Fk[c.Literals.Count][l]--;

                    if (Fk[c.Literals.Count - 1].ContainsKey(l))
                        Fk[c.Literals.Count - 1][l]++;
                    else
                        Fk[c.Literals.Count - 1][l] = 1;

                }
                Fk[c.Literals.Count - 1][-literal]--;

                c.Literals.Remove(-literal);

                AllLiterals[-literal]--;

                if (c.Literals.Count == 0)
                    HasEmptyClause = true;

                if (CountClauses.ContainsKey(c.Literals.Count))
                    CountClauses[c.Literals.Count].Add(c);
                else
                    CountClauses[c.Literals.Count] = new HashSet<Clause> { c };
            }
        }
    }

    public int? GetFirstPureLiteral()
    {
        foreach (var l in AllLiterals.Keys.ToList())
        {
            if (!AllLiterals.ContainsKey(-l)) AllLiterals[-l] = 0;

            if (AllLiterals[l] > 0 && AllLiterals[-l] == 0)
                return l;
        }

        return null;
    }

    public int? GetFirstSingletonClauseLiteral()
    {
        int? res = 0;
        if (CountClauses.ContainsKey(1))
        {
            res = CountClauses[1].FirstOrDefault()?.Literals.First();
        }

        return res == 0 ? null : res;
    }

    private int GetShortestClause()
    {
        var shortest = CountClauses.Keys.First();

        foreach (var c in CountClauses.Keys.Where(c => CountClauses[c].Count > 0 && c < shortest))
        {
            shortest = c;
        }

        return shortest;
    }

    public int Dlis()
    {
        var maxKey = AllLiterals.Keys.First();

        foreach (var l in AllLiterals.Keys)
        {
            if (AllLiterals[l] > AllLiterals[maxKey])
                maxKey = l;
        }

        return -maxKey;
    }

    public int Dlcs()
    {
        var maxKey = AllLiterals.Keys.First();

        foreach (var l in AllLiterals.Keys)
        {
            if (AllLiterals[l] + AllLiterals[-l] > AllLiterals[maxKey] + AllLiterals[-maxKey])
                maxKey = l;
        }

        if (AllLiterals[maxKey] >= AllLiterals[-maxKey])
            return -maxKey;
        return maxKey;
    }

    public int Mom()
    {
        var p = AllLiterals.Keys.Count * AllLiterals.Keys.Count + 1;

        var shortest = GetShortestClause();
        var maxLiteral = 0;
        var maxLiteralValue = int.MinValue;

        foreach (var l in AllLiterals.Keys.Where(x => CountClauses[shortest].Any(c => c.Literals.Contains(x))))
        {
            var l1 = Fk[shortest][l];
            var l2 = Fk[shortest].ContainsKey(-l) ? Fk[shortest][-l] : 0;

            var num = (l1 + l2) * p + l1 * l2;
            if (num > maxLiteralValue)
            {
                maxLiteralValue = num;
                maxLiteral = l;
            }
        }

        return maxLiteral;
    }

    public int Bohm()
    {
        const int p1 = 1;
        const int p2 = 2;

        var maxLiteral = 0;
        var maxLiteralValue = int.MinValue;
        foreach (var l in AllLiterals.Keys)
        {
            var sum = 0;

            for (var i = GetShortestClause(); i <= CountClauses.Keys.Max(); i++)
            {
                if (CountClauses[i].Count <= 0) continue;

                var l1 = Fk[i].ContainsKey(l) ? Fk[i][l] : 0;
                var l2 = Fk[i].ContainsKey(-l) ? Fk[i][-l] : 0;
                sum += p1 * Math.Max(l1, l2) + p2 * Math.Min(l1, l2);
            }

            if (sum > maxLiteralValue)
            {
                maxLiteral = l;
                maxLiteralValue = sum;
            }
        }

        return maxLiteral;
    }

    public int My()
    {
        var maxLiteral = 0;
        var maxLiteralValue = 0;

        var longestClauses = CountClauses[GetShortestClause()];
        var literals = longestClauses.SelectMany(x => x.Literals).ToHashSet();

        foreach (var l in literals)
        {
            var count = longestClauses.Count(c => c.Literals.Contains(l));

            if (count > maxLiteralValue)
            {
                maxLiteral = l;
                maxLiteralValue = count;
            }
        }

        if (maxLiteral == 0)
            throw new Exception("My: maxLiteral == 0");
        return -maxLiteral;
    }

    public Formula Clone()
    {
        return new Formula(AllClauses().Select(c => c.Clone()).ToList());
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        var empty = true;

        sb.Append('(');

        foreach (var c in AllClauses())
        {
            if (empty)
                empty = false;
            sb.Append(c);
            sb.Append(" ^ ");
        }
        if (!empty)
            sb.Remove(sb.Length - 3, 3);

        sb.Append(')');

        return sb.ToString();
    }
}

internal class Clause
{
    public HashSet<int> Literals { get; set; }

    public Clause()
    {
        Literals = new HashSet<int>();
    }

    public Clause Clone()
    {
        return new Clause { Literals = new HashSet<int>(Literals) };
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('(');
        var empty = true;

        foreach (var l in Literals)
        {
            if (empty)
                empty = false;
            sb.Append(l);
            sb.Append(" V ");
        }

        if (!empty)
            sb.Remove(sb.Length - 3, 3);

        sb.Append(')');

        return sb.ToString();
    }
}