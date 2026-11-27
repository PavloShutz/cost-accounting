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
    private readonly List<T> _items = [];
    private Func<T, string>? _grouping;
    private const string _defaultGrouping = "Усього";

    public event EventHandler<BudgetExceededEventArgs<T>>? BudgetExceeded;

    protected virtual void OnBudgetExceeded(BudgetExceededEventArgs<T> e) => BudgetExceeded?.Invoke(this, e);

    public void Add(T item)
    {
        _items.Add(item);

        decimal total = GetTotal();
        if (total > Budget)
        {
            OnBudgetExceeded(new BudgetExceededEventArgs<T>(item, total, Budget));
        }
    }

    public decimal GetTotal() => _items.Sum(item => item.Amount);
    public decimal Budget { get; } = budget;

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
    private static readonly List<Expense> _overBudgetExpenses = [];

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        const decimal budget = 25_000m;
        var log = new ExpenseLog<Expense>(budget);

        uint overBudgetCount = 0;

        log.BudgetExceeded += (sender, e) => overBudgetCount++;
        log.BudgetExceeded += LogExpenseExceeded;
        log.BudgetExceeded += AddExcessiveExpense;

        log.Add(new Expense("Tech", "Rozetka", 10_000m));
        log.Add(new Expense("Food", "Silpo", 35m));
        log.Add(new Expense("Tech", "Foxtrot", 5_000m));

        // Controls
        // * Up/Down
        // * Enter
        // mark chosen option with (*)

        string[] menuOptions =
        [
            "Вийти",
            "Додати витрату",
            "Показати всі витрати",
            "Звіт з витратності",
            "Змінити групування",
            "Показати витрати, що перевищують бюджет"
        ];

        int menuItemIndex = 0;

        while (true)
        {
            menuItemIndex = SelectOption("Меню", menuOptions, menuItemIndex);

            if (menuItemIndex == 0)
                break;

            HandleMenuChoice(menuItemIndex, log);
        }

        Console.WriteLine($"Витрат понад бюджет за сеанс: {overBudgetCount}");
    }

    private static void AddExcessiveExpense(object? sender, BudgetExceededEventArgs<Expense> e)
        => _overBudgetExpenses.Add(e.Item);

    private static void LogExpenseExceeded(object? sender, BudgetExceededEventArgs<Expense> e)
    {
        Console.WriteLine($"[подія] Витрата \"{e.Item.Category} ({e.Item.Amount:F2}₴)\" перевищила бюджет.");
        Console.WriteLine($"        Поточні витрати: {e.TotalAmount:F2}₴");
        AwaitAnyKeyToProceed();
    }

    private static int SelectOption(string title, string[] options, int index = 0)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"\n\t<<<< {title} >>>>");

            for (int i = 0; i < options.Length; i++)
                Console.WriteLine($"({(i == index ? '*' : ' ')}) {options[i]}");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.UpArrow when index > 0:
                    index--;
                    break;
                case ConsoleKey.DownArrow when index < options.Length - 1:
                    index++;
                    break;
                case ConsoleKey.Enter:
                    Console.Clear();
                    return index;
            }
        }
    }

    private static void AwaitAnyKeyToProceed()
    {
        Console.Write("\nНатисніть будь-яку клавішу щоб продовжити ...");
        _ = Console.ReadKey(true);
    }

    private static void HandleMenuChoice(int index, ExpenseLog<Expense> log)
    {
        switch (index)
        {
            case 1:
                HandleAdd(log);
                break;
            case 2:
                HandleShowAllExpenses(log);
                break;
            case 3:
                HandleGetReport(log);
                break;
            case 4:
                HandleChangeGrouping(log);
                break;
            case 5:
                HandleShowExcessiveExpenses(log);
                break;
        }
    }

    private static void HandleShowExcessiveExpenses(ExpenseLog<Expense> log)
    {
        if (_overBudgetExpenses.Count > 0)
        {
            Console.WriteLine($"Ці витрати перевищили допустимий ({log.Budget:F2}₴) бюджет.");
            foreach (Expense expense in _overBudgetExpenses)
            {
                Console.WriteLine($"\t({expense.Date}) {expense.Category} - {expense.Shop} - {expense.Amount:F2}₴");
            }
        }
        else
        {
            Console.WriteLine($"Поки немає витрат, що перевищують допустимий ({log.Budget:F2}₴) бюджет.");
        }

        AwaitAnyKeyToProceed();
    }

    private static void HandleChangeGrouping(ExpenseLog<Expense> log)
    {
        string[] groupingOptions =
        [
            "Без групування",
            "За категорією",
            "За магазином",
            "За місяцем"
        ];

        int index = SelectOption("Спосіб групування витрат", groupingOptions);
        HandleGroupChoice(index, log);
    }

    private static void HandleGroupChoice(int index, ExpenseLog<Expense> log)
    {
        log.SetGrouping(index switch
        {
            1 => expense => expense.Category,
            2 => expense => expense.Shop,
            3 => expense => expense.Date.ToString("yyyy-MM"),
            _ => null
        });
    }

    private static void HandleGetReport(ExpenseLog<Expense> log)
    {
        var report = log.GetReport();

        if (report.Count > 0)
        {
            Console.WriteLine("Звіт з витратності:");
            foreach (var record in report)
            {
                Console.WriteLine($"\t{record.Key} - {record.Value:F2}₴");
            }

            Console.WriteLine($"Загальна сума: {log.GetTotal():F2}₴");
        }
        else
        {
            Console.WriteLine("Поки немає витрат.");
        }

        AwaitAnyKeyToProceed();
    }

    private static void HandleShowAllExpenses(ExpenseLog<Expense> log)
    {
        bool hasExpenses = false;

        Action<Expense> print =
            expense =>
            {
                hasExpenses = true;
                Console.WriteLine($"\t({expense.Date}) {expense.Category} - {expense.Shop} - {expense.Amount:F2}₴");
            };
        log.ForEach(print);

        if (!hasExpenses)
            Console.WriteLine("Поки немає витрат.");

        AwaitAnyKeyToProceed();
    }

    private static void HandleAdd(ExpenseLog<Expense> log)
    {
        string category = ReadNonEmpty("Категорія: ");
        string shop = ReadNonEmpty("Магазин: ");
        decimal amount = ReadDecimal("Ціна: ");

        var expense = new Expense(category, shop, amount);
        log.Add(expense);
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

            if (decimal.TryParse(input, out decimal value) && value > 0)
            {
                return value;
            }

            Console.WriteLine("Некоректне значення!");
        }
    }
}