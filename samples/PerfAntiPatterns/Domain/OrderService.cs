using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PerfAntiPatterns.Infrastructure;
using RungerHeimdall;

namespace PerfAntiPatterns.Domain;

public struct PlainMoney
{
    public int Cents;
}

public sealed class OrderService
{
    private readonly SqlOrderRepository _repository = new SqlOrderRepository();

    public void ParamsArray() => Log("ready");

    public static void Log(string format, params object[] args)
    {
    }

    public int Count(IEnumerable<int> items)
    {
        var list = items.ToList();
        return list.Count();
    }

    public int ToListForeach(List<int> items)
    {
        var total = 0;
        foreach (var item in items.ToList())
        {
            total += item;
        }

        return total;
    }

    public string Concat(string[] items)
    {
        var result = string.Empty;
        foreach (var item in items)
        {
            result += item;
        }

        return result;
    }

    public void Closure(string[] items)
    {
        foreach (var item in items)
        {
            Action callback = () => Console.WriteLine(item);
            callback();
        }
    }

    [HotPath]
    public long Clock() => DateTime.Now.Ticks;

    public bool Flags(FileAttributes options) => options.HasFlag(FileAttributes.ReadOnly);

    public bool SameMoney(PlainMoney left, PlainMoney right)
        => EqualityComparer<PlainMoney>.Default.Equals(left, right);

    public int Parse(string value) => int.Parse(value.Substring(0, 2));

    public Regex Pattern() => new Regex("^a");

    public int NonGeneric(IEnumerable items)
    {
        var total = 0;
        foreach (int item in items)
        {
            total += item;
        }

        return total;
    }

    public bool Compare(string value) => value.ToLower() == "x";

    public int LinqLoop(List<int> items)
    {
        var total = 0;
        foreach (var item in items)
        {
            total += items.Where(x => x > 0).Count();
        }

        return total;
    }

    public async Task TaskRunAsync()
    {
        await Task.Run(() => Console.WriteLine("work"));
    }

    public async void AsyncVoid()
    {
        await Task.Delay(1);
    }

    public int Blocking(Task<int> task) => task.Result;

    public int DoubleEnumeration(IEnumerable<int> items)
    {
        var total = 0;
        foreach (var item in items)
        {
            total += item;
        }

        foreach (var item in items)
        {
            total += item;
        }

        return total;
    }

    public async Task<int> ValueTaskTwice(ValueTask<int> value)
    {
        var first = await value;
        var second = await value;
        return first + second;
    }

    public string Cached(int[] items)
    {
        var result = string.Empty;
        foreach (var item in items)
        {
            result += Task.FromResult(item).Result;
        }

        return result;
    }

    public void Resize(int[] items)
    {
        foreach (var item in items)
        {
            Array.Resize(ref items, items.Length + 1);
        }
    }

    public string Repository() => _repository.Name;
}