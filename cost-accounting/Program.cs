using System.Text;

namespace CostAccounting;

interface IHasAmount
{
    decimal Amount { get; }
}

// primary constructor usage
public class BudgetExceededEventArgs<T>(T item, decimal totalAmount, decimal budget) : EventArgs
{
    public T Item { get; } = item;
    public decimal TotalAmount { get; } = totalAmount;
    public decimal Budget { get; } = budget;
}

// primary constructor usage
class Expense(string category, string shop, decimal amount) : IHasAmount
{
    public string Category { get; } = category;
    public string Shop { get; } = shop;
    public DateTime Date { get; } = DateTime.Now;
    public decimal Amount { get; } = amount;
}

class ExpenseLog<T>(decimal budget) where T : IHasAmount
{
    private readonly decimal _budget = budget;
    private readonly List<T> _items = [];
    private Func<T, string>? _grouping;
    private readonly string _defaultGrouping = "Усього";

    public event EventHandler<BudgetExceededEventArgs<T>>? BudgetExceeded;

    protected virtual void OnBudgetExceeded(BudgetExceededEventArgs<T> e) => BudgetExceeded?.Invoke(this, e);

    public void Add(T item)
    {
        _items.Add(item);

        decimal total = GetTotal();
        if (total > _budget)
        {
            OnBudgetExceeded(new BudgetExceededEventArgs<T>(item, GetTotal(), _budget));
        }
    }

    public decimal GetTotal() => _items.Sum(item => item.Amount);
    public decimal GetBudget() => _budget;

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
    private static readonly List<Expense> _excess = [];

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        const decimal budget = 25_000m;
        var log = new ExpenseLog<Expense>(budget);

        log.Add(new Expense("Tech", "Rozetka", 10_000m));
        log.Add(new Expense("Food", "Silpo", 35m));
        log.Add(new Expense("Tech", "Foxtrot", 5_000m));

        uint addedThisSession = 0;

        log.BudgetExceeded += (sender, e) => addedThisSession++;
        log.BudgetExceeded += LogExpenseExceeded;
        log.BudgetExceeded += AddExcessiveExpense;

        // Controls
        // * Up/Down
        // * Enter
        // mark chosen option with (*)

        int menuItemIndex = 0;
        int minMenuItemIndex = 0;
        int maxMenuItemIndex = 5;

        bool isRunning = true;
        while (isRunning)
        {
            ShowMenu(menuItemIndex);

            var input = Console.ReadKey(true);

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

        Console.WriteLine($"У підсумку {addedThisSession} витрати, які перевищили бюджет.");
    }

    private static void AddExcessiveExpense(object? sender, BudgetExceededEventArgs<Expense> e)
        => _excess.Add(e.Item);

    private static void LogExpenseExceeded(object? sender, BudgetExceededEventArgs<Expense> e)
    {
        Console.WriteLine($"[подія] Витрата \"{e.Item.Category} ({e.Item.Amount})₴\" перевищила бюджет.");
        Console.WriteLine($"        Поточні витрати: {e.TotalAmount}₴");
        AwaitAnyKeyToProceed();
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

    private static void ShowGroupingOptions(int index)
    {
        Console.WriteLine("\n\t<<<< Спосіб групування витрат >>>>");
        Console.WriteLine($"({(index == 0 ? '*' : ' ')}) За замовчуванням");
        Console.WriteLine($"({(index == 1 ? '*' : ' ')}) За категорією");
        Console.WriteLine($"({(index == 2 ? '*' : ' ')}) За магазином");
        Console.WriteLine($"({(index == 3 ? '*' : ' ')}) За місяцем");
    }

    private static void AwaitAnyKeyToProceed()
    {
        Console.Write("Натисніть будь-яку клавішу щоб продовжити ...");
        _ = Console.ReadKey(true);
    }

    private static bool HandleMenuChoice(int index, ExpenseLog<Expense> log)
        => index switch
        {
            0 => false,
            1 => HandleAdd(log),
            2 => HandleShowAllExpenses(log),
            3 => HandleGetReport(log),
            4 => HandleChangeGrouping(log),
            5 => HandleShowExcessiveExpenses(log),
            _ => true
        };

    private static bool HandleShowExcessiveExpenses(ExpenseLog<Expense> log)
    {
        if (_excess.Count > 0)
        {
            Console.WriteLine($"Ці витрати перевищили допустимий ({log.GetBudget()}₴) бюджет.");
            foreach (Expense expense in _excess)
            {
                Console.WriteLine($"\t({expense.Date}) {expense.Category} - {expense.Shop} - {expense.Amount}₴");
            }
        }
        else
        {
            Console.WriteLine($"Поки немає витрат, що перевищують допустимий ({log.GetBudget()}₴) бюджет.");
        }

        AwaitAnyKeyToProceed();
        return true;
    }

    private static bool HandleChangeGrouping(ExpenseLog<Expense> log)
    {
        int menuItemIndex = 0;
        int minMenuItemIndex = 0;
        int maxMenuItemIndex = 3;

        bool isRunning = true;

        while (isRunning)
        {
            ShowGroupingOptions(menuItemIndex);

            var input = Console.ReadKey(true);

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
                    isRunning = HandleGroupChoice(menuItemIndex, log);
                    break;
                default:
                    break;
            }

            Console.Clear();
        }

        return true;
    }

    private static bool HandleGroupChoice(int index, ExpenseLog<Expense> log)
    {
        log.SetGrouping(index switch
        {
            1 => expense => expense.Category,
            2 => expense => expense.Shop,
            3 => expense => expense.Date.ToString("yyyy-MM"),
            _ => null
        });

        return false;
    }

    private static bool HandleGetReport(ExpenseLog<Expense> log)
    {
        Console.WriteLine("Звіт з витратності:");
        foreach (var record in log.GetReport())
        {
            Console.WriteLine($"\t{record.Key} - {record.Value}₴");
        }

        AwaitAnyKeyToProceed();
        return true;
    }

    private static bool HandleShowAllExpenses(ExpenseLog<Expense> log)
    {
        Action<Expense> print =
            expense => Console.WriteLine($"\t({expense.Date}) {expense.Category} - {expense.Shop} - {expense.Amount}₴");
        log.ForEach(print);

        AwaitAnyKeyToProceed();
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