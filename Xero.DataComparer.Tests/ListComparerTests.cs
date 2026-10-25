using System.ComponentModel;
using Xero.DataComparer.Core;

namespace Xero.DataComparer.Tests;

public class ListComparerTests
{
    public sealed class Trade
    {
        public string Id { get; set; } = "";
        public string Book { get; set; } = "";
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? Comment { get; set; }
        public DateTime Updated { get; set; }
    }

    static Trade T(string id, double price = 1, int quantity = 1, string book = "A", string? comment = null, DateTime updated = default) =>
        new() { Id = id, Book = book, Price = price, Quantity = quantity, Comment = comment, Updated = updated };

    static ListComparer<Trade> ById(params string[] ignored) => new(["Id"], [.. ignored]);

    [Fact]
    public async Task Splits_the_items_into_only_in_reference_only_in_target_and_differing()
    {
        var reference = new List<Trade> { T("1"), T("2"), T("3", price: 10) };
        var target = new List<Trade> { T("2"), T("3", price: 12), T("4") };

        var result = await ById().CompareListAsync(reference, target);

        Assert.Equal("1", Assert.Single(result.OnlyInReference).Id);
        Assert.Equal("4", Assert.Single(result.OnlyInTarget).Id);
        var diff = Assert.Single(result);
        Assert.Equal("3", diff["Key_Id"]);
        Assert.Equal(10d, diff["Reference_Price"]);
        Assert.Equal(12d, diff["Target_Price"]);
        Assert.Empty(result.DuplicatesInReference);
        Assert.Empty(result.DuplicatesInTarget);
    }

    [Fact]
    public async Task A_diff_row_holds_only_the_properties_that_differ_and_the_compared_items()
    {
        var reference = new List<Trade> { T("1", price: 1, quantity: 5) };
        var target = new List<Trade> { T("1", price: 1, quantity: 6) };

        var diff = Assert.Single(await ById().CompareListAsync(reference, target));

        Assert.Equal(["Reference_Quantity", "Target_Quantity", "Key_Id", "ComparedItems"], diff.Keys);
        var couple = Assert.IsType<CoupleItem<Trade>>(diff["ComparedItems"]);
        Assert.Same(reference[0], couple.ReferenceItem);
        Assert.Same(target[0], couple.TargetItem);
    }

    [Fact]
    public async Task Ignored_properties_are_not_compared()
    {
        var reference = new List<Trade> { T("1", updated: new DateTime(2026, 1, 1)) };
        var target = new List<Trade> { T("1", updated: new DateTime(2026, 1, 2)) };

        Assert.Single(await ById().CompareListAsync(reference, target));
        Assert.Empty(await ById(nameof(Trade.Updated)).CompareListAsync(reference, target));
    }

    [Fact]
    public async Task Items_are_matched_on_all_the_key_properties()
    {
        var comparer = new ListComparer<Trade>(["Id", "Book"], []);
        var reference = new List<Trade> { T("1", book: "A"), T("1", book: "B") };
        var target = new List<Trade> { T("1", book: "B", price: 2) };

        var result = await comparer.CompareListAsync(reference, target);

        Assert.Equal("A", Assert.Single(result.OnlyInReference).Book);
        Assert.Equal("B", Assert.Single(result)["Key_Book"]);
        Assert.Empty(result.DuplicatesInReference);
    }

    [Fact]
    public async Task Null_values_are_compared()
    {
        var reference = new List<Trade> { T("1", comment: null), T("2", comment: null) };
        var target = new List<Trade> { T("1", comment: "x"), T("2", comment: null) };

        var diff = Assert.Single(await ById().CompareListAsync(reference, target));

        Assert.Null(diff["Reference_Comment"]);
        Assert.Equal("x", diff["Target_Comment"]);
    }

    [Fact]
    public async Task Duplicate_keys_are_reported_not_lost()
    {
        var reference = new List<Trade> { T("1", price: 1), T("1", price: 2), T("2") };
        var target = new List<Trade> { T("1", price: 1), T("2"), T("2", price: 3) };

        var result = await ById().CompareListAsync(reference, target);

        Assert.Equal(2d, Assert.Single(result.DuplicatesInReference).Price);
        Assert.Equal(3d, Assert.Single(result.DuplicatesInTarget).Price);
        Assert.Empty(result);
        Assert.Empty(result.OnlyInReference);
        Assert.Empty(result.OnlyInTarget);
    }

    [Fact]
    public async Task Empty_lists_give_an_empty_result()
    {
        var result = await ById().CompareListAsync([], []);

        Assert.Empty(result);
        Assert.Empty(result.OnlyInReference);
        Assert.Empty(result.OnlyInTarget);
    }

