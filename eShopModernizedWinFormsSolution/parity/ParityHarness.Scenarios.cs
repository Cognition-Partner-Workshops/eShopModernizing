#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using eShopWinForms;
using eShopWinForms.Controllers;
#if LEGACY
using System.ServiceModel;
using eShopWinForms.eShopServiceReference;
#else
using eShopWinForms.Grpc.Client;
using eShopWinForms.Grpc.Client.Models;
#endif

namespace WinFormsParity
{
    internal static class Scenarios
    {
        private static CatalogView view;
#if LEGACY
        private static CatalogServiceClient client;
#else
        private static CatalogServiceGrpcClient client;
#endif
        private static readonly SortedDictionary<string, object> results = Map();
        private static readonly List<string> messages = new List<string>();
        private static int failures;
        private static int createdId;

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var output = Option(args, "--output");
                var address = Option(args, "--address");
                var persisted = args.Contains("--verify-persisted");
#if LEGACY
                // The unchanged legacy image loader resolves CurrentDirectory/../../Assets.
                var images = Path.GetFullPath(Option(args, "--legacy-images"));
                var working = Path.Combine(Path.GetDirectoryName(output), "legacy-cwd", "bin");
                Directory.CreateDirectory(working);
                var target = Path.GetFullPath(Path.Combine(working, "..", "..", "Assets", "Images", "Catalog"));
                Directory.CreateDirectory(target);
                foreach (var file in Directory.GetFiles(images, "*.png"))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                Environment.CurrentDirectory = working;
                var binding = new BasicHttpBinding
                {
                    MaxReceivedMessageSize = 16 * 1024 * 1024,
                    OpenTimeout = TimeSpan.FromSeconds(10),
                    SendTimeout = TimeSpan.FromSeconds(30)
                };
                client = new CatalogServiceClient(binding, new EndpointAddress(address));
#else
                Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25F));
                client = new CatalogServiceGrpcClient(new CatalogServiceClientOptions { Address = address });
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                using (var watcher = new DialogWatcher(GetCurrentThreadId()))
                using (view = new CatalogView())
                {
                    view.StartPosition = FormStartPosition.Manual;
                    view.Location = new Point(-20000, -20000);
                    view.ShowInTaskbar = false;
                    var controller = new CatalogController(client, view);
                    Run("load", delegate
                    {
                        controller.LoadView();
                        view.Show();
                        Application.DoEvents();
                        Require(persisted || GridRows().Count == 12, "Expected 12 seeded items.");
                        return Snapshot();
                    });
                    if (persisted) VerifyPersisted();
                    else Exercise();
                    view.Close();
                }
#if LEGACY
                client.Close();
#else
                client.Dispose();
