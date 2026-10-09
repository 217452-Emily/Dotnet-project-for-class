var numbers = new List<int> { 1, 2, 3 };

var query = numbers.Where(n =>
{
    Console.WriteLine($"Check {n}");
    return n > 1;
});

Console.WriteLine("Query defined");
numbers.Add(4);

foreach (var n in query)
{
    Console.WriteLine($"Result {n}");
}
