using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Widget;
using NUnit.Framework;

namespace InstrumentationTests
{
    [Activity(Label = "Seeker Tests", MainLauncher = true)]
    public class MainActivity : Activity
    {
        const string Tag = "SeekerTests";
        TextView _output;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var scrollView = new ScrollView(this);
            _output = new TextView(this) { TextSize = 14 };
            _output.SetPadding(16, 16, 16, 16);
            scrollView.AddView(_output);
            SetContentView(scrollView);

            _ = RunTestsAsync();
        }

        private async Task RunTestsAsync()
        {
            int passed = 0, failed = 0, skipped = 0;
            var failures = new List<string>();

            try
            {
                var testClasses = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
                    .ToList();

                AppendLine($"Found {testClasses.Count} test fixture(s)\n");

                foreach (var testClass in testClasses)
                {
                    AppendLine($"--- {testClass.Name} ---");

                    object instance = null;
                    try
                    {
                        instance = Activator.CreateInstance(testClass);
                    }
                    catch (Exception ex)
                    {
                        AppendLine($"  FAIL (constructor): {ex.Message}");
                        failed++;
                        failures.Add($"{testClass.Name}.<constructor>: {ex.Message}");
                        continue;
                    }

                    // OneTimeSetUp
                    foreach (var setup in testClass.GetMethods().Where(m => m.GetCustomAttribute<OneTimeSetUpAttribute>() != null))
                    {
                        try
                        {
                            await InvokeMethod(setup, instance);
                        }
                        catch (Exception ex)
                        {
                            AppendLine($"  FAIL (OneTimeSetUp): {ex.Message}");
                            failed++;
                            failures.Add($"{testClass.Name}.{setup.Name} (OneTimeSetUp): {ex.Message}");
                            instance = null;
                            break;
                        }
                    }

                    if (instance == null) continue;

                    var testMethods = testClass.GetMethods()
                        .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                        .ToList();

                    foreach (var test in testMethods)
                    {
                        string testName = $"{testClass.Name}.{test.Name}";

                        if (test.GetCustomAttribute<IgnoreAttribute>() != null)
                        {
                            AppendLine($"  SKIP: {test.Name}");
                            skipped++;
                            continue;
                        }

                        // SetUp
                        bool setupFailed = false;
                        foreach (var setup in testClass.GetMethods().Where(m => m.GetCustomAttribute<SetUpAttribute>() != null))
                        {
                            try
                            {
                                await InvokeMethod(setup, instance);
                            }
                            catch (Exception ex)
                            {
                                AppendLine($"  FAIL (SetUp): {test.Name} - {ex.Message}");
                                failed++;
                                failures.Add($"{testName} (SetUp): {ex.Message}");
                                setupFailed = true;
                                break;
                            }
                        }

                        if (setupFailed) continue;

                        try
                        {
                            await InvokeMethod(test, instance);
                            AppendLine($"  PASS: {test.Name}");
                            passed++;
                        }
                        catch (Exception ex)
                        {
                            var inner = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                            AppendLine($"  FAIL: {test.Name} - {inner.Message}");
                            failed++;
                            failures.Add($"{testName}: {inner.Message}");
                        }

                        // TearDown
                        foreach (var teardown in testClass.GetMethods().Where(m => m.GetCustomAttribute<TearDownAttribute>() != null))
                        {
                            try
                            {
                                await InvokeMethod(teardown, instance);
                            }
                            catch { }
                        }
                    }

                    // OneTimeTearDown
                    foreach (var teardown in testClass.GetMethods().Where(m => m.GetCustomAttribute<OneTimeTearDownAttribute>() != null))
                    {
                        try
                        {
                            await InvokeMethod(teardown, instance);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLine($"\nRunner error: {ex}");
                failed++;
            }

            AppendLine($"\n========================================");
            AppendLine($"Results: {passed} passed, {failed} failed, {skipped} skipped");
            if (failures.Count > 0)
            {
                AppendLine("\nFailures:");
                foreach (var f in failures)
                    AppendLine($"  - {f}");
            }
            AppendLine("========================================");

            Log.Info(Tag, $"Results: {passed} passed, {failed} failed, {skipped} skipped");
        }

        private void AppendLine(string text)
        {
            Log.Info(Tag, text);
            RunOnUiThread(() => _output.Append(text + "\n"));
        }

        private static async Task InvokeMethod(MethodInfo method, object instance)
        {
            var result = method.Invoke(instance, null);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