#endif
                var json = Json(results);
                File.WriteAllText(output, json + "\n", new UTF8Encoding(false));
                using (var sha = SHA256.Create())
                {
                    var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(output))).Replace("-", "");
                    File.WriteAllText(output + ".sha256", hash + "\n", new UTF8Encoding(false));
                    Console.WriteLine("SHA256=" + hash);
                }
                Console.WriteLine("SCENARIOS=" + results.Count + " UNEXPECTED_FAILURES=" + failures);
                return failures == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void Exercise()
        {
            Filter("filter-brand2", 2, 0);
            Filter("filter-type2", 0, 2);
            Filter("filter-brand5-type1", 5, 1);
            Filter("filter-brand3-empty", 3, 0);
            Filter("filter-type4-empty", 0, 4);
            SetFilters(0, 0);
            Field<TabControl>("tabControl").SelectedIndex = 1;
            Search("stock-1-2017-09-20", 1, new DateTime(2017, 9, 20), 100);
            Search("stock-1-2017-09-21", 1, new DateTime(2017, 9, 21), 120);
            Search("stock-2-2017-09-20", 2, new DateTime(2017, 9, 20), 45);
            Search("stock-3-2017-09-20", 3, new DateTime(2017, 9, 20), 0);
            Write("write-item3-50", 3, new DateTime(2030, 1, 15), "50");
            Search("search-item3-50", 3, new DateTime(2030, 1, 15), 50);
            Write("write-item3-75", 3, new DateTime(2030, 1, 15), "75");
            Search("search-item3-75", 3, new DateTime(2030, 1, 15), 75);
            Write("write-item1-101", 1, new DateTime(2017, 9, 20), "101");
            Search("search-item1-101", 1, new DateTime(2017, 9, 20), 101);
            Run("validation-empty-quantity", delegate
            {
                var before = client.GetAvailableStock(new DateTime(2030, 1, 15), 3);
                SetShipment(3, new DateTime(2030, 1, 15), "");
                Field<Button>("addAvailabilityButton").PerformClick();
                var after = client.GetAvailableStock(new DateTime(2030, 1, 15), 3);
                Require(before == after && CapturedMessages().Length == 0, "Empty quantity must be a no-op.");
                return Map("before", before, "after", after, "controls", ShipmentControls());
            });
            Run("validation-nonnumeric-quantity", delegate
            {
                SetShipment(3, new DateTime(2030, 1, 15), "bad");
                Field<Button>("addAvailabilityButton").PerformClick();
                return null;
            }, typeof(FormatException));
            Run("validation-no-list-selection", delegate
            {
                Field<ListBox>("listBox1").SelectedIndex = -1;
                Field<Button>("searchAvailabilityButton").PerformClick();
                return null;
            }, typeof(NullReferenceException));
            Run("crud-find1", () => Item(client.FindCatalogItem(1)));
            Run("crud-find999", () => Item(client.FindCatalogItem(999)));
            Run("crud-create", delegate
            {
                client.CreateCatalogItem(new CatalogItem
                {
                    Name = "Parity Harness Item",
                    Description = "Parity original",
                    Price = 12.34m,
                    Picturefilename = "1.png",
                    CatalogBrandId = 2,
                    CatalogTypeId = 1
                });
                var found = client.GetCatalogItems(0, 0).Single(i => i.Name == "Parity Harness Item");
                createdId = found.Id;
                return Item(found);
            });
            Run("crud-find-created", () => Item(client.FindCatalogItem(createdId)));
            Run("crud-update", delegate
            {
                var item = client.FindCatalogItem(createdId);
                item.Name = "Parity Harness Updated";
                item.Description = "Parity updated";
                item.Price = 15.67m;
                client.UpdateCatalogItem(item);
                var updated = client.FindCatalogItem(createdId);
                Require(updated.Name == item.Name && updated.Price == item.Price, "Update not persisted.");
                return Item(updated);
            });
            Run("crud-remove", delegate
            {
                client.RemoveCatalogItem(client.FindCatalogItem(createdId));
                var removed = client.FindCatalogItem(createdId);
                Require(removed == null, "Remove not persisted.");
                return Item(removed);
            });
            Discount("discount-2017-09-20", new DateTime(2017, 9, 20));
            Discount("discount-2026-11-05", new DateTime(2026, 11, 5));
            Discount("discount-2031-01-01", new DateTime(2031, 1, 1));
        }

        private static void VerifyPersisted()
        {
            Field<TabControl>("tabControl").SelectedIndex = 1;
            Search("persisted-item3-75", 3, new DateTime(2030, 1, 15), 75);
            Search("persisted-item1-101", 1, new DateTime(2017, 9, 20), 101);
            Run("persisted-crud-removed", delegate
            {
                var remaining = client.GetCatalogItems(0, 0).Where(i => i.Name.StartsWith("Parity Harness", StringComparison.Ordinal)).ToList();
                return remaining.Select(Item).ToList();
            }, check: value => ((IList)value).Count == 0);
        }

        private static void Discount(string name, DateTime date)
        {
            Run(name, delegate
            {
                var discount = client.GetDiscount(date);
                return discount == null ? null : Map("id", discount.Id, "start", Day(discount.Start),
                    "end", Day(discount.End), "size", discount.Size.ToString("R", CultureInfo.InvariantCulture));
            });
        }

        private static void Filter(string name, int brand, int type)
        {
            Run(name, delegate { SetFilters(brand, type); return Snapshot(); });
        }

        private static void SetFilters(int brand, int type)
        {
            Field<ComboBox>("catalogBrandComboBox").SelectedValue = brand;
            Field<ComboBox>("catalogTypeComboBox").SelectedValue = type;
            Application.DoEvents();
            Require(Convert.ToInt32(Field<ComboBox>("catalogBrandComboBox").SelectedValue) == brand, "Brand not selected.");
            Require(Convert.ToInt32(Field<ComboBox>("catalogTypeComboBox").SelectedValue) == type, "Type not selected.");
        }

        private static void Search(string name, int id, DateTime date, int expected)
        {
            Run(name, delegate
            {
                var list = Field<ListBox>("listBox1");
                list.SelectedIndex = Enumerable.Range(0, list.Items.Count).Single(i => list.Items[i].ToString().StartsWith(id + " - ", StringComparison.Ordinal));
                Field<MonthCalendar>("monthCalendar1").SetDate(date);
                var rows = Field<ListView>("listView1");
                int before = rows.Items.Count;
                Field<Button>("searchAvailabilityButton").PerformClick();
                Require(rows.Items.Count == before + 1, "Search button did not append a row.");
                Require(rows.Items[before].SubItems[2].Text == expected.ToString(CultureInfo.InvariantCulture), "Unexpected stock quantity.");
                return StockRows();
            });
        }

        private static void Write(string name, int id, DateTime date, string quantity)
        {
            Run(name, delegate
            {
                SetShipment(id, date, quantity);
                Field<Button>("addAvailabilityButton").PerformClick();
                Require(CapturedMessages().SequenceEqual(new[] { "Shipment has been added to the database." }), "Expected shipment MessageBox.");
                Require(client.GetAvailableStock(date, id) == int.Parse(quantity, CultureInfo.InvariantCulture), "Shipment not saved.");
                return ShipmentControls();
            });
        }

        private static void SetShipment(int id, DateTime date, string quantity)
        {
            Field<ComboBox>("productIdInput").SelectedItem = id;
            Field<TextBox>("arrivalDateInput").Text = Day(date);
            Field<TextBox>("quantityInput").Text = quantity;
        }

        private static object ShipmentControls() => Map("productText", Field<ComboBox>("productIdInput").Text,
            "selectedProduct", Field<ComboBox>("productIdInput").SelectedItem, "quantity", Field<TextBox>("quantityInput").Text,
            "arrivalDate", Field<TextBox>("arrivalDateInput").Text);

        private static object Snapshot() => Map("grid", GridRows(), "banner", Field<Label>("discountBanner").Text,
            "brands", Combo("catalogBrandComboBox"), "types", Combo("catalogTypeComboBox"),
            "selectedBrand", Field<ComboBox>("catalogBrandComboBox").SelectedValue,
            "selectedType", Field<ComboBox>("catalogTypeComboBox").SelectedValue,
            "listBox1", Field<ListBox>("listBox1").Items.Cast<object>().Select(x => x.ToString()).ToList(),
            "productIdInput", Field<ComboBox>("productIdInput").Items.Cast<object>().ToList(), "stockRows", StockRows());

        private static object Combo(string name) => Field<ComboBox>(name).Items.Cast<KeyValuePair<int, string>>()
            .Select(k => Map("id", k.Key, "text", k.Value)).ToList();

        private static List<object> GridRows() => Field<DataGridView>("catalogItemDataGridView").Rows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow).Select(r =>
            {
                var image = r.Cells[0].Value as Image;
                return (object)Map("id", r.Cells[1].Value.ToString(), "name", r.Cells[2].Value.ToString(),
                    "description", r.Cells[3].Value.ToString(), "priceText", r.Cells[4].Value.ToString(),
                    "imagePresent", image != null, "imageWidth", image == null ? 0 : image.Width, "imageHeight", image == null ? 0 : image.Height);
            }).OrderBy(r => int.Parse((string)((SortedDictionary<string, object>)r)["id"], CultureInfo.InvariantCulture)).ToList();

        private static object StockRows() => Field<ListView>("listView1").Items.Cast<ListViewItem>()
            .Select(r => Map("date", Day(DateTime.Parse(r.SubItems[0].Text, CultureInfo.GetCultureInfo("en-US"))),
                "item", r.SubItems[1].Text, "quantity", r.SubItems[2].Text)).ToList();

        private static object Item(CatalogItem item) => item == null ? null : Map("id", item.Id, "name", item.Name,
            "description", item.Description, "price", item.Price.ToString("F4", CultureInfo.InvariantCulture),
            "picture", item.Picturefilename, "brandId", item.CatalogBrandId, "typeId", item.CatalogTypeId);

        private static T Field<T>(string name) => (T)typeof(CatalogView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        private static string Day(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static string Option(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 == args.Length) throw new ArgumentException("Required argument: " + name);
            return Path.IsPathRooted(args[i + 1]) && name != "--address" ? Path.GetFullPath(args[i + 1]) : args[i + 1];
        }

        private static void Run(string name, Func<object> action, Type expectedException = null, Func<object, bool> check = null)
        {
            Console.WriteLine("START " + name);
            lock (messages) messages.Clear();
            object value = null;
            Exception error = null;
            try
            {
                value = action();
                if (check != null) Require(check(value), "Unexpected result for " + name + ".");
            }
            catch (Exception ex) { error = ex; }
            bool valid = expectedException == null ? error == null : error != null && error.GetType() == expectedException;
            if (!valid) failures++;
            results.Add(name, Map("value", value, "exception", error == null ? null : Map("type", error.GetType().FullName, "message", error.Message),
                "messages", CapturedMessages(), "expectedOutcome", valid));
            Console.WriteLine(name + ": " + (valid ? "OK" : "UNEXPECTED FAILURE") + (error == null ? "" : " " + error.GetType().Name + ": " + error.Message));
        }

        private delegate bool EnumWindow(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindow callback, IntPtr parameter);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int max);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

        private static string[] CapturedMessages() { lock (messages) return messages.ToArray(); }

        private sealed class DialogWatcher : IDisposable
        {
            private readonly ManualResetEvent stop = new ManualResetEvent(false);
            private readonly Thread thread;

            public DialogWatcher(uint uiThread)
            {
                thread = new Thread(() =>
                {
                    while (!stop.WaitOne(50)) DismissMessages(uiThread);
                })
                { IsBackground = true };
                thread.Start();
            }

            public void Dispose()
            {
                stop.Set();
                if (!thread.Join(5000)) throw new TimeoutException("MessageBox observer did not stop.");
                stop.Dispose();
            }
        }

        private static void DismissMessages(uint uiThread)
        {
            EnumThreadWindows(uiThread, delegate (IntPtr window, IntPtr ignored)
            {
                var name = new StringBuilder(256);
                GetClassName(window, name, name.Capacity);
                if (name.ToString() != "#32770") return true;
                var texts = new List<string>();
                var button = IntPtr.Zero;
                EnumChildWindows(window, delegate (IntPtr child, IntPtr unused)
                {
                    var cls = new StringBuilder(256);
                    GetClassName(child, cls, cls.Capacity);
                    var text = new StringBuilder(4096);
                    GetWindowText(child, text, text.Capacity);
                    if (cls.ToString() == "Static" && text.Length > 0) texts.Add(text.ToString());
                    if (cls.ToString() == "Button" && text.ToString() == "OK") button = child;
                    return true;
                }, IntPtr.Zero);
                if (button != IntPtr.Zero && texts.Count > 0)
                {
                    lock (messages) messages.Add(string.Join("\n", texts));
                    PostMessage(button, 0x00F5, IntPtr.Zero, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
        }

        private static SortedDictionary<string, object> Map(params object[] pairs)
        {
            var map = new SortedDictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i < pairs.Length; i += 2) map.Add((string)pairs[i], pairs[i + 1]);
            return map;
        }

        private static string Json(object value)
        {
            if (value == null) return "null";
            var text = value as string;
            if (text != null)
            {
                var escaped = new StringBuilder("\"");
                foreach (char c in text)
                {
                    if (c == '"' || c == '\\') escaped.Append('\\').Append(c);
                    else if (c < 32) escaped.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else escaped.Append(c);
                }
                return escaped.Append('"').ToString();
            }
            var map = value as SortedDictionary<string, object>;
            if (map != null) return "{" + string.Join(",", map.Select(p => Json(p.Key) + ":" + Json(p.Value))) + "}";
            if (value is bool) return (bool)value ? "true" : "false";
            var list = value as IEnumerable;
            if (list != null) return "[" + string.Join(",", list.Cast<object>().Select(Json)) + "]";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
