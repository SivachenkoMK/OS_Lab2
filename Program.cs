using Microsoft.Extensions.Configuration;

namespace DiningPhilosophers;

public static class Dining
{
    public static SemaphoreSlim Table = default!;
    public static SemaphoreSlim[] Forks = default!;

    public static void Arrange(int amount)
    {
        Table = new SemaphoreSlim(amount == 1 ? amount : amount - 1); // Limit to N-1 philosophers if it's not 1 philosopher
        Forks = new SemaphoreSlim[amount == 1 ? 2 : amount].Select(_ => new SemaphoreSlim(1)).ToArray(); // Arrange with N forks, if it's not 1 philosopher
    }

    public static void Start(int amount)
    {
        var philosophers = new Philosopher[amount];
        var threads = new Thread[amount];

        for (var i = 0; i < amount; i++)
        {
            philosophers[i] = new Philosopher(i);
            threads[i] = new Thread(philosophers[i].Dine);
            threads[i].Start();
        }
    }
}

public class Philosopher(int index)
{
    private int _noodlesOnThePlate;

    public void Dine()
    {
        while (true)
        {
            Think();
            Eat();
        }
    }

    private void Think()
    {
        Console.WriteLine($"{index}: Philosophizes about why implementation in C# costs less than in C...");
        Thread.Sleep(new Random().Next(1000, 2000));
    }

    private void Eat()
    {
        Dining.Table.Wait(); // Wait for a seat at the table
        Dining.Forks[index].Wait(); // Picks up left fork
        Dining.Forks[(index + 1) % Dining.Forks.Length].Wait(); // Picks up right fork

        if (_noodlesOnThePlate == 0)
            _noodlesOnThePlate = new Random().Next(10000, 20000); // Total time it would take to get the plate of noodles done

        var emptySpaceInStomach = new Random().Next(_noodlesOnThePlate > 1000 ? 1000 : _noodlesOnThePlate,
            _noodlesOnThePlate > 2000 ? 2000 : _noodlesOnThePlate); // How much space there is in the stomach of a philosopher

        Console.WriteLine($"{index}: Eating. Noodles on a plate: {_noodlesOnThePlate}. Noodles to fit in stomach: {emptySpaceInStomach}");
        Thread.Sleep(emptySpaceInStomach);

        _noodlesOnThePlate -= emptySpaceInStomach;
        Console.WriteLine(_noodlesOnThePlate == 0
            ? $"{index}: Finished eating. I'll leave this place for someone hungry to think."
            : $"{index}: Will finish eating later. Time to take a promenade and return in a while.");

        Dining.Forks[index].Release(); // Put down left fork
        Dining.Forks[(index + 1) % Dining.Forks.Length].Release(); // Put down right fork
        Dining.Table.Release(); // Leave the table
    }
}

public static class Program
{
    public static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        if (!int.TryParse(configuration.GetSection("philosophers").Value, out var amount))
            amount = 1;
        
        Dining.Arrange(amount);
        Dining.Start(amount);
    }
}