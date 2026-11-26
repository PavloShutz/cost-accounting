using System.Text;

namespace cost_accounting;

interface IHasAmount
{
    decimal Amount { get; }
}

// primary constructor usage
class Expense(string category, string shop, decimal amount) : IHasAmount
{
    public string Category { get; } = category;
    public string Shop { get; } = shop;
    public DateTime Date { get; } = DateTime.Now;
    public decimal Amount { get; } = amount;
}

class ExpenseLog<T> where T : IHasAmount
{
    private readonly List<T> _items = [];
    private Func<T, string>? _grouping;
    private readonly string _defaultGrouping = "All";

    public void Add(T item) => _items.Add(item);

    public decimal GetTotal() => _items.Sum(item => item.Amount);

    public Dictionary<string, decimal> GetReport()
    {
        var report = new Dictionary<string, decimal>();

        foreach (T item in _items)
        {
            string key = Group(item);
            report.TryAdd(key, 0m); // good alternative to try..catch..finally
            report[key] += item.Amount;
        }

        return report;
    }

    public void SetGrouping(Func<T, string>? grouping) => _grouping = grouping;

    private string Group(T item) => _grouping?.Invoke(item) ?? _defaultGrouping;

    public void ForEach(Action<T> action)
    {
        foreach (T item in _items)
        {
            action(item);
        }
    }

}

static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var log = new ExpenseLog<Expense>();

        log.Add(new Expense("Tech", "Rozetka", 15_000m));
        log.Add(new Expense("Food", "Silpo", 35m));
        log.Add(new Expense("Tech", "Foxtrot", 5_000m));

        bool isRunning = true;

        // Controls
        // * Up/Down
        // * Enter
        // mark chosen point with (*)

        int menuItemIndex = 0;
        int minMenuItemIndex = 0;
        int maxMenuItemIndex = 5;

        while (isRunning)
        {
            ShowMenu(menuItemIndex);

            var input = Console.ReadKey();

            switch (input.Key)
            {
                case ConsoleKey.UpArrow:
                    if (menuItemIndex > minMenuItemIndex)
                        --menuItemIndex;
                    break;
                case ConsoleKey.DownArrow:
                    if (menuItemIndex < maxMenuItemIndex)
                        ++menuItemIndex;
                    break;
                case ConsoleKey.Enter:
                    Console.Clear();
                    isRunning = HandleMenuChoice(menuItemIndex, log);
                    break;
                default:
                    break;
            }

            Console.Clear();
        }

        //log.SetGrouping(e => e.Category);

        //Console.WriteLine("Expenses report:");
        //foreach(var record in log.GetReport())
        //{
        //    Console.WriteLine($"\t{record.Key} - {record.Value}₴");
        //}
    }

    private static void ShowMenu(int index)
    {
        Console.WriteLine("\n\t<<<< Меню >>>>");
        Console.WriteLine($"({(index == 0 ? '*' : ' ')}) Вийти");
        Console.WriteLine($"({(index == 1 ? '*' : ' ')}) Додати витрату");
        Console.WriteLine($"({(index == 2 ? '*' : ' ')}) Показати всі витрати");
        Console.WriteLine($"({(index == 3 ? '*' : ' ')}) Звіт з витратності");
        Console.WriteLine($"({(index == 4 ? '*' : ' ')}) Змінити групування");
        Console.WriteLine($"({(index == 5 ? '*' : ' ')}) Показати витрати, що перевищують бюджет");
    }

    private static void AwaitKeyToProceed()
    {
        Console.Write("Натисніть будь-яку клавішу щоб продовжити ...");
        _ = Console.ReadKey();
    }

    private static bool HandleMenuChoice(int index, ExpenseLog<Expense> log)
        => index switch
        {
            0 => false,
            1 => HandleAdd(log),
            2 => HandleShowAllExpenses(log),
            3 => HandleGetReport(log),
            4 => HandleChangeGrouping(log),
            5 => HandleShowExpensesExceedingBudget(log),
            _ => true
        };

    private static bool HandleShowExpensesExceedingBudget(ExpenseLog<Expense> log)
    {
        throw new NotImplementedException();
    }

    private static bool HandleChangeGrouping(ExpenseLog<Expense> log)
    {
        throw new NotImplementedException();
    }

    private static bool HandleGetReport(ExpenseLog<Expense> log)
    {
        throw new NotImplementedException();
    }

    private static bool HandleShowAllExpenses(ExpenseLog<Expense> log)
    {
        Action<Expense> print =
            expense => Console.WriteLine($"\t> {expense.Category} - {expense.Shop} - {expense.Amount}₴");
        log.ForEach(print);
        
        AwaitKeyToProceed();
        return true;
    }

    private static bool HandleAdd(ExpenseLog<Expense> log)
    {
        string category = ReadNonEmpty("Категорія: ");
        string shop = ReadNonEmpty("Магазин: ");
        decimal amount = ReadDecimal("Ціна: ");

        var expense = new Expense(category, shop, amount);
        log.Add(expense);

        return true;
    }

    private static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine("Поле не може бути порожнім!");
        }
    }

    private static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (decimal.TryParse(input, out decimal value))
            {
                return value;
            }

            Console.WriteLine($"Некоректне значення!");
        }
    }
}