    [Fact]
    public void Keys_are_required_and_must_exist()
    {
        Assert.Throws<ArgumentException>(() => new ListComparer<Trade>([], []));
        Assert.Throws<ArgumentException>(() => new ListComparer<Trade>(["Missing"], []));
    }

    [Fact]
    public async Task The_typed_list_exposes_every_column_whatever_the_rows_found()
    {
        // Each pair differs on another property: both must still be columns.
        var reference = new List<Trade> { T("1", price: 1, quantity: 1), T("2", price: 1, quantity: 1) };
        var target = new List<Trade> { T("1", price: 2, quantity: 1), T("2", price: 1, quantity: 2) };

        var result = await ById(nameof(Trade.Updated)).CompareListAsync(reference, target);
        var columns = ((ITypedList)result).GetItemProperties(null).Cast<PropertyDescriptor>().ToDictionary(c => c.Name);

        Assert.Equal(
            ["Key_Id", "Reference_Book", "Target_Book", "Reference_Price", "Target_Price", "Reference_Quantity", "Target_Quantity",
             "Reference_Comment", "Target_Comment", "ComparedItems"],
            columns.Keys);
        Assert.Equal(typeof(double), columns["Reference_Price"].PropertyType);
        Assert.Equal(typeof(int), columns["Target_Quantity"].PropertyType);
        Assert.Equal(typeof(string), columns["Reference_Comment"].PropertyType);
        Assert.Equal(typeof(CoupleItem<Trade>), columns["ComparedItems"].PropertyType);
        Assert.Equal(columns.Keys, result.ColumnTypes.Keys);
    }

    [Fact]
    public async Task The_columns_are_read_only_and_safe_for_data_binding()
    {
        var result = await ById().CompareListAsync([T("1", price: 1)], [T("1", price: 2)]);
        var typed = (ITypedList)result;
        var column = typed.GetItemProperties(null)["Target_Price"]!;
        var missing = typed.GetItemProperties(null)["Target_Quantity"]!;

        Assert.Equal(nameof(Trade), typed.GetListName(null));
        Assert.Equal(2d, column.GetValue(result[0]));
        Assert.Null(missing.GetValue(result[0]));
        Assert.True(column.IsReadOnly);
        Assert.False(column.CanResetValue(result[0]));
        Assert.False(column.ShouldSerializeValue(result[0]));
        Assert.Throws<NotSupportedException>(() => column.SetValue(result[0], 3d));
    }

    [Fact]
    public async Task The_steps_are_timed()
    {
        var result = await ById().CompareListAsync([T("1")], [T("1")]);

        Assert.Equal(
            ["Build Ref HashSet", "Build Target HashSet", "Compare InBoth", "Get OnlyInReference", "Get OnlyInTarget"],
            result.Timings.Select(t => t.Name).Order());
        Assert.All(result.Timings, t => Assert.True(t.End >= t.Start));
        Assert.Equal(result.Timings.OrderBy(t => t.Start), result.Timings);
    }

    [Fact]
    public async Task Concurrent_comparisons_on_one_instance_do_not_interfere()
    {
        var comparer = ById();
        var runs = Enumerable.Range(1, 8).Select(n => comparer.CompareListAsync(
            Enumerable.Range(0, 1000).Select(i => T($"{i}", price: i)).ToList(),
            Enumerable.Range(0, 1000 + n).Select(i => T($"{i}", price: i < n ? -1 : i)).ToList()));

        var results = await Task.WhenAll(runs);

        for (int n = 1; n <= 8; n++)
        {
            Assert.Equal(n, results[n - 1].Count);
            Assert.Equal(n, results[n - 1].OnlyInTarget.Count);
            Assert.Equal(5, results[n - 1].Timings.Count);
        }
    }

    [Fact]
    public async Task Large_lists_are_compared_exactly()
    {
        const int rows = 200_000;
        var reference = Enumerable.Range(0, rows).Select(i => T($"TRD-{i:D9}", price: i)).ToList();
        var target = Enumerable.Range(100, rows).Select(i => T($"TRD-{i:D9}", price: i % 1000 == 0 ? -i : i)).ToList();

        var result = await ById().CompareListAsync(reference, target);

        Assert.Equal(100, result.OnlyInReference.Count);
        Assert.Equal(100, result.OnlyInTarget.Count);
        Assert.Equal(Enumerable.Range(100, rows - 100).Count(i => i % 1000 == 0), result.Count);
    }
}
