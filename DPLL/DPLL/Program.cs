﻿using System.Collections;
using System.Text;
ParserTest();
//StructuresTests();


void StructuresTests()
{
    Clause c1 = new();
    c1.Literals.Add(-1);
    c1.Literals.Add(1);

    Clause c2 = new();
    c2.Literals.Add(1);

    Console.WriteLine(c1.ToString());

    Formula f1 = new(new List<Clause>() { c1, c2 });
    Formula f2 = f1.Clone();

    Console.WriteLine(f1.ToString());

    Console.WriteLine("Formula: " + f1 + " IsEmpty()? " + f1.IsEmpty());
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause());

    Console.WriteLine("Set literal 5 to true for formula: " + f1);
    f1.SetLiteral(5);
    Console.WriteLine("Formula after evaluating literal 5 to true: " + f1);
    Console.WriteLine("Formula: " + f1 + " IsEmpty()? " + f1.IsEmpty());
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause());

    Console.WriteLine("Formula: " + f2 + " literals: ");
    f2.Literals.ForEach(l => Console.Write(l + ", "));
    Console.WriteLine();

    SatDpllSolver satDpllSolver = new();
    Console.WriteLine("Formula: " + f2 + " IsSatisfiable backtracking: " + satDpllSolver.IsSatisfiable(f2));

}

void ParserTest()
{
    Formula f = new();
    f.ParseFormulaFromFile(Path.Combine(Environment.CurrentDirectory, @"CNF\", "Example1.dimacs"));
    SatDpllSolver solver = new();
    Console.WriteLine("Is " +f +" satisfiable: " + solver.IsSatisfiable(f));
    
}

internal class SatDpllSolver
{
    public bool IsSatisfiable(Formula f)
    {
        //End conditions
        if (f.IsEmpty()) return true;
        if (f.HasEmptyClause()) return false;
        
        //Evaluates literals from clauses that contains only one literal
        var x = f.GetFirstSingletonClauseLiteral();
        if (x != null)
        {
            f.SetLiteral((int)x);
            return IsSatisfiable(f);
        }

        //Evaluates pure literals
        var y = f.GetFirstPureLiteral();
        if (y != null)
        {
            f.SetLiteral((int)y);
            return IsSatisfiable(f);
        }
        
        //Choose literal to evaluate
        var literal = ChooseLiteral(f);
        //var literal = Dlis(f);
        //var literal = Dlcs(f);

        //Evaluate literal to true
        var f1 = f.Clone();
        f1.SetLiteral(literal);

        if (IsSatisfiable(f1))
        {
            return true;
        }

        //Evaluate literal to false
        var f2 = f.Clone();
        f2.SetLiteral(-literal);

        return IsSatisfiable(f2);
    }

    private int ChooseLiteral(Formula f)
    {
        //Random literal
        return f.Literals[Random.Shared.Next(f.Literals.Count)];
    }

    private int Dlis(Formula f)
    {
        return f.MaxFrequentLiteral();
    }

    private int Dlcs(Formula f)
    {
        return f.MaxFrequentLiteralWithNegation();
    }
}

internal class Formula
{
    public List<Clause> Clauses { get; set; }

    public Dictionary<int, int> Frequency { get; set; }

    public List<int> Literals
    {
        get;
        set;
    }

    public Formula(IEnumerable<Clause> c)
    {
        Clauses = new List<Clause>();
        Literals = new List<int>();
        Frequency = new Dictionary<int, int>();

        Clauses.AddRange(c);
        Recalculate();
    }

    public Formula()
    {
        Clauses = new List<Clause>();
        Literals = new List<int>();
        Frequency = new Dictionary<int, int>();
        Recalculate();
    }

    public bool IsEmpty()
    {
        return Clauses.Count == 0;
    }

    public bool HasEmptyClause()
    {
        if (IsEmpty()) return false;
        return Clauses.All(c => c.Literals.Count == 0);
    }

    public void SetLiteral(int literal)
    {
        if (!Literals.Contains(literal))
            return;

        foreach (var c in Clauses.ToList())
        {
            if (c.Literals.Contains(literal))
            {
                Clauses.Remove(c);
            }
            else if (c.Literals.Contains(-literal))
            {
                c.Literals.Remove(-literal);
            }
        }

        Recalculate();
    }

    public Formula Clone()
    {
        return new Formula(Clauses.Select(c => c.Clone()).ToList());
    }

    public void Recalculate()
    {
        var literals = new List<int>();
        foreach (var clause in Clauses)
        {
            literals.AddRange(clause.Literals);
        }

        Literals.Clear();
        Literals.AddRange(literals.Distinct().ToList());
        
        foreach (var l in Literals)
        {
            Frequency[l] = Clauses.Count(x => x.Literals.Contains(l));
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        var empty = true;

        sb.Append('(');

        foreach (var c in Clauses)
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

    public void ParseFormulaFromFile(string path)
    {
        try
        {
            using StreamReader s = new(path);

            string? line;

            while ((line = s.ReadLine()) != null)
            {
                if (line.Length <= 0)
                    break;
                
                switch (line[0])
                {
                    case 'p':
                        var split = line.Split(" ");
                        Console.WriteLine("Literals count: " + split[2]);
                        Console.WriteLine("Formulas count: " + split[3]);
                        break;
                    case 'c':
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
                        Clause c = new();
                        foreach (var num in line.Split(" "))
                        {
                            if (num != "0")
                                c.Literals.Add(int.Parse(num));
                        }

                        Clauses.Add(c);
                        break;
                    default:
                        Console.WriteLine(line);
                        break;
                }
            }

            Recalculate();
        }
        catch (Exception ex)
        {
            Literals.Clear();
            Clauses.Clear();
            Console.WriteLine("Error while parsing file");
            Console.WriteLine(ex.ToString());
        }
    }

    public int? GetFirstPureLiteral()
    {
        foreach (var l in Literals)
        {
            if (!Literals.Contains(-l))
                return l;
        }

        return null;
    }

    public int? GetFirstSingletonClauseLiteral()
    {
        //O(n)
        foreach (var c in Clauses)
        {
            if (c.Literals.Count == 1)
                return c.Literals.Min();
        }

        return null;
    }

    //DLIS
    public int MaxFrequentLiteral()
    {
        int maxKey = Literals[0];
        foreach (var l in Literals)
        {
            if (Frequency[l] > Frequency[maxKey])
                maxKey = l;
        }

        return maxKey;
    }
    
    //DLCS
    public int MaxFrequentLiteralWithNegation()
    {
        var maxKey = Literals[0];

        foreach (var l in Literals)
        {
            if (Frequency[l] + Frequency[-l] > Frequency[maxKey] + Frequency[-maxKey])
                maxKey = l;
        }

        if (Frequency[-maxKey] > Frequency[maxKey])
            return -maxKey;        
        return maxKey;
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