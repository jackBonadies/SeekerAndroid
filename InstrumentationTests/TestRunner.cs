using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Util;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace InstrumentationTests
{
    /// <summary>
    /// Minimal NUnit test runner for Android instrumentation tests.
    /// Discovers and runs all [Test] methods in the assembly, logging results to logcat.
    ///
    /// Deploy and run:
    ///   dotnet build -t:Install
    ///   adb shell am instrument -w com.seeker.instrumentationtests/instrumentationtests.TestInstrumentation
    /// </summary>
    [Instrumentation(Name = "instrumentationtests.TestInstrumentation")]
    public class TestInstrumentation : Instrumentation
    {
        const string Tag = "SeekerTests";

        public override void OnCreate(Bundle arguments)
        {
            base.OnCreate(arguments);
            Start();
        }

        public override async void OnStart()
        {
            base.OnStart();

            var results = new Bundle();
            int passed = 0, failed = 0, skipped = 0;
            var failures = new List<string>();

            try
            {
                var testClasses = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
                    .ToList();

                Log.Info(Tag, $"Found {testClasses.Count} test fixture(s)");

                foreach (var testClass in testClasses)
                {
                    object instance = null;
                    try
                    {
                        instance = Activator.CreateInstance(testClass);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Tag, $"Failed to create {testClass.Name}: {ex}");
                        failed++;
                        failures.Add($"{testClass.Name}.<constructor>: {ex.Message}");
                        continue;
                    }

                    // Run [OneTimeSetUp] if present
                    foreach (var setup in testClass.GetMethods().Where(m => m.GetCustomAttribute<OneTimeSetUpAttribute>() != null))
                    {
                        try
                        {
                            await InvokeMethod(setup, instance);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(Tag, $"[OneTimeSetUp] {testClass.Name}.{setup.Name} FAILED: {ex}");
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
                            Log.Info(Tag, $"  SKIP: {testName}");
                            skipped++;
                            continue;
                        }

                        // Run [SetUp] methods
                        bool setupFailed = false;
                        foreach (var setup in testClass.GetMethods().Where(m => m.GetCustomAttribute<SetUpAttribute>() != null))
                        {
                            try
                            {
                                await InvokeMethod(setup, instance);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(Tag, $"  FAIL (SetUp): {testName} - {ex}");
                                failed++;
                                failures.Add($"{testName} (SetUp): {ex.Message}");
                                setupFailed = true;
                                break;
                            }
                        }

                        if (setupFailed) continue;

                        // Run test
                        try
                        {
                            await InvokeMethod(test, instance);
                            Log.Info(Tag, $"  PASS: {testName}");
                            passed++;
                        }
                        catch (Exception ex)
                        {
                            var inner = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                            Log.Error(Tag, $"  FAIL: {testName} - {inner}");
                            failed++;
                            failures.Add($"{testName}: {inner.Message}");
                        }

                        // Run [TearDown] methods
                        foreach (var teardown in testClass.GetMethods().Where(m => m.GetCustomAttribute<TearDownAttribute>() != null))
                        {
                            try
                            {
                                await InvokeMethod(teardown, instance);
                            }
                            catch (Exception ex)
                            {
                                Log.Warn(Tag, $"  TearDown failed for {testName}: {ex.Message}");
                            }
                        }
                    }

                    // Run [OneTimeTearDown] if present
                    foreach (var teardown in testClass.GetMethods().Where(m => m.GetCustomAttribute<OneTimeTearDownAttribute>() != null))
                    {
                        try
                        {
                            await InvokeMethod(teardown, instance);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn(Tag, $"[OneTimeTearDown] {testClass.Name}.{teardown.Name} failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Tag, $"Test runner error: {ex}");
                failures.Add($"Runner: {ex.Message}");
                failed++;
            }

            Log.Info(Tag, "========================================");
            Log.Info(Tag, $"Results: {passed} passed, {failed} failed, {skipped} skipped");
            if (failures.Count > 0)
            {
                Log.Error(Tag, "Failures:");
                foreach (var f in failures)
                    Log.Error(Tag, $"  - {f}");
            }
            Log.Info(Tag, "========================================");

            results.PutInt("passed", passed);
            results.PutInt("failed", failed);
            results.PutInt("skipped", skipped);
            results.PutString("shortMsg", $"{passed} passed, {failed} failed, {skipped} skipped");

            Finish(failed == 0 ? Result.Ok : Result.Canceled, results);
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
